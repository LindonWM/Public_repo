using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.JSInterop;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class AuthService
{
    private const string TokenStorageKey = "skillsnap.auth.token";
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly UserSessionService _userSession;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, UserSessionService userSession)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _userSession = userSession;
    }

    public bool IsAuthenticated { get; private set; }
    public string? Token { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public string? UserEmail { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = [];

    public bool IsInRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    public async Task InitializeAsync()
    {
        // Restore persisted login state on app startup.
        var storedToken = await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            TokenStorageKey
        );
        if (string.IsNullOrWhiteSpace(storedToken))
        {
            SetAnonymousState();
            return;
        }

        var jwtPayload = DecodeJwtPayload(storedToken);
        if (jwtPayload is null)
        {
            await ClearTokenStorageAsync();
            SetAnonymousState();
            return;
        }

        if (TryGetExpiry(jwtPayload, out var expiresAtUtc) && expiresAtUtc <= DateTime.UtcNow)
        {
            // Expired tokens are removed eagerly to avoid sending invalid auth headers.
            await ClearTokenStorageAsync();
            SetAnonymousState();
            return;
        }

        SetAuthenticatedState(storedToken, jwtPayload, expiresAtUtc);
    }

    public async Task<AuthResult> RegisterAsync(RegisterModel model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);
        if (!response.IsSuccessStatusCode)
        {
            return new AuthResult(false, await BuildErrorMessageAsync(response));
        }

        return new AuthResult(true, "Registered successfully. You can now log in.");
    }

    public async Task<AuthResult> LoginAsync(LoginModel model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);
        if (!response.IsSuccessStatusCode)
        {
            return new AuthResult(false, await BuildErrorMessageAsync(response));
        }

        var authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (authResponse is null || string.IsNullOrWhiteSpace(authResponse.Token))
        {
            return new AuthResult(false, "Login failed: token is missing in response.");
        }

        var jwtPayload = DecodeJwtPayload(authResponse.Token);
        if (jwtPayload is null)
        {
            return new AuthResult(false, "Login failed: token payload is invalid.");
        }

        var expiresAtUtc = authResponse.ExpiresAtUtc;
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            return new AuthResult(false, "Login failed: token already expired.");
        }

        await _jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            TokenStorageKey,
            authResponse.Token
        );
        SetAuthenticatedState(authResponse.Token, jwtPayload, expiresAtUtc);

        return new AuthResult(true, "Logged in successfully.");
    }

    public async Task LogoutAsync()
    {
        await ClearTokenStorageAsync();
        SetAnonymousState();
    }

    private void SetAuthenticatedState(
        string token,
        Dictionary<string, JsonElement> payload,
        DateTime expiresAtUtc
    )
    {
        // Derive UI session info from JWT claims and set default bearer auth for API calls.
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        IsAuthenticated = true;
        UserEmail = GetClaim(payload, JwtClaimNames.Email) ?? GetClaim(payload, JwtClaimNames.Name);
        Roles = GetRoleClaims(payload);
        var userId =
            GetClaim(payload, JwtClaimNames.Sub) ?? GetClaim(payload, JwtClaimNames.NameIdentifier);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
        _userSession.SetAuthenticatedUser(userId, UserEmail, Roles, ExpiresAtUtc);
    }

    private void SetAnonymousState()
    {
        Token = null;
        ExpiresAtUtc = null;
        IsAuthenticated = false;
        UserEmail = null;
        Roles = [];
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _userSession.Clear();
    }

    private async Task ClearTokenStorageAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
    }

    private static Dictionary<string, JsonElement>? DecodeJwtPayload(string token)
    {
        // JWT is 3 dot-separated Base64Url parts: header.payload.signature.
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadBytes);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    private static bool TryGetExpiry(
        Dictionary<string, JsonElement> payload,
        out DateTime expiresAtUtc
    )
    {
        expiresAtUtc = DateTime.MinValue;
        if (!payload.TryGetValue(JwtClaimNames.Exp, out var expValue))
        {
            return false;
        }

        if (expValue.ValueKind == JsonValueKind.Number && expValue.TryGetInt64(out var expUnix))
        {
            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            return true;
        }

        if (
            expValue.ValueKind == JsonValueKind.String
            && long.TryParse(expValue.GetString(), out var expUnixString)
        )
        {
            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnixString).UtcDateTime;
            return true;
        }

        return false;
    }

    private static string? GetClaim(Dictionary<string, JsonElement> payload, string claimType)
    {
        if (!payload.TryGetValue(claimType, out var claimValue))
        {
            return null;
        }

        return claimValue.ValueKind switch
        {
            JsonValueKind.String => claimValue.GetString(),
            JsonValueKind.Number => claimValue.GetRawText(),
            _ => null,
        };
    }

    private static IReadOnlyList<string> GetRoleClaims(Dictionary<string, JsonElement> payload)
    {
        // Support both single-role and multi-role token formats.
        if (!payload.TryGetValue(JwtClaimNames.Role, out var roleElement))
        {
            return [];
        }

        if (roleElement.ValueKind == JsonValueKind.String)
        {
            var role = roleElement.GetString();
            return string.IsNullOrWhiteSpace(role) ? [] : [role];
        }

        if (roleElement.ValueKind == JsonValueKind.Array)
        {
            var roles = new List<string>();
            foreach (var element in roleElement.EnumerateArray())
            {
                if (
                    element.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(element.GetString())
                )
                {
                    roles.Add(element.GetString()!);
                }
            }

            return roles;
        }

        return [];
    }

    private static async Task<string> BuildErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return $"{(int)response.StatusCode} {response.ReasonPhrase}. {body}";
    }

    private static class JwtClaimNames
    {
        public const string Exp = "exp";
        public const string Sub = "sub";
        public const string Email = "email";
        public const string Name = ClaimTypes.Name;
        public const string NameIdentifier = ClaimTypes.NameIdentifier;
        public const string Role = ClaimTypes.Role;
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}

public sealed record AuthResult(bool Succeeded, string Message);

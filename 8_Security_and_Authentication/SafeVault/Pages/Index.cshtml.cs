using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SafeVault.Data;
using SafeVault.Helpers;

namespace SafeVault.Pages;

public class IndexModel(IUserRepository userRepository) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(255, MinimumLength = 1)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    public string? CurrentUsername => User.Identity?.Name;

    public string? CurrentRole => User.FindFirstValue(ClaimTypes.Role);

    /// <summary>Shown after a successful, clean submission.</summary>
    public string? SuccessMessage { get; set; }

    /// <summary>Shown when a user lookup fails due to missing record or configuration/runtime issue.</summary>
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    // Razor Pages validates the antiforgery token on POST automatically.
    // Sanitize first, then re-validate so model errors reflect clean values.
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        // Sanitize at the boundary — strip HTML tags and enforce whitelists
        Username = InputSanitizer.SanitizeUsername(Username);
        Password = InputSanitizer.SanitizePassword(Password);

        // Clear stale model state so we re-validate the sanitized values
        ModelState.Clear();

        if (!InputSanitizer.IsValidUsername(Username))
            ModelState.AddModelError(
                nameof(Username),
                "Username may only contain letters, digits, spaces, hyphens, underscores, and dots (max 50 chars)."
            );

        if (!InputSanitizer.IsValidPassword(Password))
            ModelState.AddModelError(nameof(Password), "Password is required (max 255 chars).");

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var user = await userRepository.GetByUsernameAndPasswordAsync(
                Username,
                Password,
                cancellationToken
            );

            if (user is null)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            SuccessMessage = $"Logged in as {user.Username} (Role: {user.Role}).";
        }
        catch (InvalidOperationException)
        {
            ErrorMessage =
                "Database connection is not configured. Set ConnectionStrings:SafeVaultDb in appsettings.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        SuccessMessage = "Logged out successfully.";
        return RedirectToPage();
    }
}

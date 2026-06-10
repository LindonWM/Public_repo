using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SkillSnap.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (model is null)
        {
            return BadRequest(new { Message = "Request body is required." });
        }

        if (
            string.IsNullOrWhiteSpace(model.Email)
            || string.IsNullOrWhiteSpace(model.Password)
            || string.IsNullOrWhiteSpace(model.ConfirmPassword)
        )
        {
            return BadRequest(
                new { Message = "Email, password, and confirm password are required." }
            );
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new { Message = "Password and confirm password do not match." });
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null)
        {
            return Conflict(new { Message = "An account with this email already exists." });
        }

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(
                new
                {
                    Message = "Registration failed.",
                    Errors = createResult.Errors.Select(error => error.Description),
                }
            );
        }

        return Ok(new { Message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (model is null)
        {
            return BadRequest(new { Message = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new { Message = "Email and password are required." });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return Unauthorized(new { Message = "Invalid email or password." });
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            model.Password,
            lockoutOnFailure: true
        );

        if (!signInResult.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid email or password." });
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes());
        var token = await GenerateJwtTokenAsync(user, expiresAtUtc);

        return Ok(new AuthResponse(token, expiresAtUtc));
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecretKey()));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: GetJwtIssuer(),
            audience: GetJwtAudience(),
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(jwtToken);
    }

    private string GetJwtIssuer() => _configuration["Jwt:Issuer"] ?? "SkillSnap.Api";

    private string GetJwtAudience() => _configuration["Jwt:Audience"] ?? "SkillSnap.Client";

    private string GetJwtSecretKey() =>
        _configuration["Jwt:Key"] ?? "ChangeThisJwtSecretKeyToASecureValue123!";

    private int GetJwtExpirationMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 60;
    }
}

public record AuthResponse(string Token, DateTime ExpiresAtUtc);

namespace ManagementApp.Services;

public class SimpleTokenValidator : ITokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly HashSet<string> _validTokens;

    public SimpleTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
        // Load valid tokens from configuration
        // Format: "Authentication:ValidTokens": ["token1", "token2"]
        _validTokens = new(
            _configuration.GetSection("Authentication:ValidTokens").Get<string[]>() ?? 
            new[] { "default-test-token" }
        );
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        return Task.FromResult(_validTokens.Contains(token));
    }
}

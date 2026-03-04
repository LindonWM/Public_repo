namespace ManagementApp.Services;

public interface ITokenValidator
{
    Task<bool> ValidateTokenAsync(string token);
}

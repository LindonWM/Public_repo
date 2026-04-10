namespace SafeVault.Helpers;

/// <summary>
/// Wraps BCrypt password hashing with a fixed work factor (12).
/// </summary>
public static class PasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>Creates a BCrypt hash for the given plaintext password.</summary>
    public static string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>Returns true when the plaintext password matches the stored BCrypt hash.</summary>
    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}

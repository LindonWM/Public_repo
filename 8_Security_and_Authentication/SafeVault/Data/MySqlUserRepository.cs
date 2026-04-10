using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SafeVault.Models;

namespace SafeVault.Data;

public sealed class MySqlUserRepository(IConfiguration configuration) : IUserRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("SafeVaultDb")
        ?? throw new InvalidOperationException("Connection string 'SafeVaultDb' is missing.");

    public async Task<UserRecord?> GetByUsernameAndPasswordAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        // Fetch the stored hash by username only — never compare passwords in SQL
        const string sql = """
            SELECT UserID, Username, Email, Role, PasswordHash
            FROM Users
            WHERE Username = @Username
            LIMIT 1;
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var storedHash = reader.GetString("PasswordHash");

        // Constant-time BCrypt verification — prevents timing side-channel
        if (!SafeVault.Helpers.PasswordHasher.Verify(password, storedHash))
            return null;

        return new UserRecord
        {
            UserId = reader.GetInt32("UserID"),
            Username = reader.GetString("Username"),
            Email = reader.GetString("Email"),
            Role = reader.GetString("Role"),
        };
    }

    public async Task<IReadOnlyList<UserRecord>> SearchByUsernamePrefixAsync(
        string searchTerm,
        int maxResults = 20,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = """
            SELECT UserID, Username, Email, Role
            FROM Users
            WHERE Username LIKE @SearchPattern
            ORDER BY Username ASC
            LIMIT @MaxResults;
            """;

        var users = new List<UserRecord>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SearchPattern", $"{searchTerm}%");
        command.Parameters.AddWithValue("@MaxResults", Math.Clamp(maxResults, 1, 100));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(
                new UserRecord
                {
                    UserId = reader.GetInt32("UserID"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    Role = reader.GetString("Role"),
                }
            );
        }

        return users;
    }
}

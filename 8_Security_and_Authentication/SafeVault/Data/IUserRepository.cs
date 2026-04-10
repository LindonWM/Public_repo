using SafeVault.Models;

namespace SafeVault.Data;

public interface IUserRepository
{
    Task<UserRecord?> GetByUsernameAndPasswordAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<UserRecord>> SearchByUsernamePrefixAsync(
        string searchTerm,
        int maxResults = 20,
        CancellationToken cancellationToken = default
    );
}

namespace SkillSnap.Client.Services;

public class UserSessionService
{
    public bool IsAuthenticated { get; private set; }
    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? PrimaryRole { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = [];
    public DateTime? ExpiresAtUtc { get; private set; }

    public event Action? SessionChanged;

    public void SetAuthenticatedUser(
        string? userId,
        string? email,
        IReadOnlyList<string> roles,
        DateTime? expiresAtUtc
    )
    {
        IsAuthenticated = true;
        UserId = userId;
        Email = email;
        Roles = roles;
        PrimaryRole = roles.FirstOrDefault();
        ExpiresAtUtc = expiresAtUtc;
        NotifyChanged();
    }

    public void Clear()
    {
        IsAuthenticated = false;
        UserId = null;
        Email = null;
        PrimaryRole = null;
        Roles = [];
        ExpiresAtUtc = null;
        NotifyChanged();
    }

    private void NotifyChanged() => SessionChanged?.Invoke();
}

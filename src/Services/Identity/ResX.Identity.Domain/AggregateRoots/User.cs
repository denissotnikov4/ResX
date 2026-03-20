using ResX.Common.Domain;
using ResX.Common.Exceptions;
using ResX.Identity.Domain.Entities;
using ResX.Identity.Domain.Enums;
using ResX.Identity.Domain.Events;
using ResX.Identity.Domain.ValueObjects;

namespace ResX.Identity.Domain.AggregateRoots;

public class User : AggregateRoot<Guid>
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    public Email Email { get; private set; } = null!;

    public PhoneNumber? Phone { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static User Create(
        string email,
        string? phone,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Email.Create(email),
            Phone = phone != null ? PhoneNumber.Create(phone) : null,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email.Value));

        return user;
    }

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = RefreshToken.Create(Id, token, expiresAt);

        _refreshTokens.Add(refreshToken);

        RevokeOldRefreshTokens();

        return refreshToken;
    }

    public void RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token)
                           ?? throw new DomainException("Refresh token not found.");

        refreshToken.Revoke();
    }

    public RefreshToken? GetActiveRefreshToken(string token)
    {
        return _refreshTokens.FirstOrDefault(t => t.Token == token && t.IsActive);
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
        RevokeAllRefreshTokens();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        RevokeAllRefreshTokens();
    }

    public void RecordLogin()
    {
        RaiseDomainEvent(new UserLoggedInDomainEvent(Id, Email.Value));
    }

    private void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    private void RevokeOldRefreshTokens()
    {
        // Best practice for auth flows: don't physically delete tokens during login.
        // Concurrent logins can otherwise race on DELETE and trigger optimistic concurrency exceptions.
        // We keep only a small window of inactive tokens; the rest stay revoked and can be purged by a background job.
        var tokensToKeep = _refreshTokens
            .Where(t => !t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .ToHashSet();

        foreach (var token in _refreshTokens.Where(t => !t.IsActive).ToList())
        {
            if (!tokensToKeep.Contains(token))
            {
                token.Revoke();
            }
        }
    }
}
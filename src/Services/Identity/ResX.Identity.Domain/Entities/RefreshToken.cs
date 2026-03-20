using ResX.Common.Domain;

namespace ResX.Identity.Domain.Entities;

public class RefreshToken : Entity<Guid>
{
    private RefreshToken()
    {
    }

    public string Token { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsRevoked { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
    }
}
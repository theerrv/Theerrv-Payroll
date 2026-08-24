using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Auth;

/// <summary>
/// Doc §5.6. token_hash, expires_at, revoked_at — supports token rotation.
/// Reuse detection: if a revoked token is presented, revoke the entire chain.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the token value. The plain token is never stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>The replacement token in the rotation chain, for reuse detection.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public User User { get; set; } = null!;
}

namespace Zibzie.HealthCore.Domain.Entities;

public class PatientAccessGrant
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid? UserId { get; set; }

    public string? ServiceAccountId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductRole { get; set; } = string.Empty;

    public string AccessScope { get; set; } = string.Empty;

    public string AuthorizationReason { get; set; } = string.Empty;

    public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ValidUntil { get; set; }

    public Guid? GrantedByUserId { get; set; }

    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? GrantNote { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedByUserId { get; set; }

    public string? RevokeReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public PatientProfile PatientProfile { get; set; } = null!;

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null
            && ValidFrom <= now
            && (!ValidUntil.HasValue || ValidUntil.Value >= now);
    }

    public void Revoke(Guid? revokedByUserId, string? reason, DateTimeOffset now)
    {
        RevokedAt = now;
        RevokedByUserId = revokedByUserId;
        RevokeReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        UpdatedAt = now;
    }
}

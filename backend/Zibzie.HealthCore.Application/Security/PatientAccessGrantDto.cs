namespace Zibzie.HealthCore.Application.Security;

public class PatientAccessGrantDto
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductRole { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public Guid? GranteeUserId { get; set; }

    public string? ServiceAccountId { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset? ValidUntil { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    public Guid? GrantedByUserId { get; set; }

    public string? GrantedByServiceAccountId { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedByUserId { get; set; }

    public string? RevokedByServiceAccountId { get; set; }

    public string? RevokeReason { get; set; }
}

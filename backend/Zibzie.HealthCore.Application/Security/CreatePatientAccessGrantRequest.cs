namespace Zibzie.HealthCore.Application.Security;

public class CreatePatientAccessGrantRequest
{
    public Guid? GranteeUserId { get; set; }

    public string? ServiceAccountId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductRole { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset? ValidFrom { get; set; }

    public DateTimeOffset? ValidUntil { get; set; }

    public string? Notes { get; set; }
}

namespace Zibzie.HealthCore.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? ServiceAccountId { get; set; }

    public Guid? PatientId { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductRole { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public string? Permission { get; set; }

    public string? AccessScope { get; set; }

    public string? AuthorizationReason { get; set; }

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }

    public string? RequestPath { get; set; }

    public string? HttpMethod { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

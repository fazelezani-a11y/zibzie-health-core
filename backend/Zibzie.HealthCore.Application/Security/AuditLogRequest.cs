namespace Zibzie.HealthCore.Application.Security;

public sealed record AuditLogRequest
{
    public Guid? UserId { get; init; }

    public string? ServiceAccountId { get; init; }

    public Guid? PatientId { get; init; }

    public string? ProductCode { get; init; }

    public string? ProductRole { get; init; }

    public string ActionType { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public Guid? ResourceId { get; init; }

    public string? Permission { get; init; }

    public string? AccessScope { get; init; }

    public string? AuthorizationReason { get; init; }

    public bool Succeeded { get; init; }

    public string? FailureReason { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }

    public string? RequestPath { get; init; }

    public string? HttpMethod { get; init; }

    public string? MetadataJson { get; init; }
}

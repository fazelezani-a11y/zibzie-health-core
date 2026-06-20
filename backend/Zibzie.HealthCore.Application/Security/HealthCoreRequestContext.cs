namespace Zibzie.HealthCore.Application.Security;

public sealed record HealthCoreRequestContext
{
    public Guid? UserId { get; init; }

    public string? ServiceAccountId { get; init; }

    public string? ProductCode { get; init; }

    public string? ProductRole { get; init; }

    public string? CorrelationId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? RequestPath { get; init; }

    public string? HttpMethod { get; init; }

    public bool IsAuthenticated { get; init; }

    public bool IsFallbackContext { get; init; }
}

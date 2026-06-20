namespace Zibzie.HealthCore.Application.Security;

public sealed record HealthCoreAuthorizationContext
{
    public Guid? UserId { get; init; }

    public string? ServiceAccountId { get; init; }

    public Guid PatientId { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public string ProductRole { get; init; } = string.Empty;

    public DateTimeOffset? Now { get; init; }
}

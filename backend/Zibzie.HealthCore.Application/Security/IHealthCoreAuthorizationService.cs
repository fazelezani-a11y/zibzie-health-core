namespace Zibzie.HealthCore.Application.Security;

public interface IHealthCoreAuthorizationService
{
    Task<AccessDecision> CanAccessPatientAsync(
        HealthCoreAuthorizationContext context,
        CancellationToken cancellationToken = default);

    Task<AccessDecision> HasPermissionAsync(
        HealthCoreAuthorizationContext context,
        string permission,
        CancellationToken cancellationToken = default);

    Task<AccessDecision> CanViewPatientSectionAsync(
        HealthCoreAuthorizationContext context,
        string sectionPermission,
        string? sensitivityLevel = null,
        CancellationToken cancellationToken = default);

    Task<AccessDecision> CanEditPatientSectionAsync(
        HealthCoreAuthorizationContext context,
        string sectionPermission,
        string? sensitivityLevel = null,
        CancellationToken cancellationToken = default);

    Task<AccessDecision> CanViewSensitivityLevelAsync(
        HealthCoreAuthorizationContext context,
        string? sensitivityLevel,
        CancellationToken cancellationToken = default);
}

namespace Zibzie.HealthCore.Domain.Security;

public sealed record ProductAccessProfile(
    string ProductCode,
    IReadOnlyCollection<ProductRoleAccessProfile> Roles);

public sealed record ProductRoleAccessProfile(
    string RoleCode,
    string ProductCode,
    string AccessScope,
    string AuthorizationReason,
    IReadOnlyCollection<string> Permissions);

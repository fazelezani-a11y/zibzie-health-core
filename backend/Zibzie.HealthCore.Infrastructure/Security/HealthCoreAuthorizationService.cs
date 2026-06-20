using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.Security;

public class HealthCoreAuthorizationService : IHealthCoreAuthorizationService
{
    private const string RestrictedSensitivityLevel = "Restricted";

    private readonly AppDbContext _dbContext;

    public HealthCoreAuthorizationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AccessDecision> CanAccessPatientAsync(
        HealthCoreAuthorizationContext context,
        CancellationToken cancellationToken = default)
    {
        return EvaluateAsync(context, null, null, cancellationToken);
    }

    public Task<AccessDecision> HasPermissionAsync(
        HealthCoreAuthorizationContext context,
        string permission,
        CancellationToken cancellationToken = default)
    {
        return EvaluateAsync(context, permission, null, cancellationToken);
    }

    public Task<AccessDecision> CanViewPatientSectionAsync(
        HealthCoreAuthorizationContext context,
        string sectionPermission,
        string? sensitivityLevel = null,
        CancellationToken cancellationToken = default)
    {
        return EvaluateAsync(context, sectionPermission, sensitivityLevel, cancellationToken);
    }

    public Task<AccessDecision> CanEditPatientSectionAsync(
        HealthCoreAuthorizationContext context,
        string sectionPermission,
        string? sensitivityLevel = null,
        CancellationToken cancellationToken = default)
    {
        return EvaluateAsync(context, sectionPermission, sensitivityLevel, cancellationToken);
    }

    public Task<AccessDecision> CanViewSensitivityLevelAsync(
        HealthCoreAuthorizationContext context,
        string? sensitivityLevel,
        CancellationToken cancellationToken = default)
    {
        return EvaluateAsync(context, null, sensitivityLevel, cancellationToken);
    }

    private async Task<AccessDecision> EvaluateAsync(
        HealthCoreAuthorizationContext context,
        string? permission,
        string? sensitivityLevel,
        CancellationToken cancellationToken)
    {
        var validationDecision = ValidateContext(context, permission);
        if (validationDecision is not null)
        {
            return validationDecision;
        }

        var roleProfile = ProductAccessProfiles.GetRoleProfile(context.ProductCode, context.ProductRole);
        if (roleProfile is null)
        {
            return AccessDecision.Deny("Product role profile was not found.");
        }

        if (!string.IsNullOrWhiteSpace(permission) && !HasRolePermission(roleProfile, permission))
        {
            return AccessDecision.Deny("Product role does not include the requested permission.");
        }

        var sensitivityPermissionDecision = ResolveSensitivityPermission(roleProfile, sensitivityLevel);
        if (!sensitivityPermissionDecision.IsAllowed)
        {
            return sensitivityPermissionDecision;
        }

        var effectivePermission = permission ?? sensitivityPermissionDecision.MatchedPermission;
        var isEmergencyPermission = string.Equals(
            effectivePermission,
            HealthPermissions.EmergencyAccess,
            StringComparison.Ordinal);

        if (!isEmergencyPermission && CanInternalAdminAccessAllPatientsWithoutGrant(context, roleProfile))
        {
            return AccessDecision.Allow(effectivePermission, roleProfile.AccessScope);
        }

        var activeGrant = await FindActiveGrantAsync(context, cancellationToken);
        if (activeGrant is null)
        {
            return AccessDecision.Deny("No active patient access grant was found.");
        }

        if (!IsGrantScopeCompatible(activeGrant, roleProfile, isEmergencyPermission))
        {
            return AccessDecision.Deny("Patient access grant scope does not match the product role profile.");
        }

        if (isEmergencyPermission && !IsEmergencyGrant(activeGrant))
        {
            return AccessDecision.Deny("Emergency access requires an active emergency grant.");
        }

        return AccessDecision.Allow(effectivePermission, activeGrant.AccessScope, activeGrant.Id);
    }

    private static AccessDecision? ValidateContext(HealthCoreAuthorizationContext context, string? permission)
    {
        if (context.PatientId == Guid.Empty)
        {
            return AccessDecision.Deny("PatientId is required.");
        }

        if (string.IsNullOrWhiteSpace(context.ProductCode))
        {
            return AccessDecision.Deny("ProductCode is required.");
        }

        if (string.IsNullOrWhiteSpace(context.ProductRole))
        {
            return AccessDecision.Deny("ProductRole is required.");
        }

        if (!context.UserId.HasValue && string.IsNullOrWhiteSpace(context.ServiceAccountId))
        {
            return AccessDecision.Deny("UserId or ServiceAccountId is required.");
        }

        if (permission is not null && string.IsNullOrWhiteSpace(permission))
        {
            return AccessDecision.Deny("Permission is required.");
        }

        return null;
    }

    private async Task<PatientAccessGrant?> FindActiveGrantAsync(
        HealthCoreAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        var now = context.Now ?? DateTimeOffset.UtcNow;
        var serviceAccountId = context.ServiceAccountId?.Trim();

        return await _dbContext.PatientAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.PatientId == context.PatientId &&
                grant.ProductCode == context.ProductCode &&
                grant.ProductRole == context.ProductRole &&
                grant.RevokedAt == null &&
                grant.ValidFrom <= now &&
                (!grant.ValidUntil.HasValue || grant.ValidUntil.Value >= now))
            .Where(grant =>
                (context.UserId.HasValue && grant.UserId == context.UserId.Value) ||
                (!string.IsNullOrWhiteSpace(serviceAccountId) && grant.ServiceAccountId == serviceAccountId))
            .OrderByDescending(grant => grant.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool HasRolePermission(ProductRoleAccessProfile roleProfile, string permission)
    {
        return roleProfile.Permissions.Contains(permission, StringComparer.Ordinal);
    }

    private static AccessDecision ResolveSensitivityPermission(
        ProductRoleAccessProfile roleProfile,
        string? sensitivityLevel)
    {
        if (string.IsNullOrWhiteSpace(sensitivityLevel) ||
            string.Equals(sensitivityLevel, SensitivityLevels.Normal, StringComparison.Ordinal))
        {
            return AccessDecision.Allow();
        }

        if (string.Equals(sensitivityLevel, RestrictedSensitivityLevel, StringComparison.Ordinal))
        {
            return HasRolePermission(roleProfile, HealthPermissions.ViewRestrictedData)
                ? AccessDecision.Allow(HealthPermissions.ViewRestrictedData)
                : AccessDecision.Deny("Restricted data requires ViewRestrictedData permission.");
        }

        if (string.Equals(sensitivityLevel, SensitivityLevels.Sensitive, StringComparison.Ordinal))
        {
            if (HasRolePermission(roleProfile, HealthPermissions.ViewSensitiveMedicalHistory))
            {
                return AccessDecision.Allow(HealthPermissions.ViewSensitiveMedicalHistory);
            }

            return HasRolePermission(roleProfile, HealthPermissions.ViewRestrictedData)
                ? AccessDecision.Allow(HealthPermissions.ViewRestrictedData)
                : AccessDecision.Deny("Sensitive data requires sensitive or restricted data permission.");
        }

        if (HasRolePermission(roleProfile, HealthPermissions.ViewSensitiveMedicalHistory))
        {
            return AccessDecision.Allow(HealthPermissions.ViewSensitiveMedicalHistory);
        }

        return HasRolePermission(roleProfile, HealthPermissions.ViewRestrictedData)
            ? AccessDecision.Allow(HealthPermissions.ViewRestrictedData)
            : AccessDecision.Deny("Unknown sensitivity level is treated as sensitive.");
    }

    private static bool CanInternalAdminAccessAllPatientsWithoutGrant(
        HealthCoreAuthorizationContext context,
        ProductRoleAccessProfile roleProfile)
    {
        return string.Equals(context.ProductCode, ProductCodes.InternalAdmin, StringComparison.Ordinal) &&
            string.Equals(roleProfile.AccessScope, AccessScopes.AllPatients, StringComparison.Ordinal) &&
            (string.Equals(context.ProductRole, ProductRoles.SuperAdmin, StringComparison.Ordinal) ||
                string.Equals(context.ProductRole, ProductRoles.HealthCoreAdmin, StringComparison.Ordinal) ||
                string.Equals(context.ProductRole, ProductRoles.ReadOnlyAuditor, StringComparison.Ordinal));
    }

    private static bool IsGrantScopeCompatible(
        PatientAccessGrant grant,
        ProductRoleAccessProfile roleProfile,
        bool isEmergencyPermission)
    {
        if (isEmergencyPermission && string.Equals(grant.AccessScope, AccessScopes.EmergencyAccess, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(grant.AccessScope, roleProfile.AccessScope, StringComparison.Ordinal);
    }

    private static bool IsEmergencyGrant(PatientAccessGrant grant)
    {
        return string.Equals(grant.AuthorizationReason, AuthorizationReasons.Emergency, StringComparison.Ordinal) ||
            string.Equals(grant.AccessScope, AccessScopes.EmergencyAccess, StringComparison.Ordinal);
    }
}

using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;
using Zibzie.HealthCore.Infrastructure.Security;

namespace Zibzie.HealthCore.Tests.Security;

public class HealthCoreAuthorizationServiceTests
{
    [Fact]
    public async Task HasPermissionAsync_WithUnknownProductRole_Denies()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.HasPermissionAsync(
            Context(patient.Id, ProductCodes.DigiCare, "UnknownRole"),
            HealthPermissions.ViewPatientProfile,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal("Product role profile was not found.", decision.DenialReason);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenRoleDoesNotIncludePermission_Denies()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var userId = Guid.NewGuid();
        await SeedGrantAsync(
            dbContext,
            patient.Id,
            userId,
            ProductCodes.ClinicQueue,
            ProductRoles.ClinicQueueReceptionist,
            AccessScopes.OrganizationPatients,
            AuthorizationReasons.CareTeamOperation);
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.HasPermissionAsync(
            Context(patient.Id, ProductCodes.ClinicQueue, ProductRoles.ClinicQueueReceptionist, userId),
            HealthPermissions.ViewMedicalHistory,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal("Product role does not include the requested permission.", decision.DenialReason);
    }

    [Fact]
    public async Task HasPermissionAsync_ForNonInternalRoleWithoutGrant_Denies()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.HasPermissionAsync(
            Context(patient.Id, ProductCodes.DigiCare, ProductRoles.DigiCareClinician),
            HealthPermissions.ViewMedicalHistory,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal("No active patient access grant was found.", decision.DenialReason);
    }

    [Fact]
    public async Task HasPermissionAsync_ForDigiCareClinicianWithActiveGrant_Allows()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var userId = Guid.NewGuid();
        var grant = await SeedGrantAsync(
            dbContext,
            patient.Id,
            userId,
            ProductCodes.DigiCare,
            ProductRoles.DigiCareClinician,
            AccessScopes.AssignedPatientsOnly,
            AuthorizationReasons.ActiveCare);
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.HasPermissionAsync(
            Context(patient.Id, ProductCodes.DigiCare, ProductRoles.DigiCareClinician, userId),
            HealthPermissions.ViewMedicalHistory,
            CancellationToken.None);

        Assert.True(decision.IsAllowed);
        Assert.Equal(HealthPermissions.ViewMedicalHistory, decision.MatchedPermission);
        Assert.Equal(AccessScopes.AssignedPatientsOnly, decision.MatchedScope);
        Assert.Equal(grant.Id, decision.MatchedGrantId);
    }

    [Fact]
    public async Task HasPermissionAsync_WithExpiredGrant_Denies()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var patient = await SeedPatientAsync(dbContext);
        var userId = Guid.NewGuid();
        await SeedGrantAsync(
            dbContext,
            patient.Id,
            userId,
            ProductCodes.DigiCare,
            ProductRoles.DigiCareClinician,
            AccessScopes.AssignedPatientsOnly,
            AuthorizationReasons.ActiveCare,
            validFrom: now.AddDays(-10),
            validUntil: now.AddDays(-1));
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.HasPermissionAsync(
            Context(patient.Id, ProductCodes.DigiCare, ProductRoles.DigiCareClinician, userId, now),
            HealthPermissions.ViewMedicalHistory,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal("No active patient access grant was found.", decision.DenialReason);
    }

    [Fact]
    public async Task HasPermissionAsync_WithRevokedGrant_Denies()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var patient = await SeedPatientAsync(dbContext);
        var userId = Guid.NewGuid();
        await SeedGrantAsync(
            dbContext,
            patient.Id,
            userId,
            ProductCodes.DigiCare,
            ProductRoles.DigiCareClinician,
            AccessScopes.AssignedPatientsOnly,
            AuthorizationReasons.ActiveCare,
            revokedAt: now.AddMinutes(-5));
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.HasPermissionAsync(
            Context(patient.Id, ProductCodes.DigiCare, ProductRoles.DigiCareClinician, userId, now),
            HealthPermissions.ViewMedicalHistory,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal("No active patient access grant was found.", decision.DenialReason);
    }

    [Fact]
    public async Task HasPermissionAsync_ForInternalAdminSuperAdminWithoutGrant_Allows()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.HasPermissionAsync(
            Context(patient.Id, ProductCodes.InternalAdmin, ProductRoles.SuperAdmin),
            HealthPermissions.ViewPatientProfile,
            CancellationToken.None);

        Assert.True(decision.IsAllowed);
        Assert.Equal(HealthPermissions.ViewPatientProfile, decision.MatchedPermission);
        Assert.Equal(AccessScopes.AllPatients, decision.MatchedScope);
        Assert.Null(decision.MatchedGrantId);
    }

    [Fact]
    public async Task CanViewPatientSectionAsync_WithRestrictedSensitivityWithoutPermission_Denies()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var userId = Guid.NewGuid();
        await SeedGrantAsync(
            dbContext,
            patient.Id,
            userId,
            ProductCodes.DigiCare,
            ProductRoles.DigiCareClinician,
            AccessScopes.AssignedPatientsOnly,
            AuthorizationReasons.ActiveCare);
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.CanViewPatientSectionAsync(
            Context(patient.Id, ProductCodes.DigiCare, ProductRoles.DigiCareClinician, userId),
            HealthPermissions.ViewMedicalHistory,
            "Restricted",
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal("Restricted data requires ViewRestrictedData permission.", decision.DenialReason);
    }

    [Fact]
    public async Task CanViewPatientSectionAsync_WithRestrictedSensitivityAndInternalAdminPermission_Allows()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new HealthCoreAuthorizationService(dbContext);

        var decision = await service.CanViewPatientSectionAsync(
            Context(patient.Id, ProductCodes.InternalAdmin, ProductRoles.HealthCoreAdmin),
            HealthPermissions.ViewMedicalHistory,
            "Restricted",
            CancellationToken.None);

        Assert.True(decision.IsAllowed);
        Assert.Equal(HealthPermissions.ViewMedicalHistory, decision.MatchedPermission);
        Assert.Equal(AccessScopes.AllPatients, decision.MatchedScope);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static HealthCoreAuthorizationContext Context(
        Guid patientId,
        string productCode,
        string productRole,
        Guid? userId = null,
        DateTimeOffset? now = null)
    {
        return new HealthCoreAuthorizationContext
        {
            PatientId = patientId,
            UserId = userId ?? Guid.NewGuid(),
            ProductCode = productCode,
            ProductRole = productRole,
            Now = now
        };
    }

    private static async Task<PatientProfile> SeedPatientAsync(AppDbContext dbContext)
    {
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Patient",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PatientProfiles.Add(patient);
        await dbContext.SaveChangesAsync();

        return patient;
    }

    private static async Task<PatientAccessGrant> SeedGrantAsync(
        AppDbContext dbContext,
        Guid patientId,
        Guid userId,
        string productCode,
        string productRole,
        string accessScope,
        string authorizationReason,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null,
        DateTimeOffset? revokedAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        var grant = new PatientAccessGrant
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            UserId = userId,
            ProductCode = productCode,
            ProductRole = productRole,
            AccessScope = accessScope,
            AuthorizationReason = authorizationReason,
            ValidFrom = validFrom ?? now.AddDays(-1),
            ValidUntil = validUntil,
            GrantedByUserId = Guid.NewGuid(),
            GrantedAt = now.AddDays(-1),
            RevokedAt = revokedAt,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = revokedAt
        };

        dbContext.PatientAccessGrants.Add(grant);
        await dbContext.SaveChangesAsync();

        return grant;
    }
}

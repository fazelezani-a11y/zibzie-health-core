using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;
using Zibzie.HealthCore.Infrastructure.Security;

namespace Zibzie.HealthCore.Tests.Security;

public class PatientAccessGrantServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidServiceGrant_CreatesGrant()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new PatientAccessGrantService(dbContext);

        var result = await service.CreateAsync(
            patient.Id,
            CreateServiceGrantRequest(),
            InternalAdminContext());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(patient.Id, result.Value.PatientId);
        Assert.Equal("digicare-careteam-service", result.Value.ServiceAccountId);
        Assert.Equal(ProductCodes.DigiCare, result.Value.ProductCode);
        Assert.Equal(ProductRoles.DigiCareCaseManager, result.Value.ProductRole);
        Assert.Equal(AccessScopes.AssignedPatientsOnly, result.Value.Scope);
        Assert.Equal(AuthorizationReasons.CareTeamOperation, result.Value.Reason);
        Assert.True(result.Value.IsActive);

        var persisted = await dbContext.PatientAccessGrants.SingleAsync();
        Assert.Equal(result.Value.Id, persisted.Id);
        Assert.Equal(InternalAdminContext().UserId, persisted.GrantedByUserId);
    }

    [Fact]
    public async Task CreateAsync_WhenGranteeIsMissing_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new PatientAccessGrantService(dbContext);
        var request = CreateServiceGrantRequest();
        request.ServiceAccountId = null;

        var result = await service.CreateAsync(patient.Id, request, InternalAdminContext());

        Assert.False(result.Succeeded);
        Assert.Equal(PatientAccessGrantServiceError.Validation, result.Error);
        Assert.Equal("GranteeUserId or ServiceAccountId is required.", result.ErrorMessage);
        Assert.Empty(dbContext.PatientAccessGrants);
    }

    [Fact]
    public async Task CreateAsync_WhenProductCodeIsInternalAdmin_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new PatientAccessGrantService(dbContext);
        var request = CreateServiceGrantRequest();
        request.ProductCode = ProductCodes.InternalAdmin;
        request.ProductRole = ProductRoles.HealthCoreAdmin;
        request.Scope = AccessScopes.AllPatients;
        request.Reason = AuthorizationReasons.InternalAdmin;

        var result = await service.CreateAsync(patient.Id, request, InternalAdminContext());

        Assert.False(result.Succeeded);
        Assert.Equal(PatientAccessGrantServiceError.Validation, result.Error);
        Assert.Equal("InternalAdmin access grants cannot be created through this workflow.", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateActiveGrantExists_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new PatientAccessGrantService(dbContext);

        var first = await service.CreateAsync(patient.Id, CreateServiceGrantRequest(), InternalAdminContext());
        var second = await service.CreateAsync(patient.Id, CreateServiceGrantRequest(), InternalAdminContext());

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(PatientAccessGrantServiceError.Conflict, second.Error);
        Assert.Single(dbContext.PatientAccessGrants);
    }

    [Fact]
    public async Task CreateAsync_WhenValidityWindowIsInvalid_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new PatientAccessGrantService(dbContext);
        var now = DateTimeOffset.UtcNow;
        var request = CreateServiceGrantRequest();
        request.ValidFrom = now.AddHours(1);
        request.ValidUntil = now;

        var result = await service.CreateAsync(patient.Id, request, InternalAdminContext());

        Assert.False(result.Succeeded);
        Assert.Equal(PatientAccessGrantServiceError.Validation, result.Error);
        Assert.Equal("ValidUntil must be after ValidFrom.", result.ErrorMessage);
    }

    [Fact]
    public async Task RevokeAsync_WithActiveGrant_RevokesGrant()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new PatientAccessGrantService(dbContext);
        var created = await service.CreateAsync(patient.Id, CreateServiceGrantRequest(), InternalAdminContext());

        var revoked = await service.RevokeAsync(
            created.Value!.Id,
            new RevokePatientAccessGrantRequest
            {
                Reason = "No longer assigned."
            },
            InternalAdminContext());

        Assert.True(revoked.Succeeded);
        Assert.NotNull(revoked.Value!.RevokedAt);
        Assert.False(revoked.Value.IsActive);
        Assert.Equal("No longer assigned.", revoked.Value.RevokeReason);

        var persisted = await dbContext.PatientAccessGrants.SingleAsync();
        Assert.NotNull(persisted.RevokedAt);
        Assert.Equal(InternalAdminContext().UserId, persisted.RevokedByUserId);
    }

    [Fact]
    public async Task RevokeAsync_WhenGrantDoesNotExist_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var service = new PatientAccessGrantService(dbContext);

        var result = await service.RevokeAsync(
            Guid.NewGuid(),
            new RevokePatientAccessGrantRequest(),
            InternalAdminContext());

        Assert.False(result.Succeeded);
        Assert.Equal(PatientAccessGrantServiceError.NotFound, result.Error);
    }

    [Fact]
    public async Task RevokeAsync_WhenGrantAlreadyRevoked_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var service = new PatientAccessGrantService(dbContext);
        var created = await service.CreateAsync(patient.Id, CreateServiceGrantRequest(), InternalAdminContext());

        await service.RevokeAsync(created.Value!.Id, new RevokePatientAccessGrantRequest(), InternalAdminContext());
        var secondRevoke = await service.RevokeAsync(created.Value.Id, new RevokePatientAccessGrantRequest(), InternalAdminContext());

        Assert.False(secondRevoke.Succeeded);
        Assert.Equal(PatientAccessGrantServiceError.Conflict, secondRevoke.Error);
        Assert.Equal("Patient access grant is already revoked.", secondRevoke.ErrorMessage);
    }

    [Fact]
    public async Task AuthorizationService_AllowsServiceAccessAfterGrantCreation()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var grantService = new PatientAccessGrantService(dbContext);
        await grantService.CreateAsync(patient.Id, CreateServiceGrantRequest(), InternalAdminContext());
        var authorizationService = new HealthCoreAuthorizationService(dbContext);

        var decision = await authorizationService.HasPermissionAsync(
            new HealthCoreAuthorizationContext
            {
                PatientId = patient.Id,
                ServiceAccountId = "digicare-careteam-service",
                ProductCode = ProductCodes.DigiCare,
                ProductRole = ProductRoles.DigiCareCaseManager
            },
            HealthPermissions.ViewPatientSummary);

        Assert.True(decision.IsAllowed);
        Assert.Equal(HealthPermissions.ViewPatientSummary, decision.MatchedPermission);
        Assert.Equal(AccessScopes.AssignedPatientsOnly, decision.MatchedScope);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<PatientProfile> SeedPatientAsync(AppDbContext dbContext)
    {
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            FirstName = "Grant",
            LastName = "Patient",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PatientProfiles.Add(patient);
        await dbContext.SaveChangesAsync();

        return patient;
    }

    private static CreatePatientAccessGrantRequest CreateServiceGrantRequest()
    {
        return new CreatePatientAccessGrantRequest
        {
            ServiceAccountId = "digicare-careteam-service",
            ProductCode = ProductCodes.DigiCare,
            ProductRole = ProductRoles.DigiCareCaseManager,
            Scope = AccessScopes.AssignedPatientsOnly,
            Reason = AuthorizationReasons.CareTeamOperation
        };
    }

    private static HealthCoreRequestContext InternalAdminContext()
    {
        return new HealthCoreRequestContext
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = ProductRoles.HealthCoreAdmin
        };
    }
}

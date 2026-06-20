using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Application.Patients;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.Patients;

public class PatientWriteAuthorizationTests
{
    [Fact]
    public async Task CreatePatient_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCreatePatient()
    {
        await using var dbContext = CreateDbContext();
        var auditLog = new FakeAuditLogService();
        var authorizationService = new FakeAuthorizationService(AccessDecision.Deny("No create access."));
        var controller = CreateController(dbContext, authorizationService, auditLog);

        var result = await controller.CreatePatient(CreatePatientRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(HealthPermissions.CreatePatient, authorizationService.LastPermission);
        Assert.Empty(dbContext.PatientProfiles);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreatePatient, auditRequest.Permission);
        Assert.Null(auditRequest.PatientId);
        Assert.Null(auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No create access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreatePatient_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.CreatePatient, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreatePatient(CreatePatientRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<PatientDetailsDto>(createdResult.Value);
        Assert.Equal("Test", dto.FirstName);
        Assert.Equal("Patient", dto.LastName);
        Assert.Equal("09120000000", dto.MobileNumber);
        Assert.True(dto.IsActive);

        var patient = Assert.Single(dbContext.PatientProfiles.Include(x => x.ContactInfo));
        Assert.Equal(dto.Id, patient.Id);
        Assert.NotNull(patient.ContactInfo);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreatePatient, auditRequest.Permission);
        Assert.Equal(dto.Id, auditRequest.PatientId);
        Assert.Equal(dto.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task UpdatePatient_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotUpdatePatient()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var authorizationService = new FakeAuthorizationService(AccessDecision.Deny("No edit access."));
        var controller = CreateController(dbContext, authorizationService, auditLog);

        var request = UpdatePatientRequest(firstName: "Updated", mobileNumber: "09129999999");
        var result = await controller.UpdatePatient(patient.Id, request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(HealthPermissions.EditPatientProfile, authorizationService.LastPermission);
        Assert.Equal(patient.Id, authorizationService.LastContext?.PatientId);

        var persistedPatient = await dbContext.PatientProfiles
            .Include(x => x.ContactInfo)
            .SingleAsync(x => x.Id == patient.Id);
        Assert.Equal("Test", persistedPatient.FirstName);
        Assert.Equal("09120000000", persistedPatient.ContactInfo?.MobileNumber);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditPatientProfile, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(patient.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No edit access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task UpdatePatient_WhenAuthorizationAllowed_ReturnsUpdatedPatientAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.EditPatientProfile, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.UpdatePatient(patient.Id, UpdatePatientRequest(firstName: "Updated"));

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PatientDetailsDto>(okResult.Value);
        Assert.Equal(patient.Id, dto.Id);
        Assert.Equal("Updated", dto.FirstName);
        Assert.Equal("09120000000", dto.MobileNumber);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Update, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditPatientProfile, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(patient.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task DeactivatePatient_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotDeactivate()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var authorizationService = new FakeAuthorizationService(AccessDecision.Deny("No deactivate access."));
        var controller = CreateController(dbContext, authorizationService, auditLog);

        var result = await controller.DeactivatePatient(patient.Id);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(HealthPermissions.DeactivatePatient, authorizationService.LastPermission);
        Assert.Equal(patient.Id, authorizationService.LastContext?.PatientId);

        var persistedPatient = await dbContext.PatientProfiles.SingleAsync(x => x.Id == patient.Id);
        Assert.True(persistedPatient.IsActive);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.DeactivatePatient, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(patient.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No deactivate access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task DeactivatePatient_WhenAuthorizationAllowed_ReturnsNoContentAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.DeactivatePatient, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.DeactivatePatient(patient.Id);

        Assert.IsType<NoContentResult>(result);

        var persistedPatient = await dbContext.PatientProfiles.SingleAsync(x => x.Id == patient.Id);
        Assert.False(persistedPatient.IsActive);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Update, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.DeactivatePatient, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(patient.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    private static PatientsController CreateController(
        AppDbContext dbContext,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new PatientsController(
            dbContext,
            new FakePatientSummaryService(),
            authorizationService,
            new FakeRequestContextProvider(),
            auditLogService);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static CreatePatientRequest CreatePatientRequest()
    {
        return new CreatePatientRequest
        {
            FirstName = "Test",
            LastName = "Patient",
            BirthDate = new DateOnly(1984, 1, 1),
            NationalCode = "0012345678",
            Gender = "Male",
            BloodType = "O+",
            MobileNumber = "09120000000",
            Email = "test@example.com",
            EmergencyContactName = "Emergency",
            EmergencyContactPhone = "09121111111",
            HomeAddress = "home address",
            WorkAddress = "work address"
        };
    }

    private static UpdatePatientRequest UpdatePatientRequest(
        string firstName = "Test",
        string mobileNumber = "09120000000")
    {
        return new UpdatePatientRequest
        {
            FirstName = firstName,
            LastName = "Patient",
            BirthDate = new DateOnly(1984, 1, 1),
            NationalCode = "0012345678",
            Gender = "Male",
            BloodType = "O+",
            MobileNumber = mobileNumber,
            Email = "test@example.com",
            EmergencyContactName = "Emergency",
            EmergencyContactPhone = "09121111111",
            HomeAddress = "home address",
            WorkAddress = "work address"
        };
    }

    private static async Task<PatientProfile> SeedPatientAsync(AppDbContext dbContext)
    {
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Patient",
            BirthDate = new DateOnly(1984, 1, 1),
            NationalCode = "0012345678",
            Gender = "Male",
            BloodType = "O+",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ContactInfo = new ContactInfo
            {
                Id = Guid.NewGuid(),
                MobileNumber = "09120000000",
                Email = "test@example.com",
                EmergencyContactName = "Emergency",
                EmergencyContactPhone = "09121111111",
                HomeAddress = "home address",
                WorkAddress = "work address",
                CreatedAt = DateTime.UtcNow
            }
        };

        dbContext.PatientProfiles.Add(patient);
        await dbContext.SaveChangesAsync();

        return patient;
    }

    private sealed class FakePatientSummaryService : IPatientSummaryService
    {
        public Task<PatientSummaryDto?> GetPatientSummaryAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PatientSummaryDto?>(null);
        }
    }

    private sealed class FakeAuthorizationService : IHealthCoreAuthorizationService
    {
        private readonly AccessDecision _decision;

        public FakeAuthorizationService(AccessDecision decision)
        {
            _decision = decision;
        }

        public string? LastPermission { get; private set; }

        public HealthCoreAuthorizationContext? LastContext { get; private set; }

        public Task<AccessDecision> CanAccessPatientAsync(
            HealthCoreAuthorizationContext context,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> HasPermissionAsync(
            HealthCoreAuthorizationContext context,
            string permission,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            LastPermission = permission;
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanViewPatientSectionAsync(
            HealthCoreAuthorizationContext context,
            string sectionPermission,
            string? sensitivityLevel = null,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            LastPermission = sectionPermission;
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanEditPatientSectionAsync(
            HealthCoreAuthorizationContext context,
            string sectionPermission,
            string? sensitivityLevel = null,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            LastPermission = sectionPermission;
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanViewSensitivityLevelAsync(
            HealthCoreAuthorizationContext context,
            string? sensitivityLevel,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            return Task.FromResult(_decision);
        }
    }

    private sealed class FakeRequestContextProvider : IHealthCoreRequestContextProvider
    {
        private readonly HealthCoreRequestContext _requestContext = new()
        {
            ServiceAccountId = "test-service",
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = ProductRoles.HealthCoreAdmin,
            CorrelationId = "test-correlation",
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            RequestPath = "/api/test",
            HttpMethod = "POST",
            IsFallbackContext = true
        };

        public HealthCoreRequestContext GetCurrent()
        {
            return _requestContext;
        }

        public HealthCoreAuthorizationContext CreateAuthorizationContext(Guid patientId)
        {
            return new HealthCoreAuthorizationContext
            {
                UserId = _requestContext.UserId,
                ServiceAccountId = _requestContext.ServiceAccountId,
                PatientId = patientId,
                ProductCode = _requestContext.ProductCode ?? string.Empty,
                ProductRole = _requestContext.ProductRole ?? string.Empty
            };
        }
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<AuditLogRequest> Requests { get; } = new();

        public Task LogAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }
}

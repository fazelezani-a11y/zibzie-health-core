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

public class PatientReadAuthorizationTests
{
    [Fact]
    public async Task GetPatients_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var auditLog = new FakeAuditLogService();
        var authorizationService = new FakeAuthorizationService(AccessDecision.Deny("No directory access."));
        var controller = CreateController(dbContext, authorizationService, auditLog);

        var result = await controller.GetPatients(search: "0912", page: 1, pageSize: 20);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(HealthPermissions.ViewPatientDirectory, authorizationService.LastPermission);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewPatientDirectory, auditRequest.Permission);
        Assert.Null(auditRequest.PatientId);
        Assert.Null(auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No directory access.", auditRequest.FailureReason);
        Assert.Contains("\"hasSearch\":true", auditRequest.MetadataJson);
    }

    [Fact]
    public async Task GetPatients_WhenAuthorizationAllowed_ReturnsListAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewPatientDirectory, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatients();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var patients = Assert.IsType<List<PatientListItemDto>>(okResult.Value);
        var dto = Assert.Single(patients);
        Assert.Equal(patient.Id, dto.Id);
        Assert.Equal("Test Patient", dto.FullName);
        Assert.Equal("0012345678", dto.NationalCode);
        Assert.Equal("09120000000", dto.MobileNumber);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewPatientDirectory, auditRequest.Permission);
        Assert.Null(auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
        Assert.Contains("\"hasSearch\":false", auditRequest.MetadataJson);
    }

    [Fact]
    public async Task GetPatientById_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var authorizationService = new FakeAuthorizationService(AccessDecision.Deny("No profile access."));
        var controller = CreateController(dbContext, authorizationService, auditLog);

        var result = await controller.GetPatientById(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(HealthPermissions.ViewPatientProfile, authorizationService.LastPermission);
        Assert.Equal(patientId, authorizationService.LastContext?.PatientId);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewPatientProfile, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.Equal(patientId, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No profile access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientById_WhenAuthorizationAllowed_ReturnsDetailsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewPatientProfile, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientById(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PatientDetailsDto>(okResult.Value);
        Assert.Equal(patient.Id, dto.Id);
        Assert.Equal("Test", dto.FirstName);
        Assert.Equal("Patient", dto.LastName);
        Assert.Equal("0012345678", dto.NationalCode);
        Assert.Equal("09120000000", dto.MobileNumber);
        Assert.Equal("home address", dto.HomeAddress);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientProfile, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewPatientProfile, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(patient.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
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
            HttpMethod = "GET",
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

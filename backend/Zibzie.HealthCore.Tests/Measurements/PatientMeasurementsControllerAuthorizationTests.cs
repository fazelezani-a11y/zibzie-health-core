using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.Measurements;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.Measurements;

public class PatientMeasurementsControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientMeasurements_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientMeasurementService(),
            new FakeAuthorizationService(AccessDecision.Deny("No measurement access.")),
            auditLog);

        var result = await controller.GetPatientMeasurements(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Measurement, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewMeasurements, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No measurement access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreatePatientMeasurement_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCallService()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var createService = new FakePatientMeasurementService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            createService,
            new FakeAuthorizationService(AccessDecision.Deny("No measurement create access.")),
            auditLog);

        var result = await controller.CreatePatientMeasurement(patientId, CreateRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Measurement, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateMeasurement, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No measurement create access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientMeasurements_WhenAuthorizationAllowed_ReturnsMeasurementsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var measurement = await SeedMeasurementAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientMeasurementService(),
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewMeasurements, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientMeasurements(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var measurements = Assert.IsType<List<PatientMeasurementDto>>(okResult.Value);
        var dto = Assert.Single(measurements);
        Assert.Equal(measurement.Id, dto.Id);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Measurement, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewMeasurements, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreatePatientMeasurement_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var measurementId = Guid.NewGuid();
        var createService = new FakePatientMeasurementService
        {
            Result = new PatientMeasurementDto
            {
                Id = measurementId,
                PatientProfileId = patientId,
                MeasurementType = MeasurementTypes.Weight,
                DisplayName = "Weight",
                Value = 72.5m,
                Unit = "kg",
                MeasuredAt = DateTimeOffset.UtcNow,
                SourceType = SourceTypes.Manual,
                VerificationStatus = VerificationStatuses.Unverified,
                SensitivityLevel = SensitivityLevels.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            createService,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.CreateMeasurement, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreatePatientMeasurement(patientId, CreateRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<PatientMeasurementDto>(createdResult.Value);
        Assert.Equal(measurementId, dto.Id);
        Assert.Equal(1, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Measurement, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateMeasurement, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.Equal(measurementId, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task UpdatePatientMeasurement_WhenAuthorizationDenied_AuditsResolvedPatientAndMeasurementIds()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var measurement = await SeedMeasurementAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientMeasurementService(),
            new FakeAuthorizationService(AccessDecision.Deny("No measurement edit access.")),
            auditLog);

        var result = await controller.UpdatePatientMeasurement(measurement.Id, new UpdatePatientMeasurementRequest
        {
            Value = 73.1m
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Measurement, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditMeasurement, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(measurement.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
    }

    private static PatientMeasurementsController CreateController(
        AppDbContext dbContext,
        FakePatientMeasurementService createService,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new PatientMeasurementsController(
            dbContext,
            createService,
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

    private static CreatePatientMeasurementRequest CreateRequest()
    {
        return new CreatePatientMeasurementRequest
        {
            MeasurementType = MeasurementTypes.Weight,
            DisplayName = "Weight",
            Value = 72.5m,
            Unit = "kg",
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Normal
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

    private static async Task<PatientMeasurement> SeedMeasurementAsync(
        AppDbContext dbContext,
        Guid patientId)
    {
        var measurement = new PatientMeasurement
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            MeasurementType = MeasurementTypes.Weight,
            DisplayName = "Weight",
            Value = 72.5m,
            Unit = "kg",
            MeasuredAt = DateTimeOffset.UtcNow,
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Normal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.PatientMeasurements.Add(measurement);
        await dbContext.SaveChangesAsync();

        return measurement;
    }

    private sealed class FakePatientMeasurementService : IPatientMeasurementService
    {
        public int CallCount { get; private set; }

        public PatientMeasurementDto? Result { get; init; }

        public Task<PatientMeasurementDto?> CreatePatientMeasurementAsync(
            Guid patientId,
            CreatePatientMeasurementRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAuthorizationService : IHealthCoreAuthorizationService
    {
        private readonly AccessDecision _decision;

        public FakeAuthorizationService(AccessDecision decision)
        {
            _decision = decision;
        }

        public Task<AccessDecision> CanAccessPatientAsync(
            HealthCoreAuthorizationContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> HasPermissionAsync(
            HealthCoreAuthorizationContext context,
            string permission,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanViewPatientSectionAsync(
            HealthCoreAuthorizationContext context,
            string sectionPermission,
            string? sensitivityLevel = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanEditPatientSectionAsync(
            HealthCoreAuthorizationContext context,
            string sectionPermission,
            string? sensitivityLevel = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanViewSensitivityLevelAsync(
            HealthCoreAuthorizationContext context,
            string? sensitivityLevel,
            CancellationToken cancellationToken = default)
        {
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

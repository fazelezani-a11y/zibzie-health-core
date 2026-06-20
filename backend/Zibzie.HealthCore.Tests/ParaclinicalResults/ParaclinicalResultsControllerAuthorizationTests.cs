using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.ParaclinicalResults;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.ParaclinicalResults;

public class ParaclinicalResultsControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientParaclinicalResults_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeParaclinicalResultService(),
            new FakeAuthorizationService(AccessDecision.Deny("No paraclinical access.")),
            auditLog);

        var result = await controller.GetPatientParaclinicalResults(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.ParaclinicalResult, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewParaclinicalResults, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No paraclinical access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreateParaclinicalResult_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCallService()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var createService = new FakeParaclinicalResultService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            createService,
            new FakeAuthorizationService(AccessDecision.Deny("No paraclinical edit access.")),
            auditLog);

        var result = await controller.CreateParaclinicalResult(patientId, CreateRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(HealthPermissions.EditParaclinicalResults, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No paraclinical edit access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientParaclinicalResults_WhenAuthorizationAllowed_ReturnsResultsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var paraclinicalResult = await SeedParaclinicalResultAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeParaclinicalResultService(),
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewParaclinicalResults, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientParaclinicalResults(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var results = Assert.IsType<List<ParaclinicalResultDto>>(okResult.Value);
        var dto = Assert.Single(results);
        Assert.Equal(paraclinicalResult.Id, dto.Id);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.ParaclinicalResult, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewParaclinicalResults, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreateParaclinicalResult_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var createService = new FakeParaclinicalResultService
        {
            Result = CreateParaclinicalResultResult.Created(new ParaclinicalResultDto
            {
                Id = resultId,
                PatientProfileId = patientId,
                ResultType = "Lab",
                Title = "CBC",
                SourceType = SourceTypes.Manual,
                VerificationStatus = VerificationStatuses.Unverified,
                SensitivityLevel = SensitivityLevels.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            })
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            createService,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.EditParaclinicalResults, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreateParaclinicalResult(patientId, CreateRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ParaclinicalResultDto>(createdResult.Value);
        Assert.Equal(resultId, dto.Id);
        Assert.Equal(1, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.ParaclinicalResult, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditParaclinicalResults, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.Equal(resultId, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task GetParaclinicalResult_WhenAuthorizationDenied_AuditsResolvedPatientAndResultIds()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var paraclinicalResult = await SeedParaclinicalResultAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeParaclinicalResultService(),
            new FakeAuthorizationService(AccessDecision.Deny("No result detail access.")),
            auditLog);

        var result = await controller.GetParaclinicalResult(paraclinicalResult.Id);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(HealthPermissions.ViewParaclinicalResults, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(paraclinicalResult.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
    }

    private static ParaclinicalResultsController CreateController(
        AppDbContext dbContext,
        FakeParaclinicalResultService createService,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new ParaclinicalResultsController(
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

    private static CreateParaclinicalResultRequest CreateRequest()
    {
        return new CreateParaclinicalResultRequest
        {
            ResultType = "Lab",
            Title = "CBC",
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

    private static async Task<PatientParaclinicalResult> SeedParaclinicalResultAsync(
        AppDbContext dbContext,
        Guid patientId)
    {
        var result = new PatientParaclinicalResult
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            ResultType = "Lab",
            Title = "CBC",
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Normal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.PatientParaclinicalResults.Add(result);
        await dbContext.SaveChangesAsync();

        return result;
    }

    private sealed class FakeParaclinicalResultService : IParaclinicalResultService
    {
        public int CallCount { get; private set; }

        public CreateParaclinicalResultResult Result { get; init; } =
            CreateParaclinicalResultResult.PatientNotFound();

        public Task<CreateParaclinicalResultResult> CreateParaclinicalResultAsync(
            Guid patientId,
            CreateParaclinicalResultRequest request,
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

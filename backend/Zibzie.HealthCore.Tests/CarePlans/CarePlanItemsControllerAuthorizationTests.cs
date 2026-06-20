using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.CarePlans;

public class CarePlanItemsControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientCarePlan_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeCarePlanItemService(),
            new FakeAuthorizationService(AccessDecision.Deny("No care plan access.")),
            auditLog);

        var result = await controller.GetPatientCarePlan(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.CarePlanItem, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewCarePlan, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No care plan access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreateCarePlanItem_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCallService()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var createService = new FakeCarePlanItemService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            createService,
            new FakeAuthorizationService(AccessDecision.Deny("No care plan create access.")),
            auditLog);

        var result = await controller.CreateCarePlanItem(patientId, CreateRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.CarePlanItem, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateCarePlanItem, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No care plan create access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientCarePlan_WhenAuthorizationAllowed_ReturnsItemsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var carePlanItem = await SeedCarePlanItemAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeCarePlanItemService(),
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewCarePlan, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientCarePlan(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsType<List<CarePlanItemDto>>(okResult.Value);
        var dto = Assert.Single(items);
        Assert.Equal(carePlanItem.Id, dto.Id);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.CarePlanItem, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewCarePlan, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreateCarePlanItem_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var createService = new FakeCarePlanItemService
        {
            Result = new CarePlanItemDto
            {
                Id = itemId,
                PatientProfileId = patientId,
                Category = CarePlanCategories.FollowUp,
                ItemType = CarePlanItemTypes.Visit,
                Title = "Follow-up visit",
                Status = CarePlanStatuses.Planned,
                Priority = CommonPriorities.High,
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
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.CreateCarePlanItem, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreateCarePlanItem(patientId, CreateRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<CarePlanItemDto>(createdResult.Value);
        Assert.Equal(itemId, dto.Id);
        Assert.Equal(1, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.CarePlanItem, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateCarePlanItem, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.Equal(itemId, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task UpdateCarePlanItem_WhenAuthorizationDenied_AuditsResolvedPatientAndItemIds()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var carePlanItem = await SeedCarePlanItemAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeCarePlanItemService(),
            new FakeAuthorizationService(AccessDecision.Deny("No complete care plan access.")),
            auditLog);

        var result = await controller.UpdateCarePlanItem(carePlanItem.Id, new UpdateCarePlanItemRequest
        {
            Status = CarePlanStatuses.Completed
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.CarePlanItem, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CompleteCarePlanItem, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(carePlanItem.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
    }

    private static CarePlanItemsController CreateController(
        AppDbContext dbContext,
        FakeCarePlanItemService createService,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new CarePlanItemsController(
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

    private static CreateCarePlanItemRequest CreateRequest()
    {
        return new CreateCarePlanItemRequest
        {
            Category = CarePlanCategories.FollowUp,
            ItemType = CarePlanItemTypes.Visit,
            Title = "Follow-up visit",
            Status = CarePlanStatuses.Planned,
            Priority = CommonPriorities.High,
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

    private static async Task<CarePlanItem> SeedCarePlanItemAsync(
        AppDbContext dbContext,
        Guid patientId)
    {
        var item = new CarePlanItem
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            Category = CarePlanCategories.FollowUp,
            ItemType = CarePlanItemTypes.Visit,
            Title = "Follow-up visit",
            Status = CarePlanStatuses.Planned,
            Priority = CommonPriorities.High,
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Normal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.CarePlanItems.Add(item);
        await dbContext.SaveChangesAsync();

        return item;
    }

    private sealed class FakeCarePlanItemService : ICarePlanItemService
    {
        public int CallCount { get; private set; }

        public CarePlanItemDto? Result { get; init; }

        public Task<CarePlanItemDto?> CreateCarePlanItemAsync(
            Guid patientId,
            CreateCarePlanItemRequest request,
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

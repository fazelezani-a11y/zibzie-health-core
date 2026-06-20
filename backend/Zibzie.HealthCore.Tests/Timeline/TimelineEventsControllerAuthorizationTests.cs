using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Application.Timeline;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.Timeline;

public class TimelineEventsControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientTimeline_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No timeline access.")),
            auditLog);

        var result = await controller.GetPatientTimeline(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.TimelineEvent, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewTimeline, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No timeline access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientTimeline_WhenAuthorizationAllowed_ReturnsEventsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var timelineEvent = await SeedTimelineEventAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewTimeline, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientTimeline(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsType<List<TimelineEventDto>>(okResult.Value);
        var dto = Assert.Single(events);
        Assert.Equal(timelineEvent.Id, dto.Id);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.TimelineEvent, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewTimeline, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreateTimelineEvent_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCreateEvent()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No timeline create access.")),
            auditLog);

        var result = await controller.CreateTimelineEvent(patientId, CreateRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Empty(await dbContext.PatientTimelineEvents.ToListAsync());

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.TimelineEvent, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateTimelineEvent, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No timeline create access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreateTimelineEvent_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.CreateTimelineEvent, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreateTimelineEvent(patient.Id, CreateRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<TimelineEventDto>(createdResult.Value);
        Assert.Equal("Manual note", dto.Title);
        Assert.Single(await dbContext.PatientTimelineEvents.ToListAsync());

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.TimelineEvent, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateTimelineEvent, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(dto.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task UpdateTimelineEvent_WhenAuthorizationDenied_AuditsResolvedPatientAndEventIds()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var timelineEvent = await SeedTimelineEventAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No timeline edit access.")),
            auditLog);

        var result = await controller.UpdateTimelineEvent(timelineEvent.Id, new UpdateTimelineEventRequest
        {
            Title = "Updated note"
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.TimelineEvent, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditTimelineEvent, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(timelineEvent.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
    }

    private static TimelineEventsController CreateController(
        AppDbContext dbContext,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new TimelineEventsController(
            dbContext,
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

    private static CreateTimelineEventRequest CreateRequest()
    {
        return new CreateTimelineEventRequest
        {
            EventType = TimelineEventTypes.CarePlan,
            Title = "Manual note",
            SourceType = SourceTypes.Manual,
            Visibility = VisibilityValues.Internal,
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

    private static async Task<PatientTimelineEvent> SeedTimelineEventAsync(
        AppDbContext dbContext,
        Guid patientId)
    {
        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = TimelineEventTypes.CarePlan,
            Title = "Manual note",
            OccurredAt = DateTimeOffset.UtcNow,
            SourceType = SourceTypes.Manual,
            Visibility = VisibilityValues.Internal,
            SensitivityLevel = SensitivityLevels.Normal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.PatientTimelineEvents.Add(timelineEvent);
        await dbContext.SaveChangesAsync();

        return timelineEvent;
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

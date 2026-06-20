using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.Reminders;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.Reminders;

public class PatientRemindersControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientReminders_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientReminderService(),
            new FakeAuthorizationService(AccessDecision.Deny("No reminder access.")),
            auditLog);

        var result = await controller.GetPatientReminders(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Reminder, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewReminders, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No reminder access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreatePatientReminder_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCallService()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var createService = new FakePatientReminderService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            createService,
            new FakeAuthorizationService(AccessDecision.Deny("No reminder create access.")),
            auditLog);

        var result = await controller.CreatePatientReminder(patientId, CreateRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Reminder, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateReminder, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No reminder create access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientReminders_WhenAuthorizationAllowed_ReturnsRemindersAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var reminder = await SeedReminderAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientReminderService(),
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewReminders, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientReminders(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var reminders = Assert.IsType<List<PatientReminderDto>>(okResult.Value);
        var dto = Assert.Single(reminders);
        Assert.Equal(reminder.Id, dto.Id);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Reminder, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewReminders, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreatePatientReminder_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var reminderId = Guid.NewGuid();
        var createService = new FakePatientReminderService
        {
            Result = new PatientReminderDto
            {
                Id = reminderId,
                PatientProfileId = patientId,
                ReminderType = ReminderTypes.FollowUp,
                Title = "Follow-up reminder",
                DueAt = DateTimeOffset.UtcNow.AddDays(2),
                Status = ReminderStatuses.Pending,
                Priority = CommonPriorities.High,
                Audience = AudienceTypes.Internal,
                SourceType = SourceTypes.Manual,
                SensitivityLevel = SensitivityLevels.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            createService,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.CreateReminder, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreatePatientReminder(patientId, CreateRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<PatientReminderDto>(createdResult.Value);
        Assert.Equal(reminderId, dto.Id);
        Assert.Equal(1, createService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Reminder, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreateReminder, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.Equal(reminderId, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task UpdatePatientReminder_WhenAuthorizationDenied_AuditsResolvedPatientAndReminderIds()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var reminder = await SeedReminderAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientReminderService(),
            new FakeAuthorizationService(AccessDecision.Deny("No complete reminder access.")),
            auditLog);

        var result = await controller.UpdatePatientReminder(reminder.Id, new UpdatePatientReminderRequest
        {
            Status = ReminderStatuses.Done
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Reminder, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CompleteReminder, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(reminder.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
    }

    private static PatientRemindersController CreateController(
        AppDbContext dbContext,
        FakePatientReminderService createService,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new PatientRemindersController(
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

    private static CreatePatientReminderRequest CreateRequest()
    {
        return new CreatePatientReminderRequest
        {
            ReminderType = ReminderTypes.FollowUp,
            Title = "Follow-up reminder",
            DueAt = DateTimeOffset.UtcNow.AddDays(2),
            Status = ReminderStatuses.Pending,
            Priority = CommonPriorities.High,
            Audience = AudienceTypes.Internal,
            SourceType = SourceTypes.Manual,
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

    private static async Task<PatientReminder> SeedReminderAsync(
        AppDbContext dbContext,
        Guid patientId)
    {
        var reminder = new PatientReminder
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            ReminderType = ReminderTypes.FollowUp,
            Title = "Follow-up reminder",
            DueAt = DateTimeOffset.UtcNow.AddDays(2),
            Status = ReminderStatuses.Pending,
            Priority = CommonPriorities.High,
            Audience = AudienceTypes.Internal,
            SourceType = SourceTypes.Manual,
            SensitivityLevel = SensitivityLevels.Normal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.PatientReminders.Add(reminder);
        await dbContext.SaveChangesAsync();

        return reminder;
    }

    private sealed class FakePatientReminderService : IPatientReminderService
    {
        public int CallCount { get; private set; }

        public PatientReminderDto? Result { get; init; }

        public Task<PatientReminderDto?> CreatePatientReminderAsync(
            Guid patientId,
            CreatePatientReminderRequest request,
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

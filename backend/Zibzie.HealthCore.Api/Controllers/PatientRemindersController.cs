using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Reminders;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class PatientRemindersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPatientReminderService _patientReminderService;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public PatientRemindersController(
        AppDbContext dbContext,
        IPatientReminderService patientReminderService,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _patientReminderService = patientReminderService;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/reminders")]
    public async Task<ActionResult<List<PatientReminderDto>>> GetPatientReminders(
        Guid patientId,
        [FromQuery] string? reminderType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? audience = null,
        [FromQuery] DateTimeOffset? dueBefore = null,
        [FromQuery] DateTimeOffset? dueAfter = null,
        [FromQuery] bool includeDone = true)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewReminders);

        if (!accessDecision.IsAllowed)
        {
            await LogReminderAuditAsync(
                requestContext,
                patientId,
                null,
                HealthPermissions.ViewReminders,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive);

        if (!patientExists)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var query = _dbContext.PatientReminders
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(reminderType))
        {
            var normalizedReminderType = reminderType.Trim();
            query = query.Where(x => x.ReminderType == normalizedReminderType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            var normalizedPriority = priority.Trim();
            query = query.Where(x => x.Priority == normalizedPriority);
        }

        if (!string.IsNullOrWhiteSpace(audience))
        {
            var normalizedAudience = audience.Trim();
            query = query.Where(x => x.Audience == normalizedAudience);
        }

        if (dueBefore.HasValue)
        {
            var normalizedDueBefore = dueBefore.Value.ToUniversalTime();
            query = query.Where(x => x.DueAt <= normalizedDueBefore);
        }

        if (dueAfter.HasValue)
        {
            var normalizedDueAfter = dueAfter.Value.ToUniversalTime();
            query = query.Where(x => x.DueAt >= normalizedDueAfter);
        }

        if (!includeDone)
        {
            query = query.Where(x => x.Status != ReminderStatuses.Done);
        }

        var reminders = await query
            .OrderBy(x => x.Status == ReminderStatuses.Done || x.Status == ReminderStatuses.Cancelled)
            .ThenBy(x => x.DueAt)
            .ThenBy(x =>
                x.Priority == CommonPriorities.Urgent ? 0 :
                x.Priority == CommonPriorities.High ? 1 :
                x.Priority == CommonPriorities.Normal ? 2 :
                x.Priority == CommonPriorities.Low ? 3 : 4)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PatientReminderDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                ReminderType = x.ReminderType,
                Title = x.Title,
                Description = x.Description,
                DueAt = x.DueAt,
                CompletedAt = x.CompletedAt,
                Status = x.Status,
                Priority = x.Priority,
                Audience = x.Audience,
                Channel = x.Channel,
                RelatedRecordType = x.RelatedRecordType,
                RelatedRecordId = x.RelatedRecordId,
                SourceType = x.SourceType,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        await LogReminderAuditAsync(
            requestContext,
            patientId,
            null,
            HealthPermissions.ViewReminders,
            AuditActionTypes.View,
            true,
            accessDecision);

        return Ok(reminders);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/reminders")]
    public async Task<ActionResult<PatientReminderDto>> CreatePatientReminder(
        Guid patientId,
        CreatePatientReminderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReminderType))
        {
            return BadRequest(new
            {
                message = "Reminder type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Reminder title is required."
            });
        }

        if (!request.DueAt.HasValue)
        {
            return BadRequest(new
            {
                message = "Reminder due date is required."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.CreateReminder,
            request.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogReminderAuditAsync(
                requestContext,
                patientId,
                null,
                HealthPermissions.CreateReminder,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var dto = await _patientReminderService.CreatePatientReminderAsync(patientId, request);

        if (dto is null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        await LogReminderAuditAsync(
            requestContext,
            patientId,
            dto.Id,
            HealthPermissions.CreateReminder,
            AuditActionTypes.Create,
            true,
            accessDecision);

        return CreatedAtAction(
            nameof(GetPatientReminder),
            new { reminderId = dto.Id },
            dto);
    }

    [HttpGet("api/health-core/reminders/{reminderId:guid}")]
    public async Task<ActionResult<PatientReminderDto>> GetPatientReminder(Guid reminderId)
    {
        var reminder = await _dbContext.PatientReminders
            .FirstOrDefaultAsync(x => x.Id == reminderId && !x.IsDeleted);

        if (reminder is null)
        {
            return NotFound(new
            {
                message = "Reminder not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(reminder.PatientProfileId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewReminders,
            reminder.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogReminderAuditAsync(
                requestContext,
                reminder.PatientProfileId,
                reminder.Id,
                HealthPermissions.ViewReminders,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        await LogReminderAuditAsync(
            requestContext,
            reminder.PatientProfileId,
            reminder.Id,
            HealthPermissions.ViewReminders,
            AuditActionTypes.View,
            true,
            accessDecision);

        return Ok(ToDto(reminder));
    }

    [HttpPut("api/health-core/reminders/{reminderId:guid}")]
    public async Task<ActionResult<PatientReminderDto>> UpdatePatientReminder(
        Guid reminderId,
        UpdatePatientReminderRequest request)
    {
        var reminder = await _dbContext.PatientReminders
            .FirstOrDefaultAsync(x => x.Id == reminderId && !x.IsDeleted);

        if (reminder is null)
        {
            return NotFound(new
            {
                message = "Reminder not found."
            });
        }

        var permission = GetReminderUpdatePermission(request.Status);
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(reminder.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            permission,
            request.SensitivityLevel ?? reminder.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogReminderAuditAsync(
                requestContext,
                reminder.PatientProfileId,
                reminder.Id,
                permission,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var statusChangedToDone = false;

        if (request.ReminderType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ReminderType))
            {
                return BadRequest(new
                {
                    message = "Reminder type is required."
                });
            }

            reminder.ReminderType = request.ReminderType.Trim();
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Reminder title is required."
                });
            }

            reminder.Title = request.Title.Trim();
        }

        if (request.Status is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new
                {
                    message = "Reminder status is required."
                });
            }

            var normalizedStatus = request.Status.Trim();
            statusChangedToDone =
                !string.Equals(reminder.Status, ReminderStatuses.Done, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(normalizedStatus, ReminderStatuses.Done, StringComparison.OrdinalIgnoreCase);
            reminder.Status = normalizedStatus;
        }

        if (request.Priority is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Priority))
            {
                return BadRequest(new
                {
                    message = "Reminder priority is required."
                });
            }

            reminder.Priority = request.Priority.Trim();
        }

        if (request.Audience is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Audience))
            {
                return BadRequest(new
                {
                    message = "Reminder audience is required."
                });
            }

            reminder.Audience = request.Audience.Trim();
        }

        if (request.SourceType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SourceType))
            {
                return BadRequest(new
                {
                    message = "Source type is required."
                });
            }

            reminder.SourceType = request.SourceType.Trim();
        }

        if (request.SensitivityLevel is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SensitivityLevel))
            {
                return BadRequest(new
                {
                    message = "Sensitivity level is required."
                });
            }

            reminder.SensitivityLevel = request.SensitivityLevel.Trim();
        }

        if (request.DueAt.HasValue)
        {
            reminder.DueAt = request.DueAt.Value.ToUniversalTime();
        }

        if (request.CompletedAt.HasValue)
        {
            reminder.CompletedAt = request.CompletedAt.Value.ToUniversalTime();
        }

        if (request.Description is not null)
        {
            reminder.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Channel is not null)
        {
            reminder.Channel = string.IsNullOrWhiteSpace(request.Channel) ? null : request.Channel.Trim();
        }

        if (request.RelatedRecordType is not null)
        {
            reminder.RelatedRecordType = string.IsNullOrWhiteSpace(request.RelatedRecordType) ? null : request.RelatedRecordType.Trim();
        }

        if (request.RelatedRecordId.HasValue)
        {
            reminder.RelatedRecordId = request.RelatedRecordId;
        }

        var now = DateTimeOffset.UtcNow;

        if (statusChangedToDone && !reminder.CompletedAt.HasValue)
        {
            reminder.CompletedAt = now;
        }

        reminder.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        await LogReminderAuditAsync(
            requestContext,
            reminder.PatientProfileId,
            reminder.Id,
            permission,
            AuditActionTypes.Update,
            true,
            accessDecision);

        return Ok(ToDto(reminder));
    }

    [HttpDelete("api/health-core/reminders/{reminderId:guid}")]
    public async Task<IActionResult> DeletePatientReminder(Guid reminderId)
    {
        var reminder = await _dbContext.PatientReminders
            .FirstOrDefaultAsync(x => x.Id == reminderId && !x.IsDeleted);

        if (reminder is null)
        {
            return NotFound(new
            {
                message = "Reminder not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(reminder.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditReminder,
            reminder.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogReminderAuditAsync(
                requestContext,
                reminder.PatientProfileId,
                reminder.Id,
                HealthPermissions.EditReminder,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var now = DateTimeOffset.UtcNow;

        reminder.IsDeleted = true;
        reminder.DeletedAt = now;
        reminder.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        await LogReminderAuditAsync(
            requestContext,
            reminder.PatientProfileId,
            reminder.Id,
            HealthPermissions.EditReminder,
            AuditActionTypes.Delete,
            true,
            accessDecision);

        return NoContent();
    }

    private async Task LogReminderAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid patientId,
        Guid? reminderId,
        string permission,
        string actionType,
        bool succeeded,
        AccessDecision? accessDecision = null)
    {
        await _auditLogService.LogAsync(new AuditLogRequest
        {
            UserId = requestContext.UserId,
            ServiceAccountId = requestContext.ServiceAccountId,
            PatientId = patientId,
            ProductCode = requestContext.ProductCode,
            ProductRole = requestContext.ProductRole,
            ActionType = actionType,
            ResourceType = AuditResourceTypes.Reminder,
            ResourceId = reminderId,
            Permission = permission,
            AccessScope = accessDecision?.MatchedScope,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : accessDecision?.DenialReason,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod
        });
    }

    private static string GetReminderUpdatePermission(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return HealthPermissions.EditReminder;
        }

        var normalizedStatus = status.Trim();

        if (string.Equals(normalizedStatus, ReminderStatuses.Done, StringComparison.OrdinalIgnoreCase))
        {
            return HealthPermissions.CompleteReminder;
        }

        if (string.Equals(normalizedStatus, ReminderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return HealthPermissions.CancelReminder;
        }

        return HealthPermissions.EditReminder;
    }

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
    }

    private static PatientReminderDto ToDto(PatientReminder reminder)
    {
        return new PatientReminderDto
        {
            Id = reminder.Id,
            PatientProfileId = reminder.PatientProfileId,
            ReminderType = reminder.ReminderType,
            Title = reminder.Title,
            Description = reminder.Description,
            DueAt = reminder.DueAt,
            CompletedAt = reminder.CompletedAt,
            Status = reminder.Status,
            Priority = reminder.Priority,
            Audience = reminder.Audience,
            Channel = reminder.Channel,
            RelatedRecordType = reminder.RelatedRecordType,
            RelatedRecordId = reminder.RelatedRecordId,
            SourceType = reminder.SourceType,
            SensitivityLevel = reminder.SensitivityLevel,
            CreatedAt = reminder.CreatedAt,
            UpdatedAt = reminder.UpdatedAt
        };
    }
}

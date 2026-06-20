using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Application.Timeline;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class TimelineEventsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public TimelineEventsController(
        AppDbContext dbContext,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/timeline")]
    public async Task<ActionResult<List<TimelineEventDto>>> GetPatientTimeline(
        Guid patientId,
        [FromQuery] string? eventType = null,
        [FromQuery] bool includeInternal = true)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewTimeline);

        if (!accessDecision.IsAllowed)
        {
            await LogTimelineAuditAsync(
                requestContext,
                patientId,
                null,
                HealthPermissions.ViewTimeline,
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

        var query = _dbContext.PatientTimelineEvents
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var normalizedEventType = eventType.Trim();
            query = query.Where(x => x.EventType == normalizedEventType);
        }

        if (!includeInternal)
        {
            query = query.Where(x => x.Visibility == VisibilityValues.PatientVisible);
        }

        var events = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new TimelineEventDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                EventType = x.EventType,
                Title = x.Title,
                Description = x.Description,
                OccurredAt = x.OccurredAt,
                SourceType = x.SourceType,
                RelatedRecordType = x.RelatedRecordType,
                RelatedRecordId = x.RelatedRecordId,
                Visibility = x.Visibility,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        await LogTimelineAuditAsync(
            requestContext,
            patientId,
            null,
            HealthPermissions.ViewTimeline,
            AuditActionTypes.View,
            true,
            accessDecision);

        return Ok(events);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/timeline")]
    public async Task<ActionResult<TimelineEventDto>> CreateTimelineEvent(
        Guid patientId,
        CreateTimelineEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return BadRequest(new
            {
                message = "Event type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Timeline event title is required."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.CreateTimelineEvent,
            request.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogTimelineAuditAsync(
                requestContext,
                patientId,
                null,
                HealthPermissions.CreateTimelineEvent,
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

        var now = DateTimeOffset.UtcNow;

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = request.EventType.Trim(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            OccurredAt = request.OccurredAt?.ToUniversalTime() ?? now,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.Manual : request.SourceType.Trim(),
            RelatedRecordType = string.IsNullOrWhiteSpace(request.RelatedRecordType) ? null : request.RelatedRecordType.Trim(),
            RelatedRecordId = request.RelatedRecordId,
            Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? VisibilityValues.Internal : request.Visibility.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync();

        var dto = ToDto(timelineEvent);

        await LogTimelineAuditAsync(
            requestContext,
            patientId,
            dto.Id,
            HealthPermissions.CreateTimelineEvent,
            AuditActionTypes.Create,
            true,
            accessDecision);

        return CreatedAtAction(
            nameof(GetPatientTimeline),
            new { patientId },
            dto);
    }

    [HttpPut("api/health-core/timeline-events/{eventId:guid}")]
    public async Task<ActionResult<TimelineEventDto>> UpdateTimelineEvent(
        Guid eventId,
        UpdateTimelineEventRequest request)
    {
        var timelineEvent = await _dbContext.PatientTimelineEvents
            .FirstOrDefaultAsync(x => x.Id == eventId && !x.IsDeleted);

        if (timelineEvent is null)
        {
            return NotFound(new
            {
                message = "Timeline event not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(timelineEvent.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditTimelineEvent,
            request.SensitivityLevel ?? timelineEvent.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogTimelineAuditAsync(
                requestContext,
                timelineEvent.PatientProfileId,
                timelineEvent.Id,
                HealthPermissions.EditTimelineEvent,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        if (request.EventType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest(new
                {
                    message = "Event type is required."
                });
            }

            timelineEvent.EventType = request.EventType.Trim();
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Timeline event title is required."
                });
            }

            timelineEvent.Title = request.Title.Trim();
        }

        if (request.OccurredAt.HasValue)
        {
            timelineEvent.OccurredAt = request.OccurredAt.Value.ToUniversalTime();
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

            timelineEvent.SourceType = request.SourceType.Trim();
        }

        if (request.Visibility is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Visibility))
            {
                return BadRequest(new
                {
                    message = "Visibility is required."
                });
            }

            timelineEvent.Visibility = request.Visibility.Trim();
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

            timelineEvent.SensitivityLevel = request.SensitivityLevel.Trim();
        }

        if (request.Description is not null)
        {
            timelineEvent.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.RelatedRecordType is not null)
        {
            timelineEvent.RelatedRecordType = string.IsNullOrWhiteSpace(request.RelatedRecordType) ? null : request.RelatedRecordType.Trim();
        }

        if (request.RelatedRecordId.HasValue)
        {
            timelineEvent.RelatedRecordId = request.RelatedRecordId;
        }

        timelineEvent.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        await LogTimelineAuditAsync(
            requestContext,
            timelineEvent.PatientProfileId,
            timelineEvent.Id,
            HealthPermissions.EditTimelineEvent,
            AuditActionTypes.Update,
            true,
            accessDecision);

        return Ok(ToDto(timelineEvent));
    }

    [HttpDelete("api/health-core/timeline-events/{eventId:guid}")]
    public async Task<IActionResult> DeleteTimelineEvent(Guid eventId)
    {
        var timelineEvent = await _dbContext.PatientTimelineEvents
            .FirstOrDefaultAsync(x => x.Id == eventId && !x.IsDeleted);

        if (timelineEvent is null)
        {
            return NotFound(new
            {
                message = "Timeline event not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(timelineEvent.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.DeleteTimelineEvent,
            timelineEvent.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogTimelineAuditAsync(
                requestContext,
                timelineEvent.PatientProfileId,
                timelineEvent.Id,
                HealthPermissions.DeleteTimelineEvent,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var now = DateTimeOffset.UtcNow;

        timelineEvent.IsDeleted = true;
        timelineEvent.DeletedAt = now;
        timelineEvent.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        await LogTimelineAuditAsync(
            requestContext,
            timelineEvent.PatientProfileId,
            timelineEvent.Id,
            HealthPermissions.DeleteTimelineEvent,
            AuditActionTypes.Delete,
            true,
            accessDecision);

        return NoContent();
    }

    private async Task LogTimelineAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid patientId,
        Guid? eventId,
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
            ResourceType = AuditResourceTypes.TimelineEvent,
            ResourceId = eventId,
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

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
    }

    private static TimelineEventDto ToDto(PatientTimelineEvent timelineEvent)
    {
        return new TimelineEventDto
        {
            Id = timelineEvent.Id,
            PatientProfileId = timelineEvent.PatientProfileId,
            EventType = timelineEvent.EventType,
            Title = timelineEvent.Title,
            Description = timelineEvent.Description,
            OccurredAt = timelineEvent.OccurredAt,
            SourceType = timelineEvent.SourceType,
            RelatedRecordType = timelineEvent.RelatedRecordType,
            RelatedRecordId = timelineEvent.RelatedRecordId,
            Visibility = timelineEvent.Visibility,
            SensitivityLevel = timelineEvent.SensitivityLevel,
            CreatedAt = timelineEvent.CreatedAt,
            UpdatedAt = timelineEvent.UpdatedAt
        };
    }
}

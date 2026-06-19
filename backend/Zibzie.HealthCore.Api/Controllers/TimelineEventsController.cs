using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Timeline;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class TimelineEventsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public TimelineEventsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/timeline")]
    public async Task<ActionResult<List<TimelineEventDto>>> GetPatientTimeline(
        Guid patientId,
        [FromQuery] string? eventType = null,
        [FromQuery] bool includeInternal = true)
    {
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
            query = query.Where(x => x.Visibility == "PatientVisible");
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
            Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? "Internal" : request.Visibility.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync();

        var dto = ToDto(timelineEvent);

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

        var now = DateTimeOffset.UtcNow;

        timelineEvent.IsDeleted = true;
        timelineEvent.DeletedAt = now;
        timelineEvent.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return NoContent();
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

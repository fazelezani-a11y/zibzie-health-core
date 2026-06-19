using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Reminders;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class PatientRemindersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PatientRemindersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
        var status = string.IsNullOrWhiteSpace(request.Status) ? ReminderStatuses.Pending : request.Status.Trim();

        var reminder = new PatientReminder
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            ReminderType = request.ReminderType.Trim(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DueAt = request.DueAt.Value.ToUniversalTime(),
            CompletedAt = string.Equals(status, ReminderStatuses.Done, StringComparison.OrdinalIgnoreCase) ? now : null,
            Status = status,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? CommonPriorities.Normal : request.Priority.Trim(),
            Audience = string.IsNullOrWhiteSpace(request.Audience) ? AudienceTypes.Internal : request.Audience.Trim(),
            Channel = string.IsNullOrWhiteSpace(request.Channel) ? null : request.Channel.Trim(),
            RelatedRecordType = string.IsNullOrWhiteSpace(request.RelatedRecordType) ? null : request.RelatedRecordType.Trim(),
            RelatedRecordId = request.RelatedRecordId,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.Manual : request.SourceType.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = TimelineEventTypes.Reminder,
            Title = "ثبت یادآور",
            Description = reminder.Title,
            OccurredAt = now,
            SourceType = SourceTypes.System,
            RelatedRecordType = RecordTypes.PatientReminder,
            RelatedRecordId = reminder.Id,
            Visibility = VisibilityValues.Internal,
            SensitivityLevel = reminder.SensitivityLevel,
            CreatedAt = now
        };

        _dbContext.PatientReminders.Add(reminder);
        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPatientReminder),
            new { reminderId = reminder.Id },
            ToDto(reminder));
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

        var now = DateTimeOffset.UtcNow;

        reminder.IsDeleted = true;
        reminder.DeletedAt = now;
        reminder.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return NoContent();
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class CarePlanItemsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CarePlanItemsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/care-plan")]
    public async Task<ActionResult<List<CarePlanItemDto>>> GetPatientCarePlan(
        Guid patientId,
        [FromQuery] string? category = null,
        [FromQuery] string? itemType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTimeOffset? dueBefore = null,
        [FromQuery] DateTimeOffset? dueAfter = null)
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

        var query = _dbContext.CarePlanItems
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim();
            query = query.Where(x => x.Category == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            var normalizedItemType = itemType.Trim();
            query = query.Where(x => x.ItemType == normalizedItemType);
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

        if (dueBefore.HasValue)
        {
            var normalizedDueBefore = dueBefore.Value.ToUniversalTime();
            query = query.Where(x => x.DueAt.HasValue && x.DueAt <= normalizedDueBefore);
        }

        if (dueAfter.HasValue)
        {
            var normalizedDueAfter = dueAfter.Value.ToUniversalTime();
            query = query.Where(x => x.DueAt.HasValue && x.DueAt >= normalizedDueAfter);
        }

        var items = await query
            .OrderBy(x => x.Status == CarePlanStatuses.Completed || x.Status == CarePlanStatuses.Cancelled)
            .ThenBy(x => x.DueAt == null)
            .ThenBy(x => x.DueAt)
            .ThenBy(x =>
                x.Priority == CommonPriorities.Urgent ? 0 :
                x.Priority == CommonPriorities.High ? 1 :
                x.Priority == CommonPriorities.Normal ? 2 :
                x.Priority == CommonPriorities.Low ? 3 : 4)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new CarePlanItemDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                Category = x.Category,
                ItemType = x.ItemType,
                Title = x.Title,
                Description = x.Description,
                Reason = x.Reason,
                RequestedBy = x.RequestedBy,
                AssignedTo = x.AssignedTo,
                PlannedAt = x.PlannedAt,
                DueAt = x.DueAt,
                CompletedAt = x.CompletedAt,
                Status = x.Status,
                Priority = x.Priority,
                ResultSummary = x.ResultSummary,
                NextAction = x.NextAction,
                RelatedRecordType = x.RelatedRecordType,
                RelatedRecordId = x.RelatedRecordId,
                SourceType = x.SourceType,
                VerificationStatus = x.VerificationStatus,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/care-plan")]
    public async Task<ActionResult<CarePlanItemDto>> CreateCarePlanItem(
        Guid patientId,
        CreateCarePlanItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new
            {
                message = "Care plan category is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ItemType))
        {
            return BadRequest(new
            {
                message = "Care plan item type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Care plan title is required."
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
        var plannedAt = request.PlannedAt?.ToUniversalTime();
        var dueAt = request.DueAt?.ToUniversalTime();
        var status = string.IsNullOrWhiteSpace(request.Status) ? CarePlanStatuses.Planned : request.Status.Trim();

        var item = new CarePlanItem
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            Category = request.Category.Trim(),
            ItemType = request.ItemType.Trim(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? null : request.RequestedBy.Trim(),
            AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? null : request.AssignedTo.Trim(),
            PlannedAt = plannedAt,
            DueAt = dueAt,
            CompletedAt = string.Equals(status, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase) ? now : null,
            Status = status,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? CommonPriorities.Normal : request.Priority.Trim(),
            ResultSummary = string.IsNullOrWhiteSpace(request.ResultSummary) ? null : request.ResultSummary.Trim(),
            NextAction = string.IsNullOrWhiteSpace(request.NextAction) ? null : request.NextAction.Trim(),
            RelatedRecordType = string.IsNullOrWhiteSpace(request.RelatedRecordType) ? null : request.RelatedRecordType.Trim(),
            RelatedRecordId = request.RelatedRecordId,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.Manual : request.SourceType.Trim(),
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.Unverified : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = TimelineEventTypes.CarePlan,
            Title = "ثبت آیتم پلن مراقبتی",
            Description = item.Title,
            OccurredAt = item.PlannedAt ?? item.DueAt ?? now,
            SourceType = SourceTypes.System,
            RelatedRecordType = RecordTypes.CarePlanItem,
            RelatedRecordId = item.Id,
            Visibility = VisibilityValues.Internal,
            SensitivityLevel = item.SensitivityLevel,
            CreatedAt = now
        };

        _dbContext.CarePlanItems.Add(item);
        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCarePlanItem),
            new { itemId = item.Id },
            ToDto(item));
    }

    [HttpGet("api/health-core/care-plan-items/{itemId:guid}")]
    public async Task<ActionResult<CarePlanItemDto>> GetCarePlanItem(Guid itemId)
    {
        var item = await _dbContext.CarePlanItems
            .FirstOrDefaultAsync(x => x.Id == itemId && !x.IsDeleted);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Care plan item not found."
            });
        }

        return Ok(ToDto(item));
    }

    [HttpPut("api/health-core/care-plan-items/{itemId:guid}")]
    public async Task<ActionResult<CarePlanItemDto>> UpdateCarePlanItem(
        Guid itemId,
        UpdateCarePlanItemRequest request)
    {
        var item = await _dbContext.CarePlanItems
            .FirstOrDefaultAsync(x => x.Id == itemId && !x.IsDeleted);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Care plan item not found."
            });
        }

        var statusChangedToCompleted = false;

        if (request.Category is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                return BadRequest(new
                {
                    message = "Care plan category is required."
                });
            }

            item.Category = request.Category.Trim();
        }

        if (request.ItemType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ItemType))
            {
                return BadRequest(new
                {
                    message = "Care plan item type is required."
                });
            }

            item.ItemType = request.ItemType.Trim();
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Care plan title is required."
                });
            }

            item.Title = request.Title.Trim();
        }

        if (request.Status is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new
                {
                    message = "Care plan status is required."
                });
            }

            var normalizedStatus = request.Status.Trim();
            statusChangedToCompleted =
                !string.Equals(item.Status, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(normalizedStatus, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase);
            item.Status = normalizedStatus;
        }

        if (request.Priority is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Priority))
            {
                return BadRequest(new
                {
                    message = "Care plan priority is required."
                });
            }

            item.Priority = request.Priority.Trim();
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

            item.SourceType = request.SourceType.Trim();
        }

        if (request.VerificationStatus is not null)
        {
            if (string.IsNullOrWhiteSpace(request.VerificationStatus))
            {
                return BadRequest(new
                {
                    message = "Verification status is required."
                });
            }

            item.VerificationStatus = request.VerificationStatus.Trim();
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

            item.SensitivityLevel = request.SensitivityLevel.Trim();
        }

        if (request.PlannedAt.HasValue)
        {
            item.PlannedAt = request.PlannedAt.Value.ToUniversalTime();
        }

        if (request.DueAt.HasValue)
        {
            item.DueAt = request.DueAt.Value.ToUniversalTime();
        }

        if (request.CompletedAt.HasValue)
        {
            item.CompletedAt = request.CompletedAt.Value.ToUniversalTime();
        }

        if (request.Description is not null)
        {
            item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Reason is not null)
        {
            item.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        }

        if (request.RequestedBy is not null)
        {
            item.RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? null : request.RequestedBy.Trim();
        }

        if (request.AssignedTo is not null)
        {
            item.AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? null : request.AssignedTo.Trim();
        }

        if (request.ResultSummary is not null)
        {
            item.ResultSummary = string.IsNullOrWhiteSpace(request.ResultSummary) ? null : request.ResultSummary.Trim();
        }

        if (request.NextAction is not null)
        {
            item.NextAction = string.IsNullOrWhiteSpace(request.NextAction) ? null : request.NextAction.Trim();
        }

        if (request.RelatedRecordType is not null)
        {
            item.RelatedRecordType = string.IsNullOrWhiteSpace(request.RelatedRecordType) ? null : request.RelatedRecordType.Trim();
        }

        if (request.RelatedRecordId.HasValue)
        {
            item.RelatedRecordId = request.RelatedRecordId;
        }

        var now = DateTimeOffset.UtcNow;

        if (statusChangedToCompleted && !item.CompletedAt.HasValue)
        {
            item.CompletedAt = now;
        }

        item.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(item));
    }

    [HttpDelete("api/health-core/care-plan-items/{itemId:guid}")]
    public async Task<IActionResult> DeleteCarePlanItem(Guid itemId)
    {
        var item = await _dbContext.CarePlanItems
            .FirstOrDefaultAsync(x => x.Id == itemId && !x.IsDeleted);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Care plan item not found."
            });
        }

        var now = DateTimeOffset.UtcNow;

        item.IsDeleted = true;
        item.DeletedAt = now;
        item.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static CarePlanItemDto ToDto(CarePlanItem item)
    {
        return new CarePlanItemDto
        {
            Id = item.Id,
            PatientProfileId = item.PatientProfileId,
            Category = item.Category,
            ItemType = item.ItemType,
            Title = item.Title,
            Description = item.Description,
            Reason = item.Reason,
            RequestedBy = item.RequestedBy,
            AssignedTo = item.AssignedTo,
            PlannedAt = item.PlannedAt,
            DueAt = item.DueAt,
            CompletedAt = item.CompletedAt,
            Status = item.Status,
            Priority = item.Priority,
            ResultSummary = item.ResultSummary,
            NextAction = item.NextAction,
            RelatedRecordType = item.RelatedRecordType,
            RelatedRecordId = item.RelatedRecordId,
            SourceType = item.SourceType,
            VerificationStatus = item.VerificationStatus,
            SensitivityLevel = item.SensitivityLevel,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}

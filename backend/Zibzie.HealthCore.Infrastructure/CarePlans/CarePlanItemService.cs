using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.CarePlans;

public class CarePlanItemService : ICarePlanItemService
{
    private readonly AppDbContext _dbContext;

    public CarePlanItemService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CarePlanItemDto?> CreateCarePlanItemAsync(
        Guid patientId,
        CreateCarePlanItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (!patientExists)
        {
            return null;
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
            Description = NormalizeOptional(request.Description),
            Reason = NormalizeOptional(request.Reason),
            RequestedBy = NormalizeOptional(request.RequestedBy),
            AssignedTo = NormalizeOptional(request.AssignedTo),
            PlannedAt = plannedAt,
            DueAt = dueAt,
            CompletedAt = string.Equals(status, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase) ? now : null,
            Status = status,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? CommonPriorities.Normal : request.Priority.Trim(),
            ResultSummary = NormalizeOptional(request.ResultSummary),
            NextAction = NormalizeOptional(request.NextAction),
            RelatedRecordType = NormalizeOptional(request.RelatedRecordType),
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(item);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

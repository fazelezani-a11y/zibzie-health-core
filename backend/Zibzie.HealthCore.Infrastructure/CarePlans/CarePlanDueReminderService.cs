using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.CarePlans;

public class CarePlanDueReminderService : ICarePlanDueReminderService
{
    private readonly AppDbContext _dbContext;

    public CarePlanDueReminderService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task TryAddDueReminderForCarePlanItemAsync(
        CarePlanItem item,
        CancellationToken cancellationToken = default)
    {
        if (!item.DueAt.HasValue ||
            IsTerminalCarePlanStatus(item.Status))
        {
            return;
        }

        if (_dbContext.PatientReminders.Local.Any(reminder => IsLinkedCarePlanReminder(reminder, item)))
        {
            return;
        }

        var reminderExists = await _dbContext.PatientReminders
            .AnyAsync(reminder =>
                reminder.PatientProfileId == item.PatientProfileId &&
                reminder.RelatedRecordType == RecordTypes.CarePlanItem &&
                reminder.RelatedRecordId == item.Id &&
                reminder.ReminderType == ReminderTypes.CarePlan &&
                !reminder.IsDeleted,
                cancellationToken);

        if (reminderExists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var reminder = new PatientReminder
        {
            Id = Guid.NewGuid(),
            PatientProfileId = item.PatientProfileId,
            ReminderType = ReminderTypes.CarePlan,
            Title = "یادآور برنامه مراقبتی",
            Description = BuildDescription(item),
            DueAt = item.DueAt.Value,
            Status = ReminderStatuses.Pending,
            Priority = item.Priority,
            Audience = AudienceTypes.Internal,
            Channel = null,
            RelatedRecordType = RecordTypes.CarePlanItem,
            RelatedRecordId = item.Id,
            SourceType = SourceTypes.System,
            SensitivityLevel = item.SensitivityLevel,
            CreatedAt = now
        };

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = item.PatientProfileId,
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
    }

    private static bool IsTerminalCarePlanStatus(string status)
    {
        return string.Equals(status, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, CarePlanStatuses.Cancelled, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLinkedCarePlanReminder(PatientReminder reminder, CarePlanItem item)
    {
        return reminder.PatientProfileId == item.PatientProfileId &&
            reminder.RelatedRecordType == RecordTypes.CarePlanItem &&
            reminder.RelatedRecordId == item.Id &&
            reminder.ReminderType == ReminderTypes.CarePlan &&
            !reminder.IsDeleted;
    }

    private static string BuildDescription(CarePlanItem item)
    {
        return string.IsNullOrWhiteSpace(item.Reason)
            ? item.Title
            : $"{item.Title} - {item.Reason}";
    }
}

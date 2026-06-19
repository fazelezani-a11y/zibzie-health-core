using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Reminders;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.Reminders;

public class PatientReminderService : IPatientReminderService
{
    private readonly AppDbContext _dbContext;

    public PatientReminderService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientReminderDto?> CreatePatientReminderAsync(
        Guid patientId,
        CreatePatientReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (!patientExists)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var dueAt = request.DueAt ?? throw new InvalidOperationException("Reminder due date was not validated.");
        var status = string.IsNullOrWhiteSpace(request.Status) ? ReminderStatuses.Pending : request.Status.Trim();

        var reminder = new PatientReminder
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            ReminderType = request.ReminderType.Trim(),
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            DueAt = dueAt.ToUniversalTime(),
            CompletedAt = string.Equals(status, ReminderStatuses.Done, StringComparison.OrdinalIgnoreCase) ? now : null,
            Status = status,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? CommonPriorities.Normal : request.Priority.Trim(),
            Audience = string.IsNullOrWhiteSpace(request.Audience) ? AudienceTypes.Internal : request.Audience.Trim(),
            Channel = NormalizeOptional(request.Channel),
            RelatedRecordType = NormalizeOptional(request.RelatedRecordType),
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(reminder);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

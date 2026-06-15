namespace Zibzie.HealthCore.Application.Reminders;

public class PatientReminderDto
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string ReminderType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset DueAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string? Channel { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string SensitivityLevel { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

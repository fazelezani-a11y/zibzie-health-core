namespace Zibzie.HealthCore.Application.Reminders;

public class UpdatePatientReminderRequest
{
    public string? ReminderType { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public string? Audience { get; set; }

    public string? Channel { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string? SourceType { get; set; }

    public string? SensitivityLevel { get; set; }
}

namespace Zibzie.HealthCore.Application.Reminders;

public class CreatePatientReminderRequest
{
    public string ReminderType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public string Status { get; set; } = "Pending";

    public string Priority { get; set; } = "Normal";

    public string Audience { get; set; } = "Internal";

    public string? Channel { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string SourceType { get; set; } = "Manual";

    public string SensitivityLevel { get; set; } = "Normal";
}

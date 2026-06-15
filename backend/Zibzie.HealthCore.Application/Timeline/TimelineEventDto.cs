namespace Zibzie.HealthCore.Application.Timeline;

public class TimelineEventDto
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string Visibility { get; set; } = string.Empty;

    public string SensitivityLevel { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

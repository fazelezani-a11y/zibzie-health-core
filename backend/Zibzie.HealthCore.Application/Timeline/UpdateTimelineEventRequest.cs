namespace Zibzie.HealthCore.Application.Timeline;

public class UpdateTimelineEventRequest
{
    public string? EventType { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }

    public string? SourceType { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string? Visibility { get; set; }

    public string? SensitivityLevel { get; set; }
}

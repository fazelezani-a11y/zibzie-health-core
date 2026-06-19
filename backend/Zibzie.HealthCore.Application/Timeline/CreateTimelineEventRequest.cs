using Zibzie.HealthCore.Domain.Common;

namespace Zibzie.HealthCore.Application.Timeline;

public class CreateTimelineEventRequest
{
    public string EventType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }

    public string SourceType { get; set; } = SourceTypes.Manual;

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string Visibility { get; set; } = VisibilityValues.Internal;

    public string SensitivityLevel { get; set; } = SensitivityLevels.Normal;
}

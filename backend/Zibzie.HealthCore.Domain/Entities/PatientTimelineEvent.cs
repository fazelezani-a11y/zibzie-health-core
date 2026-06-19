using Zibzie.HealthCore.Domain.Common;

namespace Zibzie.HealthCore.Domain.Entities;

public class PatientTimelineEvent
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public string SourceType { get; set; } = SourceTypes.Manual;

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string Visibility { get; set; } = "Internal";

    public string SensitivityLevel { get; set; } = SensitivityLevels.Normal;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTimeOffset? DeletedAt { get; set; }

    public PatientProfile PatientProfile { get; set; } = null!;
}

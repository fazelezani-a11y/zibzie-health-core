using Zibzie.HealthCore.Domain.Common;

namespace Zibzie.HealthCore.Domain.Entities;

public class PatientMeasurement
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string MeasurementType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTimeOffset MeasuredAt { get; set; } = DateTimeOffset.UtcNow;

    public string? Method { get; set; }

    public string? BodySite { get; set; }

    public string? Context { get; set; }

    public string? ReferenceRange { get; set; }

    public bool? IsAbnormal { get; set; }

    public decimal? TargetMin { get; set; }

    public decimal? TargetMax { get; set; }

    public string SourceType { get; set; } = SourceTypes.Manual;

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string VerificationStatus { get; set; } = VerificationStatuses.Unverified;

    public string SensitivityLevel { get; set; } = SensitivityLevels.Normal;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTimeOffset? DeletedAt { get; set; }

    public PatientProfile PatientProfile { get; set; } = null!;
}

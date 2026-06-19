namespace Zibzie.HealthCore.Application.Measurements;

public class CreatePatientMeasurementRequest
{
    public string MeasurementType { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public decimal? Value { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTimeOffset? MeasuredAt { get; set; }

    public string? Method { get; set; }

    public string? BodySite { get; set; }

    public string? Context { get; set; }

    public string? ReferenceRange { get; set; }

    public bool? IsAbnormal { get; set; }

    public decimal? TargetMin { get; set; }

    public decimal? TargetMax { get; set; }

    public string SourceType { get; set; } = "Manual";

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string VerificationStatus { get; set; } = "Unverified";

    public string SensitivityLevel { get; set; } = "Normal";
}

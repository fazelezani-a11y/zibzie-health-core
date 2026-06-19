namespace Zibzie.HealthCore.Application.Measurements;

public class UpdatePatientMeasurementRequest
{
    public string? MeasurementType { get; set; }

    public string? DisplayName { get; set; }

    public decimal? Value { get; set; }

    public string? Unit { get; set; }

    public DateTimeOffset? MeasuredAt { get; set; }

    public string? Method { get; set; }

    public string? BodySite { get; set; }

    public string? Context { get; set; }

    public string? ReferenceRange { get; set; }

    public bool? IsAbnormal { get; set; }

    public decimal? TargetMin { get; set; }

    public decimal? TargetMax { get; set; }

    public string? SourceType { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string? VerificationStatus { get; set; }

    public string? SensitivityLevel { get; set; }
}

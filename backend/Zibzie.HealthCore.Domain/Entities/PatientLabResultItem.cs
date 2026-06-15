namespace Zibzie.HealthCore.Domain.Entities;

public class PatientLabResultItem
{
    public Guid Id { get; set; }

    public Guid PatientParaclinicalResultId { get; set; }

    public string TestName { get; set; } = string.Empty;

    public string? Value { get; set; }

    public decimal? NumericValue { get; set; }

    public string? Unit { get; set; }

    public string? ReferenceRange { get; set; }

    public bool? IsAbnormal { get; set; }

    public string? Interpretation { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public PatientParaclinicalResult PatientParaclinicalResult { get; set; } = null!;
}

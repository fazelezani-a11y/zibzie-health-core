namespace Zibzie.HealthCore.Application.MedicalHistory;

public class ConditionDto
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Status { get; set; }

    public int? StartedYear { get; set; }

    public string? TreatmentSummary { get; set; }

    public string? ClinicianNote { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = string.Empty;

    public string SensitivityLevel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
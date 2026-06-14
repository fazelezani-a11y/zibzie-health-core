namespace Zibzie.HealthCore.Application.MedicalHistory;

public class UpdateConditionRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Status { get; set; }

    public int? StartedYear { get; set; }

    public string? TreatmentSummary { get; set; }

    public string? ClinicianNote { get; set; }

    public string SourceType { get; set; } = "PatientSelfReport";

    public string VerificationStatus { get; set; } = "SelfReported";

    public string SensitivityLevel { get; set; } = "Normal";
}
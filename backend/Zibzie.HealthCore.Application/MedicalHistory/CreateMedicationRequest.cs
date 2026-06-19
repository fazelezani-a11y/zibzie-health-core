using Zibzie.HealthCore.Domain.Common;

namespace Zibzie.HealthCore.Application.MedicalHistory;

public class CreateMedicationRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Dose { get; set; }

    public string? Frequency { get; set; }

    public string? Route { get; set; }

    public string? Reason { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; } = true;

    public string? ClinicianNote { get; set; }

    public string SourceType { get; set; } = SourceTypes.PatientSelfReport;

    public string VerificationStatus { get; set; } = VerificationStatuses.SelfReported;

    public string SensitivityLevel { get; set; } = SensitivityLevels.Normal;
}

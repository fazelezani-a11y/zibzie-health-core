using Zibzie.HealthCore.Domain.Common;

namespace Zibzie.HealthCore.Application.MedicalHistory;

public class CreateAllergyRequest
{
    public string Allergen { get; set; } = string.Empty;

    public string? AllergyType { get; set; }

    public string? Severity { get; set; }

    public string? ReactionDescription { get; set; }

    public string? ClinicianNote { get; set; }

    public string SourceType { get; set; } = SourceTypes.PatientSelfReport;

    public string VerificationStatus { get; set; } = VerificationStatuses.SelfReported;

    public string SensitivityLevel { get; set; } = SensitivityLevels.Normal;
}

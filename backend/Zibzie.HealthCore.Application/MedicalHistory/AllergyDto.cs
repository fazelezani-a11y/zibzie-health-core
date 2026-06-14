namespace Zibzie.HealthCore.Application.MedicalHistory;

public class AllergyDto
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string Allergen { get; set; } = string.Empty;

    public string? AllergyType { get; set; }

    public string? Severity { get; set; }

    public string? ReactionDescription { get; set; }

    public string? ClinicianNote { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = string.Empty;

    public string SensitivityLevel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
namespace Zibzie.HealthCore.Domain.Entities;

public class Allergy
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string Allergen { get; set; } = string.Empty;

    public string? AllergyType { get; set; }

    public string? Severity { get; set; }

    public string? ReactionDescription { get; set; }

    public string? ClinicianNote { get; set; }

    public string SourceType { get; set; } = "PatientSelfReport";

    public string VerificationStatus { get; set; } = "SelfReported";

    public string SensitivityLevel { get; set; } = "Normal";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public PatientProfile PatientProfile { get; set; } = null!;
}
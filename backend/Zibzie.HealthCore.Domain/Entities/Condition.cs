namespace Zibzie.HealthCore.Domain.Entities;

public class Condition
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Status { get; set; }

    public int? StartedYear { get; set; }

    public string? TreatmentSummary { get; set; }

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
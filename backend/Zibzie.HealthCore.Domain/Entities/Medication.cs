namespace Zibzie.HealthCore.Domain.Entities;

public class Medication
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Dose { get; set; }

    public string? Frequency { get; set; }

    public string? Route { get; set; }

    public string? Reason { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; } = true;

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
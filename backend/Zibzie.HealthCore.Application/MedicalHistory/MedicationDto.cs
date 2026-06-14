namespace Zibzie.HealthCore.Application.MedicalHistory;

public class MedicationDto
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

    public bool IsCurrent { get; set; }

    public string? ClinicianNote { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = string.Empty;

    public string SensitivityLevel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

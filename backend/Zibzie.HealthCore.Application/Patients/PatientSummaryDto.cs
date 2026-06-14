using Zibzie.HealthCore.Application.MedicalHistory;

namespace Zibzie.HealthCore.Application.Patients;

public class PatientSummaryDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public DateOnly? BirthDate { get; set; }

    public string? NationalCode { get; set; }

    public string? Gender { get; set; }

    public string? BloodType { get; set; }

    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? HomeAddress { get; set; }

    public string? WorkAddress { get; set; }

    public List<ConditionDto> Conditions { get; set; } = new();

    public List<AllergyDto> Allergies { get; set; } = new();

    public List<MedicationDto> CurrentMedications { get; set; } = new();
}

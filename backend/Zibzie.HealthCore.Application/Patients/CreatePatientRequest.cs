namespace Zibzie.HealthCore.Application.Patients;

public class CreatePatientRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string? NationalCode { get; set; }

    public string? Gender { get; set; }

    public string? BloodType { get; set; }

    public string? MaritalStatus { get; set; }

    public string? EducationLevel { get; set; }

    public string? Occupation { get; set; }

    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? HomeAddress { get; set; }

    public string? WorkAddress { get; set; }
}
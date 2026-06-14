namespace Zibzie.HealthCore.Application.Patients;

public class PatientListItemDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string? NationalCode { get; set; }

    public string MobileNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
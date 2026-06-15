namespace Zibzie.HealthCore.Domain.Entities;

public class PatientProfile
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string? NationalCode { get; set; }

    public string? Gender { get; set; }

    public string? BloodType { get; set; }

    public string? MaritalStatus { get; set; }

    public string? EducationLevel { get; set; }

    public string? Occupation { get; set; }

    public string? ProfileImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ContactInfo? ContactInfo { get; set; }

    public List<Condition> Conditions { get; set; } = new();

    public List<Allergy> Allergies { get; set; } = new();

    public List<Medication> Medications { get; set; } = new();

    public List<PatientTimelineEvent> TimelineEvents { get; set; } = new();

    public List<PatientDocument> Documents { get; set; } = new();

    public List<PatientParaclinicalResult> ParaclinicalResults { get; set; } = new();
}

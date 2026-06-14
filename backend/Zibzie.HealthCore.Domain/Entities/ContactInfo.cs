namespace Zibzie.HealthCore.Domain.Entities;

public class ContactInfo
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? HomeAddress { get; set; }

    public string? WorkAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public PatientProfile PatientProfile { get; set; } = null!;
}
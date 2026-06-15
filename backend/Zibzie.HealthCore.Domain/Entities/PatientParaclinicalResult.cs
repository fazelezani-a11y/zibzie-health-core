namespace Zibzie.HealthCore.Domain.Entities;

public class PatientParaclinicalResult
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string ResultType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset? PerformedAt { get; set; }

    public DateTimeOffset? ResultDate { get; set; }

    public string? ProviderName { get; set; }

    public Guid? LinkedDocumentId { get; set; }

    public string? Summary { get; set; }

    public string? Interpretation { get; set; }

    public bool? IsAbnormal { get; set; }

    public bool RequiresFollowUp { get; set; } = false;

    public string? FollowUpNote { get; set; }

    public string SourceType { get; set; } = "Manual";

    public string VerificationStatus { get; set; } = "Unverified";

    public string SensitivityLevel { get; set; } = "Normal";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTimeOffset? DeletedAt { get; set; }

    public PatientProfile PatientProfile { get; set; } = null!;

    public PatientDocument? LinkedDocument { get; set; }

    public List<PatientLabResultItem> LabItems { get; set; } = new();
}

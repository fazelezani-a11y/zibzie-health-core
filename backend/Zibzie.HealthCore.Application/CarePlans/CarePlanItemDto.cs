namespace Zibzie.HealthCore.Application.CarePlans;

public class CarePlanItemDto
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string ItemType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Reason { get; set; }

    public string? RequestedBy { get; set; }

    public string? AssignedTo { get; set; }

    public DateTimeOffset? PlannedAt { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string? ResultSummary { get; set; }

    public string? NextAction { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = string.Empty;

    public string SensitivityLevel { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

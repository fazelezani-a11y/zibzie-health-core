namespace Zibzie.HealthCore.Application.CarePlans;

public class CreateCarePlanItemRequest
{
    public string Category { get; set; } = string.Empty;

    public string ItemType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Reason { get; set; }

    public string? RequestedBy { get; set; }

    public string? AssignedTo { get; set; }

    public DateTimeOffset? PlannedAt { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public string Status { get; set; } = "Planned";

    public string Priority { get; set; } = "Normal";

    public string? ResultSummary { get; set; }

    public string? NextAction { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string SourceType { get; set; } = "Manual";

    public string VerificationStatus { get; set; } = "Unverified";

    public string SensitivityLevel { get; set; } = "Normal";
}

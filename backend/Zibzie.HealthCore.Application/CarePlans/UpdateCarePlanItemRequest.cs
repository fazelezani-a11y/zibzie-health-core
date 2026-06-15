namespace Zibzie.HealthCore.Application.CarePlans;

public class UpdateCarePlanItemRequest
{
    public string? Category { get; set; }

    public string? ItemType { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Reason { get; set; }

    public string? RequestedBy { get; set; }

    public string? AssignedTo { get; set; }

    public DateTimeOffset? PlannedAt { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public string? ResultSummary { get; set; }

    public string? NextAction { get; set; }

    public string? RelatedRecordType { get; set; }

    public Guid? RelatedRecordId { get; set; }

    public string? SourceType { get; set; }

    public string? VerificationStatus { get; set; }

    public string? SensitivityLevel { get; set; }
}

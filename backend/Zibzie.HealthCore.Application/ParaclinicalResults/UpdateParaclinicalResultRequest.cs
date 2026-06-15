namespace Zibzie.HealthCore.Application.ParaclinicalResults;

public class UpdateParaclinicalResultRequest
{
    public string? ResultType { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? PerformedAt { get; set; }

    public DateTimeOffset? ResultDate { get; set; }

    public string? ProviderName { get; set; }

    public Guid? LinkedDocumentId { get; set; }

    public string? Summary { get; set; }

    public string? Interpretation { get; set; }

    public bool? IsAbnormal { get; set; }

    public bool? RequiresFollowUp { get; set; }

    public string? FollowUpNote { get; set; }

    public string? SourceType { get; set; }

    public string? VerificationStatus { get; set; }

    public string? SensitivityLevel { get; set; }
}

namespace Zibzie.HealthCore.Application.ParaclinicalResults;

public class ParaclinicalResultDto
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

    public bool RequiresFollowUp { get; set; }

    public string? FollowUpNote { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = string.Empty;

    public string SensitivityLevel { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public List<LabResultItemDto> LabItems { get; set; } = new();
}

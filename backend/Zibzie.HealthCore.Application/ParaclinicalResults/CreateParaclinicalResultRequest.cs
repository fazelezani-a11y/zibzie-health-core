using Zibzie.HealthCore.Domain.Common;

namespace Zibzie.HealthCore.Application.ParaclinicalResults;

public class CreateParaclinicalResultRequest
{
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

    public string SourceType { get; set; } = SourceTypes.Manual;

    public string VerificationStatus { get; set; } = VerificationStatuses.Unverified;

    public string SensitivityLevel { get; set; } = SensitivityLevels.Normal;

    public List<CreateLabResultItemRequest> LabItems { get; set; } = new();
}

public class CreateLabResultItemRequest
{
    public string TestName { get; set; } = string.Empty;

    public string? Value { get; set; }

    public decimal? NumericValue { get; set; }

    public string? Unit { get; set; }

    public string? ReferenceRange { get; set; }

    public bool? IsAbnormal { get; set; }

    public string? Interpretation { get; set; }

    public int? DisplayOrder { get; set; }
}

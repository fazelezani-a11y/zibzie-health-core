using Zibzie.HealthCore.Domain.Common;

namespace Zibzie.HealthCore.Domain.Entities;

public class PatientDocument
{
    public Guid Id { get; set; }

    public Guid PatientProfileId { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset? DocumentDate { get; set; }

    public string? IssuerName { get; set; }

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }

    public string? FileReference { get; set; }

    public string? MimeType { get; set; }

    public long? FileSizeBytes { get; set; }

    public string SourceType { get; set; } = SourceTypes.Manual;

    public string VerificationStatus { get; set; } = VerificationStatuses.Unverified;

    public string SensitivityLevel { get; set; } = SensitivityLevels.Normal;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTimeOffset? DeletedAt { get; set; }

    public PatientProfile PatientProfile { get; set; } = null!;
}

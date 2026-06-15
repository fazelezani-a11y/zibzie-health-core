namespace Zibzie.HealthCore.Application.Documents;

public class UpdatePatientDocumentRequest
{
    public string? DocumentType { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? DocumentDate { get; set; }

    public string? IssuerName { get; set; }

    public string? FileName { get; set; }

    public string? FileUrl { get; set; }

    public string? FileReference { get; set; }

    public string? MimeType { get; set; }

    public long? FileSizeBytes { get; set; }

    public string? SourceType { get; set; }

    public string? VerificationStatus { get; set; }

    public string? SensitivityLevel { get; set; }
}

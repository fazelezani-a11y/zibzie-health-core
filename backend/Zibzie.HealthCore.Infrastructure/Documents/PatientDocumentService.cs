using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Documents;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.Documents;

public class PatientDocumentService : IPatientDocumentService
{
    private readonly AppDbContext _dbContext;

    public PatientDocumentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientDocumentDto?> CreatePatientDocumentAsync(
        Guid patientId,
        CreatePatientDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (!patientExists)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var documentDate = request.DocumentDate?.ToUniversalTime();

        var document = new PatientDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            DocumentType = request.DocumentType.Trim(),
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            DocumentDate = documentDate,
            IssuerName = NormalizeOptional(request.IssuerName),
            FileName = NormalizeOptional(request.FileName),
            FileUrl = NormalizeOptional(request.FileUrl),
            FileReference = NormalizeOptional(request.FileReference),
            MimeType = NormalizeOptional(request.MimeType),
            FileSizeBytes = request.FileSizeBytes,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.Manual : request.SourceType.Trim(),
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.Unverified : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = TimelineEventTypes.Document,
            Title = "ثبت مدرک پزشکی",
            Description = document.Title,
            OccurredAt = document.DocumentDate ?? now,
            SourceType = SourceTypes.System,
            RelatedRecordType = RecordTypes.PatientDocument,
            RelatedRecordId = document.Id,
            Visibility = VisibilityValues.Internal,
            SensitivityLevel = document.SensitivityLevel,
            CreatedAt = now
        };

        _dbContext.PatientDocuments.Add(document);
        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(document);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PatientDocumentDto ToDto(PatientDocument document)
    {
        return new PatientDocumentDto
        {
            Id = document.Id,
            PatientProfileId = document.PatientProfileId,
            DocumentType = document.DocumentType,
            Title = document.Title,
            Description = document.Description,
            DocumentDate = document.DocumentDate,
            IssuerName = document.IssuerName,
            FileName = document.FileName,
            FileUrl = document.FileUrl,
            FileReference = document.FileReference,
            MimeType = document.MimeType,
            FileSizeBytes = document.FileSizeBytes,
            SourceType = document.SourceType,
            VerificationStatus = document.VerificationStatus,
            SensitivityLevel = document.SensitivityLevel,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
}

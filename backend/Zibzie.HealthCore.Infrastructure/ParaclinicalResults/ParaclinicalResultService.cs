using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.ParaclinicalResults;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.ParaclinicalResults;

public class ParaclinicalResultService : IParaclinicalResultService
{
    private readonly AppDbContext _dbContext;

    public ParaclinicalResultService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateParaclinicalResultResult> CreateParaclinicalResultAsync(
        Guid patientId,
        CreateParaclinicalResultRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (!patientExists)
        {
            return CreateParaclinicalResultResult.PatientNotFound();
        }

        if (request.LinkedDocumentId.HasValue &&
            !await LinkedDocumentBelongsToPatientAsync(patientId, request.LinkedDocumentId.Value, cancellationToken))
        {
            return CreateParaclinicalResultResult.LinkedDocumentNotFound();
        }

        var labItems = request.LabItems ?? new List<CreateLabResultItemRequest>();

        for (var i = 0; i < labItems.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(labItems[i].TestName))
            {
                return CreateParaclinicalResultResult.LabItemTestNameRequired();
            }
        }

        var now = DateTimeOffset.UtcNow;
        var resultDate = request.ResultDate?.ToUniversalTime();
        var performedAt = request.PerformedAt?.ToUniversalTime();

        var result = new PatientParaclinicalResult
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            ResultType = request.ResultType.Trim(),
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            PerformedAt = performedAt,
            ResultDate = resultDate,
            ProviderName = NormalizeOptional(request.ProviderName),
            LinkedDocumentId = request.LinkedDocumentId,
            Summary = NormalizeOptional(request.Summary),
            Interpretation = NormalizeOptional(request.Interpretation),
            IsAbnormal = request.IsAbnormal,
            RequiresFollowUp = request.RequiresFollowUp,
            FollowUpNote = NormalizeOptional(request.FollowUpNote),
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.Manual : request.SourceType.Trim(),
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.Unverified : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        for (var i = 0; i < labItems.Count; i++)
        {
            var item = labItems[i];

            result.LabItems.Add(new PatientLabResultItem
            {
                Id = Guid.NewGuid(),
                PatientParaclinicalResultId = result.Id,
                TestName = item.TestName.Trim(),
                Value = NormalizeOptional(item.Value),
                NumericValue = item.NumericValue,
                Unit = NormalizeOptional(item.Unit),
                ReferenceRange = NormalizeOptional(item.ReferenceRange),
                IsAbnormal = item.IsAbnormal,
                Interpretation = NormalizeOptional(item.Interpretation),
                DisplayOrder = item.DisplayOrder ?? i + 1,
                CreatedAt = now
            });
        }

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = TimelineEventTypes.ParaclinicalResult,
            Title = "ثبت نتیجه پاراکلینیک",
            Description = result.Title,
            OccurredAt = result.ResultDate ?? result.PerformedAt ?? now,
            SourceType = SourceTypes.System,
            RelatedRecordType = RecordTypes.PatientParaclinicalResult,
            RelatedRecordId = result.Id,
            Visibility = VisibilityValues.Internal,
            SensitivityLevel = result.SensitivityLevel,
            CreatedAt = now
        };

        _dbContext.PatientParaclinicalResults.Add(result);
        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateParaclinicalResultResult.Created(ToDto(result));
    }

    private async Task<bool> LinkedDocumentBelongsToPatientAsync(
        Guid patientId,
        Guid linkedDocumentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PatientDocuments
            .AnyAsync(x =>
                x.Id == linkedDocumentId &&
                x.PatientProfileId == patientId &&
                !x.IsDeleted,
                cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ParaclinicalResultDto ToDto(PatientParaclinicalResult result)
    {
        return new ParaclinicalResultDto
        {
            Id = result.Id,
            PatientProfileId = result.PatientProfileId,
            ResultType = result.ResultType,
            Title = result.Title,
            Description = result.Description,
            PerformedAt = result.PerformedAt,
            ResultDate = result.ResultDate,
            ProviderName = result.ProviderName,
            LinkedDocumentId = result.LinkedDocumentId,
            Summary = result.Summary,
            Interpretation = result.Interpretation,
            IsAbnormal = result.IsAbnormal,
            RequiresFollowUp = result.RequiresFollowUp,
            FollowUpNote = result.FollowUpNote,
            SourceType = result.SourceType,
            VerificationStatus = result.VerificationStatus,
            SensitivityLevel = result.SensitivityLevel,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt,
            LabItems = result.LabItems
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedAt)
                .Select(ToDto)
                .ToList()
        };
    }

    private static LabResultItemDto ToDto(PatientLabResultItem item)
    {
        return new LabResultItemDto
        {
            Id = item.Id,
            PatientParaclinicalResultId = item.PatientParaclinicalResultId,
            TestName = item.TestName,
            Value = item.Value,
            NumericValue = item.NumericValue,
            Unit = item.Unit,
            ReferenceRange = item.ReferenceRange,
            IsAbnormal = item.IsAbnormal,
            Interpretation = item.Interpretation,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt
        };
    }
}

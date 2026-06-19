using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Measurements;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.Measurements;

public class PatientMeasurementService : IPatientMeasurementService
{
    private readonly AppDbContext _dbContext;

    public PatientMeasurementService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientMeasurementDto?> CreatePatientMeasurementAsync(
        Guid patientId,
        CreatePatientMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (!patientExists)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var measurementType = request.MeasurementType.Trim();
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? measurementType
            : request.DisplayName.Trim();
        var value = request.Value ?? throw new InvalidOperationException("Measurement value was not validated.");

        var measurement = new PatientMeasurement
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            MeasurementType = measurementType,
            DisplayName = displayName,
            Value = value,
            Unit = request.Unit.Trim(),
            MeasuredAt = request.MeasuredAt?.ToUniversalTime() ?? now,
            Method = NormalizeOptional(request.Method),
            BodySite = NormalizeOptional(request.BodySite),
            Context = NormalizeOptional(request.Context),
            ReferenceRange = NormalizeOptional(request.ReferenceRange),
            IsAbnormal = request.IsAbnormal,
            TargetMin = request.TargetMin,
            TargetMax = request.TargetMax,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.Manual : request.SourceType.Trim(),
            RelatedRecordType = NormalizeOptional(request.RelatedRecordType),
            RelatedRecordId = request.RelatedRecordId,
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.Unverified : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = TimelineEventTypes.Measurement,
            Title = "ثبت شاخص سلامت",
            Description = $"{measurement.DisplayName}: {measurement.Value.ToString(CultureInfo.InvariantCulture)} {measurement.Unit}",
            OccurredAt = measurement.MeasuredAt,
            SourceType = SourceTypes.System,
            RelatedRecordType = RecordTypes.PatientMeasurement,
            RelatedRecordId = measurement.Id,
            Visibility = VisibilityValues.Internal,
            SensitivityLevel = measurement.SensitivityLevel,
            CreatedAt = now
        };

        _dbContext.PatientMeasurements.Add(measurement);
        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(measurement);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PatientMeasurementDto ToDto(PatientMeasurement measurement)
    {
        return new PatientMeasurementDto
        {
            Id = measurement.Id,
            PatientProfileId = measurement.PatientProfileId,
            MeasurementType = measurement.MeasurementType,
            DisplayName = measurement.DisplayName,
            Value = measurement.Value,
            Unit = measurement.Unit,
            MeasuredAt = measurement.MeasuredAt,
            Method = measurement.Method,
            BodySite = measurement.BodySite,
            Context = measurement.Context,
            ReferenceRange = measurement.ReferenceRange,
            IsAbnormal = measurement.IsAbnormal,
            TargetMin = measurement.TargetMin,
            TargetMax = measurement.TargetMax,
            SourceType = measurement.SourceType,
            RelatedRecordType = measurement.RelatedRecordType,
            RelatedRecordId = measurement.RelatedRecordId,
            VerificationStatus = measurement.VerificationStatus,
            SensitivityLevel = measurement.SensitivityLevel,
            CreatedAt = measurement.CreatedAt,
            UpdatedAt = measurement.UpdatedAt
        };
    }
}

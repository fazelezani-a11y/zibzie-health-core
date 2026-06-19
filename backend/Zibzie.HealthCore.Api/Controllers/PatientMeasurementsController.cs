using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Measurements;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class PatientMeasurementsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PatientMeasurementsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/measurements")]
    public async Task<ActionResult<List<PatientMeasurementDto>>> GetPatientMeasurements(
        Guid patientId,
        [FromQuery] string? measurementType = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? verificationStatus = null,
        [FromQuery] string? sensitivityLevel = null,
        [FromQuery] DateTimeOffset? measuredFrom = null,
        [FromQuery] DateTimeOffset? measuredTo = null,
        [FromQuery] string? relatedRecordType = null,
        [FromQuery] Guid? relatedRecordId = null)
    {
        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive);

        if (!patientExists)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var query = _dbContext.PatientMeasurements
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(measurementType))
        {
            var normalizedMeasurementType = measurementType.Trim();
            query = query.Where(x => x.MeasurementType == normalizedMeasurementType);
        }

        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            var normalizedSourceType = sourceType.Trim();
            query = query.Where(x => x.SourceType == normalizedSourceType);
        }

        if (!string.IsNullOrWhiteSpace(verificationStatus))
        {
            var normalizedVerificationStatus = verificationStatus.Trim();
            query = query.Where(x => x.VerificationStatus == normalizedVerificationStatus);
        }

        if (!string.IsNullOrWhiteSpace(sensitivityLevel))
        {
            var normalizedSensitivityLevel = sensitivityLevel.Trim();
            query = query.Where(x => x.SensitivityLevel == normalizedSensitivityLevel);
        }

        if (measuredFrom.HasValue)
        {
            var normalizedMeasuredFrom = measuredFrom.Value.ToUniversalTime();
            query = query.Where(x => x.MeasuredAt >= normalizedMeasuredFrom);
        }

        if (measuredTo.HasValue)
        {
            var normalizedMeasuredTo = measuredTo.Value.ToUniversalTime();
            query = query.Where(x => x.MeasuredAt <= normalizedMeasuredTo);
        }

        if (!string.IsNullOrWhiteSpace(relatedRecordType))
        {
            var normalizedRelatedRecordType = relatedRecordType.Trim();
            query = query.Where(x => x.RelatedRecordType == normalizedRelatedRecordType);
        }

        if (relatedRecordId.HasValue)
        {
            query = query.Where(x => x.RelatedRecordId == relatedRecordId.Value);
        }

        var measurements = await query
            .OrderByDescending(x => x.MeasuredAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PatientMeasurementDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                MeasurementType = x.MeasurementType,
                DisplayName = x.DisplayName,
                Value = x.Value,
                Unit = x.Unit,
                MeasuredAt = x.MeasuredAt,
                Method = x.Method,
                BodySite = x.BodySite,
                Context = x.Context,
                ReferenceRange = x.ReferenceRange,
                IsAbnormal = x.IsAbnormal,
                TargetMin = x.TargetMin,
                TargetMax = x.TargetMax,
                SourceType = x.SourceType,
                RelatedRecordType = x.RelatedRecordType,
                RelatedRecordId = x.RelatedRecordId,
                VerificationStatus = x.VerificationStatus,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(measurements);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/measurements")]
    public async Task<ActionResult<PatientMeasurementDto>> CreatePatientMeasurement(
        Guid patientId,
        CreatePatientMeasurementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MeasurementType))
        {
            return BadRequest(new
            {
                message = "Measurement type is required."
            });
        }

        if (!request.Value.HasValue)
        {
            return BadRequest(new
            {
                message = "Measurement value is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Unit))
        {
            return BadRequest(new
            {
                message = "Measurement unit is required."
            });
        }

        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive);

        if (!patientExists)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var measurementType = request.MeasurementType.Trim();
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? measurementType
            : request.DisplayName.Trim();

        var measurement = new PatientMeasurement
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            MeasurementType = measurementType,
            DisplayName = displayName,
            Value = request.Value.Value,
            Unit = request.Unit.Trim(),
            MeasuredAt = request.MeasuredAt?.ToUniversalTime() ?? now,
            Method = NormalizeOptional(request.Method),
            BodySite = NormalizeOptional(request.BodySite),
            Context = NormalizeOptional(request.Context),
            ReferenceRange = NormalizeOptional(request.ReferenceRange),
            IsAbnormal = request.IsAbnormal,
            TargetMin = request.TargetMin,
            TargetMax = request.TargetMax,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Manual" : request.SourceType.Trim(),
            RelatedRecordType = NormalizeOptional(request.RelatedRecordType),
            RelatedRecordId = request.RelatedRecordId,
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? "Unverified" : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? "Normal" : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        var timelineEvent = new PatientTimelineEvent
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            EventType = "Measurement",
            Title = "ثبت شاخص سلامت",
            Description = $"{measurement.DisplayName}: {measurement.Value.ToString(CultureInfo.InvariantCulture)} {measurement.Unit}",
            OccurredAt = measurement.MeasuredAt,
            SourceType = "System",
            RelatedRecordType = "PatientMeasurement",
            RelatedRecordId = measurement.Id,
            Visibility = "Internal",
            SensitivityLevel = measurement.SensitivityLevel,
            CreatedAt = now
        };

        _dbContext.PatientMeasurements.Add(measurement);
        _dbContext.PatientTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPatientMeasurement),
            new { measurementId = measurement.Id },
            ToDto(measurement));
    }

    [HttpGet("api/health-core/measurements/{measurementId:guid}")]
    public async Task<ActionResult<PatientMeasurementDto>> GetPatientMeasurement(Guid measurementId)
    {
        var measurement = await _dbContext.PatientMeasurements
            .FirstOrDefaultAsync(x => x.Id == measurementId && !x.IsDeleted);

        if (measurement is null)
        {
            return NotFound(new
            {
                message = "Measurement not found."
            });
        }

        return Ok(ToDto(measurement));
    }

    [HttpPut("api/health-core/measurements/{measurementId:guid}")]
    public async Task<ActionResult<PatientMeasurementDto>> UpdatePatientMeasurement(
        Guid measurementId,
        UpdatePatientMeasurementRequest request)
    {
        var measurement = await _dbContext.PatientMeasurements
            .FirstOrDefaultAsync(x => x.Id == measurementId && !x.IsDeleted);

        if (measurement is null)
        {
            return NotFound(new
            {
                message = "Measurement not found."
            });
        }

        if (request.MeasurementType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.MeasurementType))
            {
                return BadRequest(new
                {
                    message = "Measurement type is required."
                });
            }

            measurement.MeasurementType = request.MeasurementType.Trim();
        }

        if (request.Value.HasValue)
        {
            measurement.Value = request.Value.Value;
        }

        if (request.Unit is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Unit))
            {
                return BadRequest(new
                {
                    message = "Measurement unit is required."
                });
            }

            measurement.Unit = request.Unit.Trim();
        }

        if (request.SourceType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SourceType))
            {
                return BadRequest(new
                {
                    message = "Source type is required."
                });
            }

            measurement.SourceType = request.SourceType.Trim();
        }

        if (request.VerificationStatus is not null)
        {
            if (string.IsNullOrWhiteSpace(request.VerificationStatus))
            {
                return BadRequest(new
                {
                    message = "Verification status is required."
                });
            }

            measurement.VerificationStatus = request.VerificationStatus.Trim();
        }

        if (request.SensitivityLevel is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SensitivityLevel))
            {
                return BadRequest(new
                {
                    message = "Sensitivity level is required."
                });
            }

            measurement.SensitivityLevel = request.SensitivityLevel.Trim();
        }

        if (request.DisplayName is not null)
        {
            measurement.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? measurement.MeasurementType
                : request.DisplayName.Trim();
        }

        if (request.MeasuredAt.HasValue)
        {
            measurement.MeasuredAt = request.MeasuredAt.Value.ToUniversalTime();
        }

        if (request.Method is not null)
        {
            measurement.Method = NormalizeOptional(request.Method);
        }

        if (request.BodySite is not null)
        {
            measurement.BodySite = NormalizeOptional(request.BodySite);
        }

        if (request.Context is not null)
        {
            measurement.Context = NormalizeOptional(request.Context);
        }

        if (request.ReferenceRange is not null)
        {
            measurement.ReferenceRange = NormalizeOptional(request.ReferenceRange);
        }

        if (request.IsAbnormal.HasValue)
        {
            measurement.IsAbnormal = request.IsAbnormal;
        }

        if (request.TargetMin.HasValue)
        {
            measurement.TargetMin = request.TargetMin;
        }

        if (request.TargetMax.HasValue)
        {
            measurement.TargetMax = request.TargetMax;
        }

        if (request.RelatedRecordType is not null)
        {
            measurement.RelatedRecordType = NormalizeOptional(request.RelatedRecordType);
        }

        if (request.RelatedRecordId.HasValue)
        {
            measurement.RelatedRecordId = request.RelatedRecordId;
        }

        if (string.IsNullOrWhiteSpace(measurement.DisplayName))
        {
            measurement.DisplayName = measurement.MeasurementType;
        }

        measurement.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(measurement));
    }

    [HttpDelete("api/health-core/measurements/{measurementId:guid}")]
    public async Task<IActionResult> DeletePatientMeasurement(Guid measurementId)
    {
        var measurement = await _dbContext.PatientMeasurements
            .FirstOrDefaultAsync(x => x.Id == measurementId && !x.IsDeleted);

        if (measurement is null)
        {
            return NotFound(new
            {
                message = "Measurement not found."
            });
        }

        var now = DateTimeOffset.UtcNow;

        measurement.IsDeleted = true;
        measurement.DeletedAt = now;
        measurement.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return NoContent();
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

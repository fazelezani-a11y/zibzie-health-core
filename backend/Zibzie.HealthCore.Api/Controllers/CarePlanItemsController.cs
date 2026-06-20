using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class CarePlanItemsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ICarePlanItemService _carePlanItemService;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public CarePlanItemsController(
        AppDbContext dbContext,
        ICarePlanItemService carePlanItemService,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _carePlanItemService = carePlanItemService;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/care-plan")]
    public async Task<ActionResult<List<CarePlanItemDto>>> GetPatientCarePlan(
        Guid patientId,
        [FromQuery] string? category = null,
        [FromQuery] string? itemType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTimeOffset? dueBefore = null,
        [FromQuery] DateTimeOffset? dueAfter = null)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewCarePlan);

        if (!accessDecision.IsAllowed)
        {
            await LogCarePlanAuditAsync(
                requestContext,
                patientId,
                null,
                HealthPermissions.ViewCarePlan,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
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

        var query = _dbContext.CarePlanItems
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim();
            query = query.Where(x => x.Category == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            var normalizedItemType = itemType.Trim();
            query = query.Where(x => x.ItemType == normalizedItemType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            var normalizedPriority = priority.Trim();
            query = query.Where(x => x.Priority == normalizedPriority);
        }

        if (dueBefore.HasValue)
        {
            var normalizedDueBefore = dueBefore.Value.ToUniversalTime();
            query = query.Where(x => x.DueAt.HasValue && x.DueAt <= normalizedDueBefore);
        }

        if (dueAfter.HasValue)
        {
            var normalizedDueAfter = dueAfter.Value.ToUniversalTime();
            query = query.Where(x => x.DueAt.HasValue && x.DueAt >= normalizedDueAfter);
        }

        var items = await query
            .OrderBy(x => x.Status == CarePlanStatuses.Completed || x.Status == CarePlanStatuses.Cancelled)
            .ThenBy(x => x.DueAt == null)
            .ThenBy(x => x.DueAt)
            .ThenBy(x =>
                x.Priority == CommonPriorities.Urgent ? 0 :
                x.Priority == CommonPriorities.High ? 1 :
                x.Priority == CommonPriorities.Normal ? 2 :
                x.Priority == CommonPriorities.Low ? 3 : 4)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new CarePlanItemDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                Category = x.Category,
                ItemType = x.ItemType,
                Title = x.Title,
                Description = x.Description,
                Reason = x.Reason,
                RequestedBy = x.RequestedBy,
                AssignedTo = x.AssignedTo,
                PlannedAt = x.PlannedAt,
                DueAt = x.DueAt,
                CompletedAt = x.CompletedAt,
                Status = x.Status,
                Priority = x.Priority,
                ResultSummary = x.ResultSummary,
                NextAction = x.NextAction,
                RelatedRecordType = x.RelatedRecordType,
                RelatedRecordId = x.RelatedRecordId,
                SourceType = x.SourceType,
                VerificationStatus = x.VerificationStatus,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        await LogCarePlanAuditAsync(
            requestContext,
            patientId,
            null,
            HealthPermissions.ViewCarePlan,
            AuditActionTypes.View,
            true,
            accessDecision);

        return Ok(items);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/care-plan")]
    public async Task<ActionResult<CarePlanItemDto>> CreateCarePlanItem(
        Guid patientId,
        CreateCarePlanItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new
            {
                message = "Care plan category is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ItemType))
        {
            return BadRequest(new
            {
                message = "Care plan item type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Care plan title is required."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.CreateCarePlanItem,
            request.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogCarePlanAuditAsync(
                requestContext,
                patientId,
                null,
                HealthPermissions.CreateCarePlanItem,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var dto = await _carePlanItemService.CreateCarePlanItemAsync(patientId, request);

        if (dto is null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        await LogCarePlanAuditAsync(
            requestContext,
            patientId,
            dto.Id,
            HealthPermissions.CreateCarePlanItem,
            AuditActionTypes.Create,
            true,
            accessDecision);

        return CreatedAtAction(
            nameof(GetCarePlanItem),
            new { itemId = dto.Id },
            dto);
    }

    [HttpGet("api/health-core/care-plan-items/{itemId:guid}")]
    public async Task<ActionResult<CarePlanItemDto>> GetCarePlanItem(Guid itemId)
    {
        var item = await _dbContext.CarePlanItems
            .FirstOrDefaultAsync(x => x.Id == itemId && !x.IsDeleted);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Care plan item not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(item.PatientProfileId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewCarePlan,
            item.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogCarePlanAuditAsync(
                requestContext,
                item.PatientProfileId,
                item.Id,
                HealthPermissions.ViewCarePlan,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        await LogCarePlanAuditAsync(
            requestContext,
            item.PatientProfileId,
            item.Id,
            HealthPermissions.ViewCarePlan,
            AuditActionTypes.View,
            true,
            accessDecision);

        return Ok(ToDto(item));
    }

    [HttpPut("api/health-core/care-plan-items/{itemId:guid}")]
    public async Task<ActionResult<CarePlanItemDto>> UpdateCarePlanItem(
        Guid itemId,
        UpdateCarePlanItemRequest request)
    {
        var item = await _dbContext.CarePlanItems
            .FirstOrDefaultAsync(x => x.Id == itemId && !x.IsDeleted);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Care plan item not found."
            });
        }

        var permission = GetCarePlanUpdatePermission(request.Status);
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(item.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            permission,
            request.SensitivityLevel ?? item.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogCarePlanAuditAsync(
                requestContext,
                item.PatientProfileId,
                item.Id,
                permission,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var statusChangedToCompleted = false;

        if (request.Category is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                return BadRequest(new
                {
                    message = "Care plan category is required."
                });
            }

            item.Category = request.Category.Trim();
        }

        if (request.ItemType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ItemType))
            {
                return BadRequest(new
                {
                    message = "Care plan item type is required."
                });
            }

            item.ItemType = request.ItemType.Trim();
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Care plan title is required."
                });
            }

            item.Title = request.Title.Trim();
        }

        if (request.Status is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new
                {
                    message = "Care plan status is required."
                });
            }

            var normalizedStatus = request.Status.Trim();
            statusChangedToCompleted =
                !string.Equals(item.Status, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(normalizedStatus, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase);
            item.Status = normalizedStatus;
        }

        if (request.Priority is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Priority))
            {
                return BadRequest(new
                {
                    message = "Care plan priority is required."
                });
            }

            item.Priority = request.Priority.Trim();
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

            item.SourceType = request.SourceType.Trim();
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

            item.VerificationStatus = request.VerificationStatus.Trim();
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

            item.SensitivityLevel = request.SensitivityLevel.Trim();
        }

        if (request.PlannedAt.HasValue)
        {
            item.PlannedAt = request.PlannedAt.Value.ToUniversalTime();
        }

        if (request.DueAt.HasValue)
        {
            item.DueAt = request.DueAt.Value.ToUniversalTime();
        }

        if (request.CompletedAt.HasValue)
        {
            item.CompletedAt = request.CompletedAt.Value.ToUniversalTime();
        }

        if (request.Description is not null)
        {
            item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Reason is not null)
        {
            item.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        }

        if (request.RequestedBy is not null)
        {
            item.RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? null : request.RequestedBy.Trim();
        }

        if (request.AssignedTo is not null)
        {
            item.AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? null : request.AssignedTo.Trim();
        }

        if (request.ResultSummary is not null)
        {
            item.ResultSummary = string.IsNullOrWhiteSpace(request.ResultSummary) ? null : request.ResultSummary.Trim();
        }

        if (request.NextAction is not null)
        {
            item.NextAction = string.IsNullOrWhiteSpace(request.NextAction) ? null : request.NextAction.Trim();
        }

        if (request.RelatedRecordType is not null)
        {
            item.RelatedRecordType = string.IsNullOrWhiteSpace(request.RelatedRecordType) ? null : request.RelatedRecordType.Trim();
        }

        if (request.RelatedRecordId.HasValue)
        {
            item.RelatedRecordId = request.RelatedRecordId;
        }

        var now = DateTimeOffset.UtcNow;

        if (statusChangedToCompleted && !item.CompletedAt.HasValue)
        {
            item.CompletedAt = now;
        }

        item.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        await LogCarePlanAuditAsync(
            requestContext,
            item.PatientProfileId,
            item.Id,
            permission,
            AuditActionTypes.Update,
            true,
            accessDecision);

        return Ok(ToDto(item));
    }

    [HttpDelete("api/health-core/care-plan-items/{itemId:guid}")]
    public async Task<IActionResult> DeleteCarePlanItem(Guid itemId)
    {
        var item = await _dbContext.CarePlanItems
            .FirstOrDefaultAsync(x => x.Id == itemId && !x.IsDeleted);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Care plan item not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(item.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditCarePlanItem,
            item.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogCarePlanAuditAsync(
                requestContext,
                item.PatientProfileId,
                item.Id,
                HealthPermissions.EditCarePlanItem,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var now = DateTimeOffset.UtcNow;

        item.IsDeleted = true;
        item.DeletedAt = now;
        item.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        await LogCarePlanAuditAsync(
            requestContext,
            item.PatientProfileId,
            item.Id,
            HealthPermissions.EditCarePlanItem,
            AuditActionTypes.Delete,
            true,
            accessDecision);

        return NoContent();
    }

    private async Task LogCarePlanAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid patientId,
        Guid? itemId,
        string permission,
        string actionType,
        bool succeeded,
        AccessDecision? accessDecision = null)
    {
        await _auditLogService.LogAsync(new AuditLogRequest
        {
            UserId = requestContext.UserId,
            ServiceAccountId = requestContext.ServiceAccountId,
            PatientId = patientId,
            ProductCode = requestContext.ProductCode,
            ProductRole = requestContext.ProductRole,
            ActionType = actionType,
            ResourceType = AuditResourceTypes.CarePlanItem,
            ResourceId = itemId,
            Permission = permission,
            AccessScope = accessDecision?.MatchedScope,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : accessDecision?.DenialReason,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod
        });
    }

    private static string GetCarePlanUpdatePermission(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return HealthPermissions.EditCarePlanItem;
        }

        var normalizedStatus = status.Trim();

        if (string.Equals(normalizedStatus, CarePlanStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return HealthPermissions.CompleteCarePlanItem;
        }

        if (string.Equals(normalizedStatus, CarePlanStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return HealthPermissions.CancelCarePlanItem;
        }

        return HealthPermissions.EditCarePlanItem;
    }

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
    }

    private static CarePlanItemDto ToDto(CarePlanItem item)
    {
        return new CarePlanItemDto
        {
            Id = item.Id,
            PatientProfileId = item.PatientProfileId,
            Category = item.Category,
            ItemType = item.ItemType,
            Title = item.Title,
            Description = item.Description,
            Reason = item.Reason,
            RequestedBy = item.RequestedBy,
            AssignedTo = item.AssignedTo,
            PlannedAt = item.PlannedAt,
            DueAt = item.DueAt,
            CompletedAt = item.CompletedAt,
            Status = item.Status,
            Priority = item.Priority,
            ResultSummary = item.ResultSummary,
            NextAction = item.NextAction,
            RelatedRecordType = item.RelatedRecordType,
            RelatedRecordId = item.RelatedRecordId,
            SourceType = item.SourceType,
            VerificationStatus = item.VerificationStatus,
            SensitivityLevel = item.SensitivityLevel,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}

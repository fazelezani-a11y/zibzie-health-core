using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class PatientAccessGrantsController : ControllerBase
{
    private static readonly Guid GrantManagementAuthorizationPatientId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly IPatientAccessGrantService _patientAccessGrantService;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public PatientAccessGrantsController(
        IPatientAccessGrantService patientAccessGrantService,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _patientAccessGrantService = patientAccessGrantService;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/access-grants")]
    public async Task<ActionResult<IReadOnlyCollection<PatientAccessGrantDto>>> GetPatientAccessGrants(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var accessDecision = await AuthorizeAsync(
            patientId,
            HealthPermissions.ViewPatientAccessGrants,
            cancellationToken);

        if (!accessDecision.IsAllowed)
        {
            await LogGrantAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.ViewPatientAccessGrants,
                false,
                accessDecision);

            return AccessDenied();
        }

        var result = await _patientAccessGrantService.ListForPatientAsync(patientId, cancellationToken);

        if (!result.Succeeded)
        {
            await LogGrantAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.View,
                HealthPermissions.ViewPatientAccessGrants,
                false,
                accessDecision,
                result.ErrorMessage);

            return ToErrorResult(result);
        }

        await LogGrantAuditAsync(
            requestContext,
            patientId,
            null,
            AuditActionTypes.View,
            HealthPermissions.ViewPatientAccessGrants,
            true,
            accessDecision);

        return Ok(result.Value);
    }

    [HttpGet("api/health-core/access-grants/{grantId:guid}")]
    public async Task<ActionResult<PatientAccessGrantDto>> GetPatientAccessGrant(
        Guid grantId,
        CancellationToken cancellationToken)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var grantResult = await _patientAccessGrantService.GetByIdAsync(grantId, cancellationToken);
        var authorizationPatientId = grantResult.Value?.PatientId ?? GrantManagementAuthorizationPatientId;
        var accessDecision = await AuthorizeAsync(
            authorizationPatientId,
            HealthPermissions.ViewPatientAccessGrants,
            cancellationToken);

        if (!accessDecision.IsAllowed)
        {
            await LogGrantAuditAsync(
                requestContext,
                grantResult.Value?.PatientId,
                grantId,
                AuditActionTypes.AccessDenied,
                HealthPermissions.ViewPatientAccessGrants,
                false,
                accessDecision);

            return AccessDenied();
        }

        if (!grantResult.Succeeded)
        {
            await LogGrantAuditAsync(
                requestContext,
                null,
                grantId,
                AuditActionTypes.View,
                HealthPermissions.ViewPatientAccessGrants,
                false,
                accessDecision,
                grantResult.ErrorMessage);

            return ToErrorResult(grantResult);
        }

        await LogGrantAuditAsync(
            requestContext,
            grantResult.Value!.PatientId,
            grantId,
            AuditActionTypes.View,
            HealthPermissions.ViewPatientAccessGrants,
            true,
            accessDecision,
            metadataJson: CreateGrantAuditMetadata(grantResult.Value));

        return Ok(grantResult.Value);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/access-grants")]
    public async Task<ActionResult<PatientAccessGrantDto>> CreatePatientAccessGrant(
        Guid patientId,
        CreatePatientAccessGrantRequest request,
        CancellationToken cancellationToken)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var accessDecision = await AuthorizeAsync(
            patientId,
            HealthPermissions.CreatePatientAccessGrant,
            cancellationToken);

        if (!accessDecision.IsAllowed)
        {
            await LogGrantAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.CreatePatientAccessGrant,
                false,
                accessDecision,
                metadataJson: CreateGrantAuditMetadata(request));

            return AccessDenied();
        }

        var result = await _patientAccessGrantService.CreateAsync(
            patientId,
            request,
            requestContext,
            cancellationToken);

        if (!result.Succeeded)
        {
            await LogGrantAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.GrantAccess,
                HealthPermissions.CreatePatientAccessGrant,
                false,
                accessDecision,
                result.ErrorMessage,
                CreateGrantAuditMetadata(request));

            return ToErrorResult(result);
        }

        await LogGrantAuditAsync(
            requestContext,
            patientId,
            result.Value!.Id,
            AuditActionTypes.GrantAccess,
            HealthPermissions.CreatePatientAccessGrant,
            true,
            accessDecision,
            metadataJson: CreateGrantAuditMetadata(result.Value));

        return CreatedAtAction(
            nameof(GetPatientAccessGrant),
            new { grantId = result.Value.Id },
            result.Value);
    }

    [HttpPost("api/health-core/access-grants/{grantId:guid}/revoke")]
    public async Task<ActionResult<PatientAccessGrantDto>> RevokePatientAccessGrant(
        Guid grantId,
        RevokePatientAccessGrantRequest request,
        CancellationToken cancellationToken)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var grantResult = await _patientAccessGrantService.GetByIdAsync(grantId, cancellationToken);
        var authorizationPatientId = grantResult.Value?.PatientId ?? GrantManagementAuthorizationPatientId;
        var accessDecision = await AuthorizeAsync(
            authorizationPatientId,
            HealthPermissions.RevokePatientAccessGrant,
            cancellationToken);

        if (!accessDecision.IsAllowed)
        {
            await LogGrantAuditAsync(
                requestContext,
                grantResult.Value?.PatientId,
                grantId,
                AuditActionTypes.AccessDenied,
                HealthPermissions.RevokePatientAccessGrant,
                false,
                accessDecision,
                metadataJson: grantResult.Value is null ? null : CreateGrantAuditMetadata(grantResult.Value));

            return AccessDenied();
        }

        if (!grantResult.Succeeded)
        {
            await LogGrantAuditAsync(
                requestContext,
                null,
                grantId,
                AuditActionTypes.RevokeAccess,
                HealthPermissions.RevokePatientAccessGrant,
                false,
                accessDecision,
                grantResult.ErrorMessage);

            return ToErrorResult(grantResult);
        }

        var existingGrant = grantResult.Value!;
        var result = await _patientAccessGrantService.RevokeAsync(
            grantId,
            request,
            requestContext,
            cancellationToken);

        if (!result.Succeeded)
        {
            await LogGrantAuditAsync(
                requestContext,
                existingGrant.PatientId,
                grantId,
                AuditActionTypes.RevokeAccess,
                HealthPermissions.RevokePatientAccessGrant,
                false,
                accessDecision,
                result.ErrorMessage,
                CreateGrantAuditMetadata(existingGrant));

            return ToErrorResult(result);
        }

        await LogGrantAuditAsync(
            requestContext,
            result.Value!.PatientId,
            grantId,
            AuditActionTypes.RevokeAccess,
            HealthPermissions.RevokePatientAccessGrant,
            true,
            accessDecision,
            metadataJson: CreateGrantAuditMetadata(result.Value));

        return Ok(result.Value);
    }

    private async Task<AccessDecision> AuthorizeAsync(
        Guid patientId,
        string permission,
        CancellationToken cancellationToken)
    {
        return await _authorizationService.HasPermissionAsync(
            _requestContextProvider.CreateAuthorizationContext(patientId),
            permission,
            cancellationToken);
    }

    private async Task LogGrantAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid? patientId,
        Guid? grantId,
        string actionType,
        string permission,
        bool succeeded,
        AccessDecision? accessDecision,
        string? failureReason = null,
        string? metadataJson = null)
    {
        await _auditLogService.LogAsync(new AuditLogRequest
        {
            UserId = requestContext.UserId,
            ServiceAccountId = requestContext.ServiceAccountId,
            PatientId = patientId,
            ProductCode = requestContext.ProductCode,
            ProductRole = requestContext.ProductRole,
            ActionType = actionType,
            ResourceType = AuditResourceTypes.PatientAccessGrant,
            ResourceId = grantId,
            Permission = permission,
            AccessScope = accessDecision?.MatchedScope,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : failureReason ?? accessDecision?.DenialReason ?? "Access denied.",
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod,
            MetadataJson = metadataJson
        });
    }

    private ObjectResult ToErrorResult<T>(PatientAccessGrantServiceResult<T> result)
    {
        var body = new
        {
            message = result.ErrorMessage ?? "Patient access grant operation failed."
        };

        return result.Error switch
        {
            PatientAccessGrantServiceError.NotFound => NotFound(body),
            PatientAccessGrantServiceError.Conflict => Conflict(body),
            PatientAccessGrantServiceError.Validation => BadRequest(body),
            _ => BadRequest(body)
        };
    }

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
    }

    private static string CreateGrantAuditMetadata(CreatePatientAccessGrantRequest request)
    {
        return JsonSerializer.Serialize(new
        {
            request.ProductCode,
            request.ProductRole,
            request.Scope,
            request.Reason,
            request.GranteeUserId,
            serviceAccountId = string.IsNullOrWhiteSpace(request.ServiceAccountId)
                ? null
                : request.ServiceAccountId.Trim(),
            hasValidUntil = request.ValidUntil.HasValue
        });
    }

    private static string CreateGrantAuditMetadata(PatientAccessGrantDto grant)
    {
        return JsonSerializer.Serialize(new
        {
            grant.Id,
            grant.ProductCode,
            grant.ProductRole,
            grant.Scope,
            grant.Reason,
            grant.GranteeUserId,
            grant.ServiceAccountId,
            grant.IsActive,
            hasValidUntil = grant.ValidUntil.HasValue
        });
    }
}

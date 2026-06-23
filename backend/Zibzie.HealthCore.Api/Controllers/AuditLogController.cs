using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
[Route("api/health-core/audit-log")]
public class AuditLogController : ControllerBase
{
    private static readonly Guid AuditReviewAuthorizationPatientId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;

    private readonly AppDbContext _dbContext;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(
        AppDbContext dbContext,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogQueryResponse>> GetAuditLog(
        [FromQuery] AuditLogQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            return BadRequest(new
            {
                message = "The from filter must be before the to filter."
            });
        }

        var page = Math.Max(request.Page ?? DefaultPage, DefaultPage);
        var pageSize = Math.Clamp(request.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationPatientId = request.PatientId ?? AuditReviewAuthorizationPatientId;
        var accessDecision = await _authorizationService.HasPermissionAsync(
            _requestContextProvider.CreateAuthorizationContext(authorizationPatientId),
            HealthPermissions.ViewAuditLog,
            cancellationToken);

        if (!accessDecision.IsAllowed)
        {
            await LogAuditReviewAsync(
                requestContext,
                request.PatientId,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision,
                CreateAuditReviewMetadata(request, page, pageSize));

            return AccessDenied();
        }

        var query = _dbContext.AuditLogEntries
            .AsNoTracking()
            .AsQueryable();

        if (request.PatientId.HasValue)
        {
            query = query.Where(entry => entry.PatientId == request.PatientId);
        }

        if (request.ActorUserId.HasValue)
        {
            query = query.Where(entry => entry.UserId == request.ActorUserId);
        }

        if (!string.IsNullOrWhiteSpace(request.ActorServiceAccountId))
        {
            var actorServiceAccountId = request.ActorServiceAccountId.Trim();
            query = query.Where(entry => entry.ServiceAccountId == actorServiceAccountId);
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            var actionType = request.ActionType.Trim();
            query = query.Where(entry => entry.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(request.ResourceType))
        {
            var resourceType = request.ResourceType.Trim();
            query = query.Where(entry => entry.ResourceType == resourceType);
        }

        if (request.From.HasValue)
        {
            query = query.Where(entry => entry.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(entry => entry.CreatedAt <= request.To.Value);
        }

        if (request.Succeeded.HasValue)
        {
            query = query.Where(entry => entry.Succeeded == request.Succeeded.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entry => entry.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new AuditLogEntryDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                ServiceAccountId = entry.ServiceAccountId,
                PatientId = entry.PatientId,
                ProductCode = entry.ProductCode,
                ProductRole = entry.ProductRole,
                ActionType = entry.ActionType,
                ResourceType = entry.ResourceType,
                ResourceId = entry.ResourceId,
                Permission = entry.Permission,
                AccessScope = entry.AccessScope,
                AuthorizationReason = entry.AuthorizationReason,
                Succeeded = entry.Succeeded,
                FailureReason = entry.FailureReason,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                CorrelationId = entry.CorrelationId,
                RequestPath = entry.RequestPath,
                HttpMethod = entry.HttpMethod,
                CreatedAt = entry.CreatedAt
            })
            .ToListAsync(cancellationToken);

        await LogAuditReviewAsync(
            requestContext,
            request.PatientId,
            AuditActionTypes.View,
            true,
            accessDecision,
            CreateAuditReviewMetadata(request, page, pageSize, totalCount));

        return Ok(new AuditLogQueryResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    private async Task LogAuditReviewAsync(
        HealthCoreRequestContext requestContext,
        Guid? patientId,
        string actionType,
        bool succeeded,
        AccessDecision? accessDecision,
        string metadataJson)
    {
        await _auditLogService.LogAsync(new AuditLogRequest
        {
            UserId = requestContext.UserId,
            ServiceAccountId = requestContext.ServiceAccountId,
            PatientId = patientId,
            ProductCode = requestContext.ProductCode,
            ProductRole = requestContext.ProductRole,
            ActionType = actionType,
            ResourceType = AuditResourceTypes.AuditLog,
            Permission = HealthPermissions.ViewAuditLog,
            AccessScope = accessDecision?.MatchedScope,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : accessDecision?.DenialReason ?? "Access denied.",
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod,
            MetadataJson = metadataJson
        });
    }

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
    }

    private static string CreateAuditReviewMetadata(
        AuditLogQueryRequest request,
        int page,
        int pageSize,
        int? totalCount = null)
    {
        return JsonSerializer.Serialize(new
        {
            request.PatientId,
            request.ActorUserId,
            hasActorServiceAccountId = !string.IsNullOrWhiteSpace(request.ActorServiceAccountId),
            actionType = string.IsNullOrWhiteSpace(request.ActionType) ? null : request.ActionType.Trim(),
            resourceType = string.IsNullOrWhiteSpace(request.ResourceType) ? null : request.ResourceType.Trim(),
            request.From,
            request.To,
            request.Succeeded,
            page,
            pageSize,
            totalCount
        });
    }
}

public sealed class AuditLogQueryRequest
{
    public Guid? PatientId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? ActorServiceAccountId { get; set; }

    public string? ActionType { get; set; }

    public string? ResourceType { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public bool? Succeeded { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}

public sealed record AuditLogQueryResponse
{
    public IReadOnlyCollection<AuditLogEntryDto> Items { get; init; } = Array.Empty<AuditLogEntryDto>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}

public sealed record AuditLogEntryDto
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string? ServiceAccountId { get; init; }

    public Guid? PatientId { get; init; }

    public string? ProductCode { get; init; }

    public string? ProductRole { get; init; }

    public string ActionType { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public Guid? ResourceId { get; init; }

    public string? Permission { get; init; }

    public string? AccessScope { get; init; }

    public string? AuthorizationReason { get; init; }

    public bool Succeeded { get; init; }

    public string? FailureReason { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }

    public string? RequestPath { get; init; }

    public string? HttpMethod { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

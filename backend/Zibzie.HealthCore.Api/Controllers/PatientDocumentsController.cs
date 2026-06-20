using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Documents;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class PatientDocumentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPatientDocumentService _patientDocumentService;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public PatientDocumentsController(
        AppDbContext dbContext,
        IPatientDocumentService patientDocumentService,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _patientDocumentService = patientDocumentService;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/documents")]
    public async Task<ActionResult<List<PatientDocumentDto>>> GetPatientDocuments(
        Guid patientId,
        [FromQuery] string? documentType = null,
        [FromQuery] string? verificationStatus = null,
        [FromQuery] string? sensitivityLevel = null)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewDocuments,
            sensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogDocumentAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.ViewDocuments,
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

        var query = _dbContext.PatientDocuments
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            var normalizedDocumentType = documentType.Trim();
            query = query.Where(x => x.DocumentType == normalizedDocumentType);
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

        var documents = await query
            .OrderBy(x => x.DocumentDate == null)
            .ThenByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PatientDocumentDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                DocumentType = x.DocumentType,
                Title = x.Title,
                Description = x.Description,
                DocumentDate = x.DocumentDate,
                IssuerName = x.IssuerName,
                FileName = x.FileName,
                FileUrl = x.FileUrl,
                FileReference = x.FileReference,
                MimeType = x.MimeType,
                FileSizeBytes = x.FileSizeBytes,
                SourceType = x.SourceType,
                VerificationStatus = x.VerificationStatus,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        await LogDocumentAuditAsync(
            requestContext,
            patientId,
            null,
            AuditActionTypes.View,
            HealthPermissions.ViewDocuments,
            true,
            accessDecision);

        return Ok(documents);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/documents")]
    public async Task<ActionResult<PatientDocumentDto>> CreatePatientDocument(
        Guid patientId,
        CreatePatientDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            return BadRequest(new
            {
                message = "Document type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Document title is required."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.UploadDocuments,
            request.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogDocumentAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.UploadDocuments,
                false,
                accessDecision);

            return AccessDenied();
        }

        var dto = await _patientDocumentService.CreatePatientDocumentAsync(patientId, request);

        if (dto is null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        await LogDocumentAuditAsync(
            requestContext,
            patientId,
            dto.Id,
            AuditActionTypes.Create,
            HealthPermissions.UploadDocuments,
            true,
            accessDecision);

        return CreatedAtAction(
            nameof(GetPatientDocument),
            new { documentId = dto.Id },
            dto);
    }

    [HttpGet("api/health-core/documents/{documentId:guid}")]
    public async Task<ActionResult<PatientDocumentDto>> GetPatientDocument(Guid documentId)
    {
        var document = await _dbContext.PatientDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && !x.IsDeleted);

        if (document is null)
        {
            return NotFound(new
            {
                message = "Document not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(document.PatientProfileId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewDocuments,
            document.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogDocumentAuditAsync(
                requestContext,
                document.PatientProfileId,
                document.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.ViewDocuments,
                false,
                accessDecision);

            return AccessDenied();
        }

        await LogDocumentAuditAsync(
            requestContext,
            document.PatientProfileId,
            document.Id,
            AuditActionTypes.View,
            HealthPermissions.ViewDocuments,
            true,
            accessDecision);

        return Ok(ToDto(document));
    }

    [HttpPut("api/health-core/documents/{documentId:guid}")]
    public async Task<ActionResult<PatientDocumentDto>> UpdatePatientDocument(
        Guid documentId,
        UpdatePatientDocumentRequest request)
    {
        var document = await _dbContext.PatientDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && !x.IsDeleted);

        if (document is null)
        {
            return NotFound(new
            {
                message = "Document not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(document.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditDocuments,
            request.SensitivityLevel ?? document.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogDocumentAuditAsync(
                requestContext,
                document.PatientProfileId,
                document.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.EditDocuments,
                false,
                accessDecision);

            return AccessDenied();
        }

        if (request.DocumentType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.DocumentType))
            {
                return BadRequest(new
                {
                    message = "Document type is required."
                });
            }

            document.DocumentType = request.DocumentType.Trim();
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Document title is required."
                });
            }

            document.Title = request.Title.Trim();
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

            document.SourceType = request.SourceType.Trim();
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

            document.VerificationStatus = request.VerificationStatus.Trim();
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

            document.SensitivityLevel = request.SensitivityLevel.Trim();
        }

        if (request.DocumentDate.HasValue)
        {
            document.DocumentDate = request.DocumentDate.Value.ToUniversalTime();
        }

        if (request.Description is not null)
        {
            document.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.IssuerName is not null)
        {
            document.IssuerName = string.IsNullOrWhiteSpace(request.IssuerName) ? null : request.IssuerName.Trim();
        }

        if (request.FileName is not null)
        {
            document.FileName = string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim();
        }

        if (request.FileUrl is not null)
        {
            document.FileUrl = string.IsNullOrWhiteSpace(request.FileUrl) ? null : request.FileUrl.Trim();
        }

        if (request.FileReference is not null)
        {
            document.FileReference = string.IsNullOrWhiteSpace(request.FileReference) ? null : request.FileReference.Trim();
        }

        if (request.MimeType is not null)
        {
            document.MimeType = string.IsNullOrWhiteSpace(request.MimeType) ? null : request.MimeType.Trim();
        }

        if (request.FileSizeBytes.HasValue)
        {
            document.FileSizeBytes = request.FileSizeBytes;
        }

        document.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        await LogDocumentAuditAsync(
            requestContext,
            document.PatientProfileId,
            document.Id,
            AuditActionTypes.Update,
            HealthPermissions.EditDocuments,
            true,
            accessDecision);

        return Ok(ToDto(document));
    }

    [HttpDelete("api/health-core/documents/{documentId:guid}")]
    public async Task<IActionResult> DeletePatientDocument(Guid documentId)
    {
        var document = await _dbContext.PatientDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && !x.IsDeleted);

        if (document is null)
        {
            return NotFound(new
            {
                message = "Document not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(document.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.DeleteDocuments,
            document.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogDocumentAuditAsync(
                requestContext,
                document.PatientProfileId,
                document.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.DeleteDocuments,
                false,
                accessDecision);

            return AccessDenied();
        }

        var now = DateTimeOffset.UtcNow;

        document.IsDeleted = true;
        document.DeletedAt = now;
        document.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        await LogDocumentAuditAsync(
            requestContext,
            document.PatientProfileId,
            document.Id,
            AuditActionTypes.Delete,
            HealthPermissions.DeleteDocuments,
            true,
            accessDecision);

        return NoContent();
    }

    private async Task LogDocumentAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid? patientId,
        Guid? documentId,
        string actionType,
        string permission,
        bool succeeded,
        AccessDecision? accessDecision)
    {
        await _auditLogService.LogAsync(new AuditLogRequest
        {
            UserId = requestContext.UserId,
            ServiceAccountId = requestContext.ServiceAccountId,
            PatientId = patientId,
            ProductCode = requestContext.ProductCode,
            ProductRole = requestContext.ProductRole,
            ActionType = actionType,
            ResourceType = AuditResourceTypes.Document,
            ResourceId = documentId,
            Permission = permission,
            AccessScope = accessDecision?.MatchedScope,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : accessDecision?.DenialReason ?? "Access denied.",
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod
        });
    }

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
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

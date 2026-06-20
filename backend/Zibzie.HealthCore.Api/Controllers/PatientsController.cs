using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Zibzie.HealthCore.Application.Patients;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
[Route("api/health-core/patients")]
public class PatientsController : ControllerBase
{
    private static readonly Guid DirectoryAuthorizationPatientId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly AppDbContext _dbContext;
    private readonly IPatientSummaryService _patientSummaryService;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public PatientsController(
        AppDbContext dbContext,
        IPatientSummaryService patientSummaryService,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _patientSummaryService = patientSummaryService;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PatientListItemDto>>> GetPatients(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 20;
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = CreateDirectoryAuthorizationContext(requestContext);
        var accessDecision = await _authorizationService.HasPermissionAsync(
            authorizationContext,
            HealthPermissions.ViewPatientDirectory);

        if (!accessDecision.IsAllowed)
        {
            await LogPatientProfileAuditAsync(
                requestContext,
                null,
                null,
                HealthPermissions.ViewPatientDirectory,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision,
                CreateDirectoryAuditMetadata(page, pageSize, search));

            return AccessDenied();
        }

        var query = _dbContext.PatientProfiles
            .Include(x => x.ContactInfo)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            query = query.Where(x =>
                x.FirstName.Contains(normalizedSearch) ||
                x.LastName.Contains(normalizedSearch) ||
                (x.NationalCode != null && x.NationalCode.Contains(normalizedSearch)) ||
                (x.ContactInfo != null && x.ContactInfo.MobileNumber.Contains(normalizedSearch)));
        }

        var patients = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PatientListItemDto
            {
                Id = x.Id,
                FullName = (x.FirstName + " " + x.LastName).Trim(),
                BirthDate = x.BirthDate,
                NationalCode = x.NationalCode,
                MobileNumber = x.ContactInfo != null ? x.ContactInfo.MobileNumber : string.Empty,
                IsActive = x.IsActive
            })
            .ToListAsync();

        await LogPatientProfileAuditAsync(
            requestContext,
            null,
            null,
            HealthPermissions.ViewPatientDirectory,
            AuditActionTypes.View,
            true,
            accessDecision,
            CreateDirectoryAuditMetadata(page, pageSize, search));

        return Ok(patients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDetailsDto>> GetPatientById(Guid id)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(id);
        var accessDecision = await _authorizationService.HasPermissionAsync(
            authorizationContext,
            HealthPermissions.ViewPatientProfile);

        if (!accessDecision.IsAllowed)
        {
            await LogPatientProfileAuditAsync(
                requestContext,
                id,
                id,
                HealthPermissions.ViewPatientProfile,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var patient = await _dbContext.PatientProfiles
            .Include(x => x.ContactInfo)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (patient is null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var dto = new PatientDetailsDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            BirthDate = patient.BirthDate,
            NationalCode = patient.NationalCode,
            Gender = patient.Gender,
            BloodType = patient.BloodType,
            MaritalStatus = patient.MaritalStatus,
            EducationLevel = patient.EducationLevel,
            Occupation = patient.Occupation,
            MobileNumber = patient.ContactInfo?.MobileNumber ?? string.Empty,
            Email = patient.ContactInfo?.Email,
            EmergencyContactName = patient.ContactInfo?.EmergencyContactName,
            EmergencyContactPhone = patient.ContactInfo?.EmergencyContactPhone,
            HomeAddress = patient.ContactInfo?.HomeAddress,
            WorkAddress = patient.ContactInfo?.WorkAddress,
            IsActive = patient.IsActive,
            CreatedAt = patient.CreatedAt
        };

        await LogPatientProfileAuditAsync(
            requestContext,
            id,
            id,
            HealthPermissions.ViewPatientProfile,
            AuditActionTypes.View,
            true,
            accessDecision);

        return Ok(dto);
    }

    [HttpGet("{patientId:guid}/summary")]
    public async Task<ActionResult<PatientSummaryDto>> GetPatientSummary(Guid patientId)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.HasPermissionAsync(
            authorizationContext,
            HealthPermissions.ViewPatientSummary);

        if (!accessDecision.IsAllowed)
        {
            await LogPatientSummaryAuditAsync(
                requestContext,
                patientId,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var dto = await _patientSummaryService.GetPatientSummaryAsync(patientId);

        if (dto is null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        await LogPatientSummaryAuditAsync(
            requestContext,
            patientId,
            AuditActionTypes.View,
            true,
            accessDecision);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<PatientDetailsDto>> CreatePatient(CreatePatientRequest request)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = CreateDirectoryAuthorizationContext(requestContext);
        var accessDecision = await _authorizationService.HasPermissionAsync(
            authorizationContext,
            HealthPermissions.CreatePatient);

        if (!accessDecision.IsAllowed)
        {
            await LogPatientProfileAuditAsync(
                requestContext,
                null,
                null,
                HealthPermissions.CreatePatient,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest(new { message = "First name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { message = "Last name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            return BadRequest(new { message = "Mobile number is required." });
        }

        var mobileNumber = request.MobileNumber.Trim();

        var duplicateMobile = await _dbContext.ContactInfos
            .AnyAsync(x => x.MobileNumber == mobileNumber);

        if (duplicateMobile)
        {
            return Conflict(new
            {
                message = "A patient with this mobile number already exists."
            });
        }

        var now = DateTime.UtcNow;

        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            BirthDate = request.BirthDate,
            NationalCode = string.IsNullOrWhiteSpace(request.NationalCode) ? null : request.NationalCode.Trim(),
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
            BloodType = string.IsNullOrWhiteSpace(request.BloodType) ? null : request.BloodType.Trim(),
            MaritalStatus = string.IsNullOrWhiteSpace(request.MaritalStatus) ? null : request.MaritalStatus.Trim(),
            EducationLevel = string.IsNullOrWhiteSpace(request.EducationLevel) ? null : request.EducationLevel.Trim(),
            Occupation = string.IsNullOrWhiteSpace(request.Occupation) ? null : request.Occupation.Trim(),
            IsActive = true,
            CreatedAt = now,
            ContactInfo = new ContactInfo
            {
                Id = Guid.NewGuid(),
                MobileNumber = mobileNumber,
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName) ? null : request.EmergencyContactName.Trim(),
                EmergencyContactPhone = string.IsNullOrWhiteSpace(request.EmergencyContactPhone) ? null : request.EmergencyContactPhone.Trim(),
                HomeAddress = string.IsNullOrWhiteSpace(request.HomeAddress) ? null : request.HomeAddress.Trim(),
                WorkAddress = string.IsNullOrWhiteSpace(request.WorkAddress) ? null : request.WorkAddress.Trim(),
                CreatedAt = now
            }
        };

        _dbContext.PatientProfiles.Add(patient);
        await _dbContext.SaveChangesAsync();

        var dto = new PatientDetailsDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            BirthDate = patient.BirthDate,
            NationalCode = patient.NationalCode,
            Gender = patient.Gender,
            BloodType = patient.BloodType,
            MaritalStatus = patient.MaritalStatus,
            EducationLevel = patient.EducationLevel,
            Occupation = patient.Occupation,
            MobileNumber = patient.ContactInfo.MobileNumber,
            Email = patient.ContactInfo.Email,
            EmergencyContactName = patient.ContactInfo.EmergencyContactName,
            EmergencyContactPhone = patient.ContactInfo.EmergencyContactPhone,
            HomeAddress = patient.ContactInfo.HomeAddress,
            WorkAddress = patient.ContactInfo.WorkAddress,
            IsActive = patient.IsActive,
            CreatedAt = patient.CreatedAt
        };

        await LogPatientProfileAuditAsync(
            requestContext,
            patient.Id,
            patient.Id,
            HealthPermissions.CreatePatient,
            AuditActionTypes.Create,
            true,
            accessDecision);

        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDetailsDto>> UpdatePatient(Guid id, UpdatePatientRequest request)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(id);
        var accessDecision = await _authorizationService.HasPermissionAsync(
            authorizationContext,
            HealthPermissions.EditPatientProfile);

        if (!accessDecision.IsAllowed)
        {
            await LogPatientProfileAuditAsync(
                requestContext,
                id,
                id,
                HealthPermissions.EditPatientProfile,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest(new { message = "First name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { message = "Last name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            return BadRequest(new { message = "Mobile number is required." });
        }

        var patient = await _dbContext.PatientProfiles
            .Include(x => x.ContactInfo)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (patient is null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var mobileNumber = request.MobileNumber.Trim();

        var duplicateMobile = await _dbContext.ContactInfos
            .AnyAsync(x =>
                x.MobileNumber == mobileNumber &&
                x.PatientProfileId != patient.Id);

        if (duplicateMobile)
        {
            return Conflict(new
            {
                message = "Another patient with this mobile number already exists."
            });
        }

        var now = DateTime.UtcNow;

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.BirthDate = request.BirthDate;
        patient.NationalCode = string.IsNullOrWhiteSpace(request.NationalCode) ? null : request.NationalCode.Trim();
        patient.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
        patient.BloodType = string.IsNullOrWhiteSpace(request.BloodType) ? null : request.BloodType.Trim();
        patient.MaritalStatus = string.IsNullOrWhiteSpace(request.MaritalStatus) ? null : request.MaritalStatus.Trim();
        patient.EducationLevel = string.IsNullOrWhiteSpace(request.EducationLevel) ? null : request.EducationLevel.Trim();
        patient.Occupation = string.IsNullOrWhiteSpace(request.Occupation) ? null : request.Occupation.Trim();
        patient.UpdatedAt = now;

        if (patient.ContactInfo is null)
        {
            patient.ContactInfo = new ContactInfo
            {
                Id = Guid.NewGuid(),
                PatientProfileId = patient.Id,
                CreatedAt = now
            };
        }

        patient.ContactInfo.MobileNumber = mobileNumber;
        patient.ContactInfo.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        patient.ContactInfo.EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName) ? null : request.EmergencyContactName.Trim();
        patient.ContactInfo.EmergencyContactPhone = string.IsNullOrWhiteSpace(request.EmergencyContactPhone) ? null : request.EmergencyContactPhone.Trim();
        patient.ContactInfo.HomeAddress = string.IsNullOrWhiteSpace(request.HomeAddress) ? null : request.HomeAddress.Trim();
        patient.ContactInfo.WorkAddress = string.IsNullOrWhiteSpace(request.WorkAddress) ? null : request.WorkAddress.Trim();
        patient.ContactInfo.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        var dto = new PatientDetailsDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            BirthDate = patient.BirthDate,
            NationalCode = patient.NationalCode,
            Gender = patient.Gender,
            BloodType = patient.BloodType,
            MaritalStatus = patient.MaritalStatus,
            EducationLevel = patient.EducationLevel,
            Occupation = patient.Occupation,
            MobileNumber = patient.ContactInfo.MobileNumber,
            Email = patient.ContactInfo.Email,
            EmergencyContactName = patient.ContactInfo.EmergencyContactName,
            EmergencyContactPhone = patient.ContactInfo.EmergencyContactPhone,
            HomeAddress = patient.ContactInfo.HomeAddress,
            WorkAddress = patient.ContactInfo.WorkAddress,
            IsActive = patient.IsActive,
            CreatedAt = patient.CreatedAt
        };

        await LogPatientProfileAuditAsync(
            requestContext,
            patient.Id,
            patient.Id,
            HealthPermissions.EditPatientProfile,
            AuditActionTypes.Update,
            true,
            accessDecision);

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivatePatient(Guid id)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(id);
        var accessDecision = await _authorizationService.HasPermissionAsync(
            authorizationContext,
            HealthPermissions.DeactivatePatient);

        if (!accessDecision.IsAllowed)
        {
            await LogPatientProfileAuditAsync(
                requestContext,
                id,
                id,
                HealthPermissions.DeactivatePatient,
                AuditActionTypes.AccessDenied,
                false,
                accessDecision);

            return AccessDenied();
        }

        var patient = await _dbContext.PatientProfiles
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (patient is null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await LogPatientProfileAuditAsync(
            requestContext,
            patient.Id,
            patient.Id,
            HealthPermissions.DeactivatePatient,
            AuditActionTypes.Update,
            true,
            accessDecision);

        return NoContent();
    }

    private async Task LogPatientSummaryAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid patientId,
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
            ResourceType = AuditResourceTypes.PatientSummary,
            Permission = HealthPermissions.ViewPatientSummary,
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

    private async Task LogPatientProfileAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid? patientId,
        Guid? resourceId,
        string permission,
        string actionType,
        bool succeeded,
        AccessDecision? accessDecision = null,
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
            ResourceType = AuditResourceTypes.PatientProfile,
            ResourceId = resourceId,
            Permission = permission,
            AccessScope = accessDecision?.MatchedScope,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : accessDecision?.DenialReason,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod,
            MetadataJson = metadataJson
        });
    }

    private static HealthCoreAuthorizationContext CreateDirectoryAuthorizationContext(
        HealthCoreRequestContext requestContext)
    {
        return new HealthCoreAuthorizationContext
        {
            UserId = requestContext.UserId,
            ServiceAccountId = requestContext.ServiceAccountId,
            PatientId = DirectoryAuthorizationPatientId,
            ProductCode = requestContext.ProductCode ?? string.Empty,
            ProductRole = requestContext.ProductRole ?? string.Empty
        };
    }

    private static string CreateDirectoryAuditMetadata(int page, int pageSize, string? search)
    {
        return JsonSerializer.Serialize(new
        {
            page,
            pageSize,
            hasSearch = !string.IsNullOrWhiteSpace(search)
        });
    }

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
    }
}

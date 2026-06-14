using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Patients;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
[Route("api/health-core/patients")]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PatientsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

        return Ok(patients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDetailsDto>> GetPatientById(Guid id)
    {
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

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<PatientDetailsDto>> CreatePatient(CreatePatientRequest request)
    {
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

        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, dto);
    }
        [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDetailsDto>> UpdatePatient(Guid id, UpdatePatientRequest request)
    {
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

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivatePatient(Guid id)
    {
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

        return NoContent();
    }
}
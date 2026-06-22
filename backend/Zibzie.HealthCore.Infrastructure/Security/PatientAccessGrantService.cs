using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.Security;

public class PatientAccessGrantService : IPatientAccessGrantService
{
    private const int MaxServiceAccountIdLength = 200;
    private const int MaxNoteLength = 1000;

    private readonly AppDbContext _dbContext;

    public PatientAccessGrantService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>>> ListForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (!patientExists)
        {
            return PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>>.Failure(
                PatientAccessGrantServiceError.NotFound,
                "Patient not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var grants = await _dbContext.PatientAccessGrants
            .AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderBy(x => x.RevokedAt.HasValue)
            .ThenByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.GrantedAt)
            .ToListAsync(cancellationToken);

        return PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>>.Success(
            grants.Select(x => ToDto(x, now)).ToArray());
    }

    public async Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> GetByIdAsync(
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        var grant = await _dbContext.PatientAccessGrants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == grantId, cancellationToken);

        if (grant is null)
        {
            return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.NotFound,
                "Patient access grant not found.");
        }

        return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Success(ToDto(grant, DateTimeOffset.UtcNow));
    }

    public async Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> CreateAsync(
        Guid patientId,
        CreatePatientAccessGrantRequest request,
        HealthCoreRequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var validationError = ValidateCreateRequest(request, now, out var normalized);

        if (validationError is not null)
        {
            return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.Validation,
                validationError);
        }

        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (!patientExists)
        {
            return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.NotFound,
                "Patient not found.");
        }

        var duplicateExists = await _dbContext.PatientAccessGrants
            .AnyAsync(x =>
                    x.PatientId == patientId &&
                    x.UserId == normalized.GranteeUserId &&
                    x.ServiceAccountId == normalized.ServiceAccountId &&
                    x.ProductCode == normalized.ProductCode &&
                    x.ProductRole == normalized.ProductRole &&
                    x.AccessScope == normalized.Scope &&
                    x.RevokedAt == null &&
                    (!x.ValidUntil.HasValue || x.ValidUntil.Value >= normalized.ValidFrom) &&
                    (!normalized.ValidUntil.HasValue || x.ValidFrom <= normalized.ValidUntil.Value),
                cancellationToken);

        if (duplicateExists)
        {
            return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.Conflict,
                "An overlapping active patient access grant already exists for this grantee.");
        }

        var grant = new PatientAccessGrant
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            UserId = normalized.GranteeUserId,
            ServiceAccountId = normalized.ServiceAccountId,
            ProductCode = normalized.ProductCode,
            ProductRole = normalized.ProductRole,
            AccessScope = normalized.Scope,
            AuthorizationReason = normalized.Reason,
            ValidFrom = normalized.ValidFrom,
            ValidUntil = normalized.ValidUntil,
            GrantedByUserId = requestContext.UserId,
            GrantedAt = now,
            GrantNote = normalized.Notes,
            CreatedAt = now
        };

        _dbContext.PatientAccessGrants.Add(grant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Success(ToDto(grant, now));
    }

    public async Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> RevokeAsync(
        Guid grantId,
        RevokePatientAccessGrantRequest request,
        HealthCoreRequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        var grant = await _dbContext.PatientAccessGrants
            .FirstOrDefaultAsync(x => x.Id == grantId, cancellationToken);

        if (grant is null)
        {
            return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.NotFound,
                "Patient access grant not found.");
        }

        if (grant.RevokedAt.HasValue)
        {
            return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.Conflict,
                "Patient access grant is already revoked.");
        }

        var now = DateTimeOffset.UtcNow;

        grant.Revoke(requestContext.UserId, NormalizeOptional(request.Reason), now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return PatientAccessGrantServiceResult<PatientAccessGrantDto>.Success(ToDto(grant, now));
    }

    private static string? ValidateCreateRequest(
        CreatePatientAccessGrantRequest request,
        DateTimeOffset now,
        out NormalizedCreateGrantRequest normalized)
    {
        var productCode = NormalizeRequired(request.ProductCode);
        var productRole = NormalizeRequired(request.ProductRole);
        var scope = NormalizeRequired(request.Scope);
        var reason = NormalizeRequired(request.Reason);
        var serviceAccountId = NormalizeOptional(request.ServiceAccountId);
        var notes = NormalizeOptional(request.Notes);
        var validFrom = (request.ValidFrom ?? now).ToUniversalTime();
        var validUntil = request.ValidUntil?.ToUniversalTime();

        normalized = new NormalizedCreateGrantRequest(
            request.GranteeUserId,
            serviceAccountId,
            productCode ?? string.Empty,
            productRole ?? string.Empty,
            scope ?? string.Empty,
            reason ?? string.Empty,
            validFrom,
            validUntil,
            notes);

        if (!request.GranteeUserId.HasValue && string.IsNullOrWhiteSpace(serviceAccountId))
        {
            return "GranteeUserId or ServiceAccountId is required.";
        }

        if (serviceAccountId?.Length > MaxServiceAccountIdLength)
        {
            return "ServiceAccountId is too long.";
        }

        if (notes?.Length > MaxNoteLength)
        {
            return "Notes is too long.";
        }

        if (productCode is null || !ProductCodes.All.Contains(productCode, StringComparer.Ordinal))
        {
            return "ProductCode is invalid.";
        }

        if (string.Equals(productCode, ProductCodes.InternalAdmin, StringComparison.Ordinal))
        {
            return "InternalAdmin access grants cannot be created through this workflow.";
        }

        if (productRole is null || !ProductRoles.All.Contains(productRole, StringComparer.Ordinal))
        {
            return "ProductRole is invalid.";
        }

        var roleProfile = ProductAccessProfiles.GetRoleProfile(productCode, productRole);

        if (roleProfile is null)
        {
            return "ProductRole is not valid for the selected ProductCode.";
        }

        if (scope is null || !AccessScopes.All.Contains(scope, StringComparer.Ordinal))
        {
            return "Scope is invalid.";
        }

        if (!string.Equals(scope, roleProfile.AccessScope, StringComparison.Ordinal))
        {
            return "Scope must match the selected product role profile.";
        }

        if (reason is null || !AuthorizationReasons.All.Contains(reason, StringComparer.Ordinal))
        {
            return "Reason is invalid.";
        }

        if (validUntil.HasValue && validUntil.Value <= validFrom)
        {
            return "ValidUntil must be after ValidFrom.";
        }

        if (validUntil.HasValue && validUntil.Value <= now)
        {
            return "ValidUntil must be in the future.";
        }

        return null;
    }

    private static PatientAccessGrantDto ToDto(PatientAccessGrant grant, DateTimeOffset now)
    {
        return new PatientAccessGrantDto
        {
            Id = grant.Id,
            PatientId = grant.PatientId,
            ProductCode = grant.ProductCode,
            ProductRole = grant.ProductRole,
            Scope = grant.AccessScope,
            Reason = grant.AuthorizationReason,
            GranteeUserId = grant.UserId,
            ServiceAccountId = grant.ServiceAccountId,
            IsActive = grant.IsActive(now),
            ValidFrom = grant.ValidFrom,
            ValidUntil = grant.ValidUntil,
            GrantedAt = grant.GrantedAt,
            GrantedByUserId = grant.GrantedByUserId,
            GrantedByServiceAccountId = null,
            RevokedAt = grant.RevokedAt,
            RevokedByUserId = grant.RevokedByUserId,
            RevokedByServiceAccountId = null,
            RevokeReason = grant.RevokeReason
        };
    }

    private static string? NormalizeRequired(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record NormalizedCreateGrantRequest(
        Guid? GranteeUserId,
        string? ServiceAccountId,
        string ProductCode,
        string ProductRole,
        string Scope,
        string Reason,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidUntil,
        string? Notes);
}

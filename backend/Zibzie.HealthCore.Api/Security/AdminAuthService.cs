using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Security;

public class AdminAuthService : IAdminAuthService
{
    private const string GenericLoginFailure = "Invalid username or password.";

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AdminUser> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditLogService _auditLogService;

    public AdminAuthService(
        AppDbContext dbContext,
        IPasswordHasher<AdminUser> passwordHasher,
        IJwtTokenService jwtTokenService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _auditLogService = auditLogService;
    }

    public async Task<AdminLoginResult> LoginAsync(
        string? username,
        string? password,
        HealthCoreRequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(username);

        if (normalizedUsername is null || string.IsNullOrWhiteSpace(password))
        {
            await AuditFailedLoginAsync(requestContext, "Missing admin credentials.", cancellationToken);
            return AdminLoginResult.Failed(GenericLoginFailure);
        }

        var adminUser = await _dbContext.AdminUsers
            .SingleOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);

        if (adminUser is null)
        {
            await AuditFailedLoginAsync(requestContext, "Invalid admin credentials.", cancellationToken);
            return AdminLoginResult.Failed(GenericLoginFailure);
        }

        if (!adminUser.IsActive)
        {
            await AuditFailedLoginAsync(requestContext, "Admin user is inactive.", cancellationToken);
            return AdminLoginResult.Failed(GenericLoginFailure);
        }

        if (!ProductRoles.InternalAdminRoles.Contains(adminUser.ProductRole))
        {
            await AuditFailedLoginAsync(requestContext, "Admin user role is not an internal admin role.", cancellationToken);
            return AdminLoginResult.Failed(GenericLoginFailure);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            adminUser,
            adminUser.PasswordHash,
            password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            await AuditFailedLoginAsync(requestContext, "Invalid admin credentials.", cancellationToken);
            return AdminLoginResult.Failed(GenericLoginFailure);
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, password);
        }

        adminUser.LastLoginAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.CreateAdminAccessToken(adminUser);

        await _auditLogService.LogAsync(new AuditLogRequest
        {
            UserId = adminUser.Id,
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = adminUser.ProductRole,
            ActionType = AuditActionTypes.Login,
            ResourceType = AuditResourceTypes.SecuritySettings,
            ResourceId = adminUser.Id,
            Succeeded = true,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod
        }, cancellationToken);

        return new AdminLoginResult
        {
            Succeeded = true,
            AdminUserId = adminUser.Id,
            Username = adminUser.Username,
            DisplayName = adminUser.DisplayName,
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = adminUser.ProductRole,
            Token = token
        };
    }

    public static string? NormalizeUsername(string? username)
    {
        return string.IsNullOrWhiteSpace(username)
            ? null
            : username.Trim().ToLowerInvariant();
    }

    private async Task AuditFailedLoginAsync(
        HealthCoreRequestContext requestContext,
        string failureReason,
        CancellationToken cancellationToken)
    {
        await _auditLogService.LogAsync(new AuditLogRequest
        {
            ProductCode = ProductCodes.InternalAdmin,
            ActionType = AuditActionTypes.Login,
            ResourceType = AuditResourceTypes.SecuritySettings,
            Succeeded = false,
            FailureReason = failureReason,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod
        }, cancellationToken);
    }
}

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Zibzie.HealthCore.Api.Security;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;
using Zibzie.HealthCore.Infrastructure.Security;

namespace Zibzie.HealthCore.Tests.Security;

public class AdminAuthServiceTests
{
    private const string SigningKey = "TEST_ONLY_ADMIN_AUTH_SIGNING_KEY_1234567890";

    [Fact]
    public async Task LoginAsync_WithValidPassword_ReturnsJwtWithInternalAdminClaimsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var admin = await SeedAdminAsync(dbContext, "Admin", "correct-password");
        var service = CreateService(dbContext);

        var result = await service.LoginAsync(
            "ADMIN",
            "correct-password",
            RequestContext(),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Token);
        Assert.Equal(admin.Id, result.AdminUserId);
        Assert.Equal(ProductCodes.InternalAdmin, result.ProductCode);
        Assert.Equal(ProductRoles.HealthCoreAdmin, result.ProductRole);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token!.AccessToken);
        Assert.Equal("TestIssuer", token.Issuer);
        Assert.Contains("TestAudience", token.Audiences);
        Assert.Equal(admin.Id.ToString(), ClaimValue(token, JwtRegisteredClaimNames.Sub));
        Assert.Equal(admin.Id.ToString(), ClaimValue(token, "user_id"));
        Assert.Equal(ProductCodes.InternalAdmin, ClaimValue(token, "product_code"));
        Assert.Equal(ProductRoles.HealthCoreAdmin, ClaimValue(token, "product_role"));
        Assert.False(string.IsNullOrWhiteSpace(ClaimValue(token, JwtRegisteredClaimNames.Jti)));
        Assert.False(string.IsNullOrWhiteSpace(ClaimValue(token, JwtRegisteredClaimNames.Iat)));

        var updatedAdmin = await dbContext.AdminUsers.SingleAsync(x => x.Id == admin.Id);
        Assert.NotNull(updatedAdmin.LastLoginAt);

        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditActionTypes.Login, auditEntry.ActionType);
        Assert.Equal(AuditResourceTypes.SecuritySettings, auditEntry.ResourceType);
        Assert.Equal(admin.Id, auditEntry.UserId);
        Assert.Equal(admin.Id, auditEntry.ResourceId);
        Assert.Equal(ProductCodes.InternalAdmin, auditEntry.ProductCode);
        Assert.Equal(ProductRoles.HealthCoreAdmin, auditEntry.ProductRole);
        Assert.True(auditEntry.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsGenericFailureAndAuditsFailure()
    {
        await using var dbContext = CreateDbContext();
        var admin = await SeedAdminAsync(dbContext, "admin", "correct-password");
        var service = CreateService(dbContext);

        var result = await service.LoginAsync(
            "admin",
            "wrong-password",
            RequestContext(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.FailureReason);
        Assert.Null(result.Token);

        var updatedAdmin = await dbContext.AdminUsers.SingleAsync(x => x.Id == admin.Id);
        Assert.Null(updatedAdmin.LastLoginAt);

        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditActionTypes.Login, auditEntry.ActionType);
        Assert.Equal(AuditResourceTypes.SecuritySettings, auditEntry.ResourceType);
        Assert.False(auditEntry.Succeeded);
        Assert.Equal("Invalid admin credentials.", auditEntry.FailureReason);
        Assert.Null(auditEntry.UserId);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveAdmin_ReturnsGenericFailureAndAuditsFailure()
    {
        await using var dbContext = CreateDbContext();
        await SeedAdminAsync(dbContext, "admin", "correct-password", isActive: false);
        var service = CreateService(dbContext);

        var result = await service.LoginAsync(
            "admin",
            "correct-password",
            RequestContext(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.FailureReason);
        Assert.Null(result.Token);

        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.False(auditEntry.Succeeded);
        Assert.Equal("Admin user is inactive.", auditEntry.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_AfterRepeatedFailures_ReturnsGenericFailureAndAuditsThrottle()
    {
        await using var dbContext = CreateDbContext();
        await SeedAdminAsync(dbContext, "admin", "correct-password");
        var service = CreateService(
            dbContext,
            new AdminLoginThrottleOptions
            {
                MaxFailedAttempts = 2,
                WindowMinutes = 15,
                LockoutMinutes = 5
            });

        await service.LoginAsync(
            "admin",
            "wrong-password",
            RequestContext(),
            CancellationToken.None);
        await service.LoginAsync(
            "admin",
            "wrong-password",
            RequestContext(),
            CancellationToken.None);

        var throttledResult = await service.LoginAsync(
            "admin",
            "correct-password",
            RequestContext(),
            CancellationToken.None);

        Assert.False(throttledResult.Succeeded);
        Assert.Equal("Invalid username or password.", throttledResult.FailureReason);
        Assert.Null(throttledResult.Token);

        var auditEntries = await dbContext.AuditLogEntries
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
        Assert.Equal(3, auditEntries.Count);
        Assert.Equal("Admin login temporarily throttled.", auditEntries.Last().FailureReason);
        Assert.False(auditEntries.Last().Succeeded);
    }

    [Fact]
    public async Task GeneratedAdminTokenClaims_MapIntoRequestContext()
    {
        await using var dbContext = CreateDbContext();
        var admin = await SeedAdminAsync(dbContext, "admin", "correct-password");
        var service = CreateService(dbContext);
        var result = await service.LoginAsync(
            "admin",
            "correct-password",
            RequestContext(),
            CancellationToken.None);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token!.AccessToken);
        var httpContext = new DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(token.Claims, "Bearer")),
            TraceIdentifier = "trace-1"
        };
        var provider = new HttpHealthCoreRequestContextProvider(
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(new HealthCoreAuthOptions
            {
                AllowHeaderFallback = false,
                AllowDefaultDevFallback = false
            }),
            new TestHostEnvironment());

        var requestContext = provider.GetCurrent();

        Assert.Equal(admin.Id, requestContext.UserId);
        Assert.Equal(ProductCodes.InternalAdmin, requestContext.ProductCode);
        Assert.Equal(ProductRoles.HealthCoreAdmin, requestContext.ProductRole);
        Assert.True(requestContext.IsAuthenticated);
        Assert.False(requestContext.IsFallbackContext);
    }

    private static AdminAuthService CreateService(
        AppDbContext dbContext,
        AdminLoginThrottleOptions? throttleOptions = null)
    {
        return new AdminAuthService(
            dbContext,
            new PasswordHasher<AdminUser>(),
            new JwtTokenService(Options.Create(new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningKey = SigningKey,
                AccessTokenMinutes = 60
            })),
            new AuditLogService(dbContext),
            new InMemoryAdminLoginThrottle(Options.Create(new AdminAuthOptions
            {
                LoginThrottle = throttleOptions ?? new AdminLoginThrottleOptions()
            })));
    }

    private static async Task<AdminUser> SeedAdminAsync(
        AppDbContext dbContext,
        string username,
        string password,
        bool isActive = true)
    {
        var passwordHasher = new PasswordHasher<AdminUser>();
        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = AdminAuthService.NormalizeUsername(username)!,
            DisplayName = "Test Admin",
            ProductRole = ProductRoles.HealthCoreAdmin,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        dbContext.AdminUsers.Add(admin);
        await dbContext.SaveChangesAsync();

        return admin;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static HealthCoreRequestContext RequestContext()
    {
        return new HealthCoreRequestContext
        {
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            CorrelationId = "correlation-1",
            RequestPath = "/api/health-core/auth/admin/login",
            HttpMethod = "POST"
        };
    }

    private static string? ClaimValue(JwtSecurityToken token, string type)
    {
        return token.Claims.SingleOrDefault(x => x.Type == type)?.Value;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Zibzie.HealthCore.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Security;

public static class AdminUserBootstrapper
{
    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AdminAuthOptions>>().Value;
        var bootstrap = options.BootstrapAdmin;

        if (!bootstrap.Enabled)
        {
            return;
        }

        var username = AdminAuthService.NormalizeUsername(bootstrap.Username);

        if (username is null || string.IsNullOrWhiteSpace(bootstrap.Password))
        {
            throw new InvalidOperationException("Bootstrap admin username and password are required when bootstrap is enabled.");
        }

        if (!ProductRoles.InternalAdminRoles.Contains(bootstrap.ProductRole))
        {
            throw new InvalidOperationException("Bootstrap admin product role must be a known InternalAdmin role.");
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await dbContext.AdminUsers.AnyAsync(x => x.Username == username);

        if (exists)
        {
            return;
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();
        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(bootstrap.DisplayName)
                ? null
                : bootstrap.DisplayName.Trim(),
            ProductRole = bootstrap.ProductRole,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, bootstrap.Password);

        dbContext.AdminUsers.Add(adminUser);
        await dbContext.SaveChangesAsync();
    }
}

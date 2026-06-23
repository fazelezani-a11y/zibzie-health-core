using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Zibzie.HealthCore.Api.Security;
using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Tests.Security;

public class HealthCoreSecurityStartupValidationTests
{
    [Fact]
    public void Validate_AllowsDevelopmentFallback()
    {
        HealthCoreSecurityStartupValidation.Validate(
            new TestHostEnvironment(Environments.Development),
            new HealthCoreAuthOptions
            {
                AllowHeaderFallback = true,
                AllowDefaultDevFallback = true
            },
            new JwtOptions(),
            new AdminAuthOptions());
    }

    [Fact]
    public void Validate_ThrowsInProductionWhenFallbackIsEnabled()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HealthCoreSecurityStartupValidation.Validate(
                new TestHostEnvironment(Environments.Production),
                new HealthCoreAuthOptions
                {
                    AllowHeaderFallback = true
                },
                CreateSafeJwtOptions(),
                new AdminAuthOptions()));
    }

    [Fact]
    public void Validate_ThrowsInProductionWhenBootstrapIsEnabled()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HealthCoreSecurityStartupValidation.Validate(
                new TestHostEnvironment(Environments.Production),
                new HealthCoreAuthOptions(),
                CreateSafeJwtOptions(),
                new AdminAuthOptions
                {
                    BootstrapAdmin = new BootstrapAdminOptions
                    {
                        Enabled = true,
                        ProductRole = ProductRoles.HealthCoreAdmin
                    }
                }));
    }

    [Fact]
    public void Validate_ThrowsInProductionWhenJwtConfigurationIsMissing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HealthCoreSecurityStartupValidation.Validate(
                new TestHostEnvironment(Environments.Production),
                new HealthCoreAuthOptions(),
                new JwtOptions(),
                new AdminAuthOptions()));
    }

    [Fact]
    public void Validate_ThrowsInProductionWhenSigningKeyIsTooShort()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HealthCoreSecurityStartupValidation.Validate(
                new TestHostEnvironment(Environments.Production),
                new HealthCoreAuthOptions(),
                new JwtOptions
                {
                    Key = "short-key"
                },
                new AdminAuthOptions()));
    }

    [Fact]
    public void Validate_ThrowsInProductionWhenJwtCoreValidationIsDisabled()
    {
        var jwtOptions = CreateSafeJwtOptions();
        jwtOptions.ValidateLifetime = false;

        Assert.Throws<InvalidOperationException>(() =>
            HealthCoreSecurityStartupValidation.Validate(
                new TestHostEnvironment(Environments.Production),
                new HealthCoreAuthOptions(),
                jwtOptions,
                new AdminAuthOptions()));
    }

    [Fact]
    public void Validate_ThrowsInProductionWhenSigningKeyValidationIsDisabled()
    {
        var jwtOptions = CreateSafeJwtOptions();
        jwtOptions.ValidateIssuerSigningKey = false;

        Assert.Throws<InvalidOperationException>(() =>
            HealthCoreSecurityStartupValidation.Validate(
                new TestHostEnvironment(Environments.Production),
                new HealthCoreAuthOptions(),
                jwtOptions,
                new AdminAuthOptions()));
    }

    [Fact]
    public void Validate_ThrowsInProductionWhenAuthorityHttpsMetadataIsDisabled()
    {
        var jwtOptions = CreateSafeJwtOptions();
        jwtOptions.Authority = "https://auth.example.test";
        jwtOptions.RequireHttpsMetadata = false;

        Assert.Throws<InvalidOperationException>(() =>
            HealthCoreSecurityStartupValidation.Validate(
                new TestHostEnvironment(Environments.Production),
                new HealthCoreAuthOptions(),
                jwtOptions,
                new AdminAuthOptions()));
    }

    [Fact]
    public void Validate_AllowsProductionWithSafeJwtConfiguration()
    {
        HealthCoreSecurityStartupValidation.Validate(
            new TestHostEnvironment(Environments.Production),
            new HealthCoreAuthOptions(),
            CreateSafeJwtOptions(),
            new AdminAuthOptions());
    }

    private static JwtOptions CreateSafeJwtOptions()
    {
        return new JwtOptions
        {
            Issuer = "Zibzie.HealthCore",
            Audience = "Zibzie.HealthCore",
            Key = "production-test-signing-key-with-32-bytes-minimum"
        };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "Zibzie.HealthCore.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

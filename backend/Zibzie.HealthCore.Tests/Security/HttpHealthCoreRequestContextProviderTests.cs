using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Zibzie.HealthCore.Api.Security;
using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Tests.Security;

public class HttpHealthCoreRequestContextProviderTests
{
    [Fact]
    public void GetCurrent_ReadsUserAndProductContextFromClaims()
    {
        var userId = Guid.NewGuid();
        var httpContext = CreateHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("product_code", ProductCodes.DigiCare),
            new Claim("product_role", ProductRoles.DigiCareClinician),
        }, "TestAuth"));
        var provider = CreateProvider(httpContext);

        var context = provider.GetCurrent();

        Assert.Equal(userId, context.UserId);
        Assert.Null(context.ServiceAccountId);
        Assert.Equal(ProductCodes.DigiCare, context.ProductCode);
        Assert.Equal(ProductRoles.DigiCareClinician, context.ProductRole);
        Assert.True(context.IsAuthenticated);
        Assert.False(context.IsFallbackContext);
    }

    [Fact]
    public void GetCurrent_ReadsCorrelationIdAndRequestMetadata()
    {
        var httpContext = CreateHttpContext();
        httpContext.TraceIdentifier = "trace-1";
        httpContext.Request.Headers["X-Correlation-ID"] = "correlation-1";
        httpContext.Request.Headers["User-Agent"] = "test-agent";
        httpContext.Request.Path = "/api/health-core/patients";
        httpContext.Request.Method = "GET";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        var provider = CreateProvider(httpContext);

        var context = provider.GetCurrent();

        Assert.Equal("correlation-1", context.CorrelationId);
        Assert.Equal("test-agent", context.UserAgent);
        Assert.Equal("/api/health-core/patients", context.RequestPath);
        Assert.Equal("GET", context.HttpMethod);
        Assert.Equal("127.0.0.1", context.IpAddress);
    }

    [Fact]
    public void GetCurrent_MarksHeaderContextAsFallback()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-HealthCore-Product"] = ProductCodes.HomeVisit;
        httpContext.Request.Headers["X-HealthCore-Product-Role"] = ProductRoles.HomeVisitDoctor;
        httpContext.Request.Headers["X-HealthCore-Service-Account"] = "homevisit-service";
        var provider = CreateProvider(httpContext);

        var context = provider.GetCurrent();

        Assert.Equal(ProductCodes.HomeVisit, context.ProductCode);
        Assert.Equal(ProductRoles.HomeVisitDoctor, context.ProductRole);
        Assert.Equal("homevisit-service", context.ServiceAccountId);
        Assert.False(context.IsAuthenticated);
        Assert.True(context.IsFallbackContext);
    }

    [Fact]
    public void GetCurrent_UsesDefaultFallbackWhenNoIdentityOrHeadersExist()
    {
        var provider = CreateProvider(CreateHttpContext());

        var context = provider.GetCurrent();

        Assert.Equal(ProductCodes.InternalAdmin, context.ProductCode);
        Assert.Equal(ProductRoles.HealthCoreAdmin, context.ProductRole);
        Assert.Equal("dev-admin", context.ServiceAccountId);
        Assert.False(context.IsAuthenticated);
        Assert.True(context.IsFallbackContext);
    }

    [Fact]
    public void CreateAuthorizationContext_MapsCurrentRequestContext()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var httpContext = CreateHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("product", ProductCodes.SecondOpinion),
            new Claim("role", ProductRoles.SecondOpinionLeadSpecialist),
        }, "TestAuth"));
        var provider = CreateProvider(httpContext);

        var authorizationContext = provider.CreateAuthorizationContext(patientId);

        Assert.Equal(userId, authorizationContext.UserId);
        Assert.Null(authorizationContext.ServiceAccountId);
        Assert.Equal(patientId, authorizationContext.PatientId);
        Assert.Equal(ProductCodes.SecondOpinion, authorizationContext.ProductCode);
        Assert.Equal(ProductRoles.SecondOpinionLeadSpecialist, authorizationContext.ProductRole);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            TraceIdentifier = "trace-default"
        };
    }

    private static HttpHealthCoreRequestContextProvider CreateProvider(HttpContext httpContext)
    {
        return new HttpHealthCoreRequestContextProvider(new HttpContextAccessor
        {
            HttpContext = httpContext
        });
    }
}

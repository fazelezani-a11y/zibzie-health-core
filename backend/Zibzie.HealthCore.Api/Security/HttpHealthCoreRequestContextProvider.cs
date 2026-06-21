using System.Security.Claims;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Zibzie.HealthCore.Application.Security;

namespace Zibzie.HealthCore.Api.Security;

public class HttpHealthCoreRequestContextProvider : IHealthCoreRequestContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HealthCoreAuthOptions _authOptions;
    private readonly IHostEnvironment _hostEnvironment;

    public HttpHealthCoreRequestContextProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<HealthCoreAuthOptions> authOptions,
        IHostEnvironment hostEnvironment)
    {
        _httpContextAccessor = httpContextAccessor;
        _authOptions = authOptions.Value;
        _hostEnvironment = hostEnvironment;
    }

    public HealthCoreRequestContext GetCurrent()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return IsDefaultDevFallbackAllowed()
                ? CreateDefaultFallbackContext()
                : new HealthCoreRequestContext();
        }

        var user = httpContext.User;
        var isAuthenticated = user.Identity?.IsAuthenticated == true;

        var userId = isAuthenticated ? TryReadUserId(user) : null;

        var productCodeFromClaims = isAuthenticated ? FirstClaimValue(user, "product_code", "product") : null;
        var productRoleFromClaims = isAuthenticated ? FirstClaimValue(user, "product_role", "role", ClaimTypes.Role) : null;
        var serviceAccountIdFromClaims = isAuthenticated ? FirstClaimValue(user, "service_account_id", "client_id") : null;

        var headerFallbackAllowed = IsHeaderFallbackAllowed();
        var productCodeFromHeaders = headerFallbackAllowed
            ? FirstHeaderValue(httpContext, "X-HealthCore-Product")
            : null;
        var productRoleFromHeaders = headerFallbackAllowed
            ? FirstHeaderValue(httpContext, "X-HealthCore-Product-Role")
            : null;
        var serviceAccountIdFromHeaders = headerFallbackAllowed
            ? FirstHeaderValue(httpContext, "X-HealthCore-Service-Account")
            : null;

        var usedHeaderFallback =
            (string.IsNullOrWhiteSpace(productCodeFromClaims) && !string.IsNullOrWhiteSpace(productCodeFromHeaders)) ||
            (string.IsNullOrWhiteSpace(productRoleFromClaims) && !string.IsNullOrWhiteSpace(productRoleFromHeaders)) ||
            (string.IsNullOrWhiteSpace(serviceAccountIdFromClaims) && !string.IsNullOrWhiteSpace(serviceAccountIdFromHeaders));

        var productCode = productCodeFromClaims ?? productCodeFromHeaders;
        var productRole = productRoleFromClaims ?? productRoleFromHeaders;
        var serviceAccountId = serviceAccountIdFromClaims ?? serviceAccountIdFromHeaders;

        var usedDefaultFallback = false;

        if (IsDefaultDevFallbackAllowed())
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                productCode = _authOptions.DefaultDevProductCode;
                usedDefaultFallback = true;
            }

            if (string.IsNullOrWhiteSpace(productRole))
            {
                productRole = _authOptions.DefaultDevProductRole;
                usedDefaultFallback = true;
            }

            if (!userId.HasValue && string.IsNullOrWhiteSpace(serviceAccountId))
            {
                serviceAccountId = _authOptions.DefaultDevServiceAccountId;
                usedDefaultFallback = true;
            }
        }

        return new HealthCoreRequestContext
        {
            UserId = userId,
            ServiceAccountId = TrimToNull(serviceAccountId),
            ProductCode = TrimToNull(productCode),
            ProductRole = TrimToNull(productRole),
            CorrelationId = FirstHeaderValue(httpContext, "X-Correlation-ID") ?? httpContext.TraceIdentifier,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = FirstHeaderValue(httpContext, "User-Agent"),
            RequestPath = httpContext.Request.Path.Value,
            HttpMethod = httpContext.Request.Method,
            IsAuthenticated = isAuthenticated,
            IsFallbackContext = usedHeaderFallback || usedDefaultFallback
        };
    }

    public HealthCoreAuthorizationContext CreateAuthorizationContext(Guid patientId)
    {
        var current = GetCurrent();

        return new HealthCoreAuthorizationContext
        {
            UserId = current.UserId,
            ServiceAccountId = current.ServiceAccountId,
            PatientId = patientId,
            ProductCode = current.ProductCode ?? string.Empty,
            ProductRole = current.ProductRole ?? string.Empty
        };
    }

    private HealthCoreRequestContext CreateDefaultFallbackContext()
    {
        return new HealthCoreRequestContext
        {
            ServiceAccountId = TrimToNull(_authOptions.DefaultDevServiceAccountId),
            ProductCode = TrimToNull(_authOptions.DefaultDevProductCode),
            ProductRole = TrimToNull(_authOptions.DefaultDevProductRole),
            IsAuthenticated = false,
            IsFallbackContext = true
        };
    }

    private bool IsHeaderFallbackAllowed()
    {
        return _authOptions.AllowHeaderFallback && !_hostEnvironment.IsProduction();
    }

    private bool IsDefaultDevFallbackAllowed()
    {
        return _authOptions.AllowDefaultDevFallback && !_hostEnvironment.IsProduction();
    }

    private static Guid? TryReadUserId(ClaimsPrincipal user)
    {
        var userIdValue = FirstClaimValue(user, ClaimTypes.NameIdentifier, "sub", "user_id");

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    private static string? FirstClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? FirstHeaderValue(HttpContext httpContext, string headerName)
    {
        return httpContext.Request.Headers.TryGetValue(headerName, out var values)
            ? TrimToNull(values.FirstOrDefault())
            : null;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

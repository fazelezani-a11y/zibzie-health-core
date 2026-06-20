using System.Security.Claims;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Security;

public class HttpHealthCoreRequestContextProvider : IHealthCoreRequestContextProvider
{
    private const string DefaultDevelopmentServiceAccountId = "dev-admin";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpHealthCoreRequestContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public HealthCoreRequestContext GetCurrent()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return CreateDefaultFallbackContext();
        }

        var user = httpContext.User;
        var isAuthenticated = user.Identity?.IsAuthenticated == true;

        var userId = TryReadUserId(user);

        var productCodeFromClaims = FirstClaimValue(user, "product_code", "product");
        var productRoleFromClaims = FirstClaimValue(user, "product_role", "role", ClaimTypes.Role);
        var serviceAccountIdFromClaims = FirstClaimValue(user, "service_account_id", "client_id");

        var productCodeFromHeaders = FirstHeaderValue(httpContext, "X-HealthCore-Product");
        var productRoleFromHeaders = FirstHeaderValue(httpContext, "X-HealthCore-Product-Role");
        var serviceAccountIdFromHeaders = FirstHeaderValue(httpContext, "X-HealthCore-Service-Account");

        var usedHeaderFallback =
            (string.IsNullOrWhiteSpace(productCodeFromClaims) && !string.IsNullOrWhiteSpace(productCodeFromHeaders)) ||
            (string.IsNullOrWhiteSpace(productRoleFromClaims) && !string.IsNullOrWhiteSpace(productRoleFromHeaders)) ||
            (string.IsNullOrWhiteSpace(serviceAccountIdFromClaims) && !string.IsNullOrWhiteSpace(serviceAccountIdFromHeaders));

        var productCode = productCodeFromClaims ?? productCodeFromHeaders;
        var productRole = productRoleFromClaims ?? productRoleFromHeaders;
        var serviceAccountId = serviceAccountIdFromClaims ?? serviceAccountIdFromHeaders;

        var usedDefaultFallback = false;

        if (string.IsNullOrWhiteSpace(productCode))
        {
            productCode = ProductCodes.InternalAdmin;
            usedDefaultFallback = true;
        }

        if (string.IsNullOrWhiteSpace(productRole))
        {
            productRole = ProductRoles.HealthCoreAdmin;
            usedDefaultFallback = true;
        }

        if (!userId.HasValue && string.IsNullOrWhiteSpace(serviceAccountId))
        {
            serviceAccountId = DefaultDevelopmentServiceAccountId;
            usedDefaultFallback = true;
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

    private static HealthCoreRequestContext CreateDefaultFallbackContext()
    {
        return new HealthCoreRequestContext
        {
            ServiceAccountId = DefaultDevelopmentServiceAccountId,
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = ProductRoles.HealthCoreAdmin,
            IsAuthenticated = false,
            IsFallbackContext = true
        };
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

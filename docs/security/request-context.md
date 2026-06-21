# Health Core Request Context

Phase 84B1 added a lightweight request-context foundation for authorization and audit integration.

The request context is now used by protected controller endpoints to build authorization decisions and audit log requests.

## Purpose

Protected endpoints need a consistent way to build `HealthCoreAuthorizationContext` and `AuditLogRequest` values from the current HTTP request.

The request context centralizes:

- `UserId`
- `ServiceAccountId`
- `ProductCode`
- `ProductRole`
- `CorrelationId`
- IP address
- user agent
- request path
- HTTP method
- whether the request is authenticated
- whether fallback context was used

## Current Auth Status

Phase 87C wires JWT bearer authentication into the API.

`Program.cs` registers JWT bearer authentication and calls `UseAuthentication()` / `UseAuthorization()`. This allows a valid bearer token to populate `HttpContext.User`.

Protected controllers still perform explicit authorization checks through `IHealthCoreAuthorizationService`; they do not rely on broad `[Authorize]` attributes or global authentication requirements.

If no valid bearer token is present, Development fallback may still provide local context when configured. If fallback is disabled, the request context remains unauthenticated/empty and protected endpoints are denied by the authorization service.

## Resolution Order

The HTTP provider resolves context in this order:

1. Authenticated user claims, when present:
   - user id: `ClaimTypes.NameIdentifier`, `sub`, `user_id`
   - product code: `product_code`, `product`
   - product role: `product_role`, `role`, `ClaimTypes.Role`
   - service account: `service_account_id`, `client_id`
2. Temporary fallback headers, only when `HealthCoreAuth:AllowHeaderFallback` is enabled and the environment is not Production:
   - `X-HealthCore-Product`
   - `X-HealthCore-Product-Role`
   - `X-HealthCore-Service-Account`
   - `X-Correlation-ID`
3. Temporary development fallback, only when `HealthCoreAuth:AllowDefaultDevFallback` is enabled and the environment is not Production:
   - `ProductCode = InternalAdmin`
   - `ProductRole = HealthCoreAdmin`
   - `ServiceAccountId = dev-admin`

`X-Correlation-ID` is used when present. Otherwise the ASP.NET Core trace identifier is used.

## Fallback Configuration

Phase 87B added `HealthCoreAuth` configuration.

Base/default configuration is production-safe:

- `AllowHeaderFallback = false`
- `AllowDefaultDevFallback = false`

Development configuration keeps local admin-panel behavior working:

- `AllowHeaderFallback = true`
- `AllowDefaultDevFallback = true`
- `DefaultDevProductCode = InternalAdmin`
- `DefaultDevProductRole = HealthCoreAdmin`
- `DefaultDevServiceAccountId = dev-admin`

The provider also has a hard safety guard: fallback is ignored in Production even if configuration accidentally enables it.

When no authenticated claims are available and fallback is disabled, the provider returns an unauthenticated context without product code, product role, user id, or service account id. Protected endpoints should then be denied by `IHealthCoreAuthorizationService`.

## Fallback Warning

Header fallback and default development fallback are not production-safe authorization mechanisms.

They exist only so the current unauthenticated admin panel and local development flows can keep working before production authentication is implemented.

Any request context that uses headers or default values is marked with:

`IsFallbackContext = true`

Before production deployment, fallback contexts must remain disabled outside explicitly approved development/test environments.

## Production Direction

Production should use a real identity provider or service-to-service authentication. Product context and role information should come from signed claims or trusted server-side mapping, not arbitrary client-supplied headers.

Future product frontends should authenticate users through their product identity flow, then pass or exchange trusted identity/product context with Health Core.

See [Production auth and JWT strategy](production-auth-jwt-strategy.md) for the proposed production claim contract, product context model, environment fallback policy, and phased migration path.

See [Admin login and frontend JWT integration strategy](admin-login-frontend-integration-strategy.md) for the proposed admin login flow and frontend token handling plan.

## Future Consumers

This context is used or intended to be used in:

- `IHealthCoreAuthorizationService`
- `IAuditLogService`
- explicit controller checks for protected endpoint groups
- access denied audit logging
- successful read/write audit logging

## Not Implemented Yet for Production Auth

- No production identity provider integration.
- No frontend login/token integration.
- No patient access grant creation UI/API.
- No authentication failure audit before controller execution.

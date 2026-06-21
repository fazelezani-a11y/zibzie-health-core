# Health Core Request Context

Phase 84B1 added a lightweight request-context foundation for authorization and audit integration.

The request context is now used by protected controller endpoints to build authorization decisions and audit log requests.

## Purpose

Future protected endpoints need a consistent way to build `HealthCoreAuthorizationContext` and `AuditLogRequest` values from the current HTTP request.

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

The current API does not have real authentication or ASP.NET Core authorization middleware.

There is a `Jwt` configuration section in API settings, but `Program.cs` does not currently register JWT bearer authentication or call authentication middleware.

Protected controllers perform explicit authorization checks through `IHealthCoreAuthorizationService`; they do not rely on `[Authorize]` attributes.

## Resolution Order

The HTTP provider resolves context in this order:

1. Authenticated user claims, when present:
   - user id: `ClaimTypes.NameIdentifier`, `sub`, `user_id`
   - product code: `product_code`, `product`
   - product role: `product_role`, `role`, `ClaimTypes.Role`
   - service account: `service_account_id`, `client_id`
2. Temporary fallback headers:
   - `X-HealthCore-Product`
   - `X-HealthCore-Product-Role`
   - `X-HealthCore-Service-Account`
   - `X-Correlation-ID`
3. Temporary development fallback:
   - `ProductCode = InternalAdmin`
   - `ProductRole = HealthCoreAdmin`
   - `ServiceAccountId = dev-admin`

`X-Correlation-ID` is used when present. Otherwise the ASP.NET Core trace identifier is used.

## Fallback Warning

Header fallback and default development fallback are not production-safe authorization mechanisms.

They exist only so the current unauthenticated admin panel and local development flows can keep working while the security foundation is being built.

Any request context that uses headers or default values is marked with:

`IsFallbackContext = true`

Before endpoint enforcement is enabled in production, fallback contexts should either be rejected for protected endpoints or limited to explicitly approved development/test environments.

## Production Direction

Production should use a real identity provider or service-to-service authentication. Product context and role information should come from signed claims or trusted server-side mapping, not arbitrary client-supplied headers.

Future product frontends should authenticate users through their product identity flow, then pass or exchange trusted identity/product context with Health Core.

See [Production auth and JWT strategy](production-auth-jwt-strategy.md) for the proposed production claim contract, product context model, environment fallback policy, and phased migration path.

## Future Consumers

This context is used or intended to be used in:

- `IHealthCoreAuthorizationService`
- `IAuditLogService`
- explicit controller checks for protected endpoint groups
- access denied audit logging
- successful read/write audit logging

## Not Implemented Yet for Production Auth

- No JWT bearer authentication middleware.
- No production identity provider integration.
- No environment-based fallback disablement.
- No frontend login/token integration.
- No patient access grant creation UI/API.
- No authentication failure audit before controller execution.

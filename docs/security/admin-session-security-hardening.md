# Admin Session Security Hardening

Phase 87E4 hardens the internal admin auth/session transition without changing Health Core endpoint permissions or the patient/medical data model.

## Scope

This phase covers:

- admin JWT/session lifetime expectations
- Production startup safety checks
- httpOnly cookie behavior
- logout and session clearing
- login failure handling
- brute-force/rate-limit decisions
- CSRF considerations for cookie-backed mutations
- legacy localStorage containment
- production readiness checklist

It does not implement service token issuing, central SSO, refresh tokens, endpoint permission changes, or Development fallback removal.

## Current Admin Auth Flow

1. `/login` posts credentials to the Next route handler `POST /api/admin-auth/login`.
2. The route handler calls backend `POST /api/health-core/auth/admin/login`.
3. The backend validates `AdminUser` credentials with `PasswordHasher<AdminUser>`.
4. The backend issues an internal admin JWT with `product_code = InternalAdmin` and `product_role`.
5. The Next route handler stores the token in the `zibzie_admin_access_token` httpOnly cookie.
6. Server components and browser-side Health Core calls use the cookie-backed server/proxy path.
7. Backend protected endpoints still make decisions through `IHealthCoreAuthorizationService`.

## Token Lifetime and Configuration

The current access-token lifetime is controlled by:

`Jwt:AccessTokenMinutes`

Current default:

- `60` minutes

This is intentionally short enough for early admin use while avoiding refresh-token complexity in this phase. Future hardening may reduce this further or introduce refresh/session renewal after logout, lockout, and monitoring behavior are more complete.

Production safety now includes startup validation:

- fallback must be disabled in Production
- bootstrap admin must be disabled in Production
- JWT authority or signing key must be configured in Production
- configured symmetric signing key must be at least 32 bytes
- JWT issuer, audience, and lifetime validation must be enabled in Production
- signing-key validation must be enabled when a symmetric key is configured
- authority metadata must require HTTPS in Production

Base configuration does not contain a production secret. Production JWT signing keys or authority settings must come from environment variables, a secret store, or deployment configuration.

## Cookie Security

The admin session cookie is:

`zibzie_admin_access_token`

Current cookie options:

- `httpOnly = true`
- `sameSite = lax`
- `secure = true` outside Development
- `path = /`
- `expires` aligned with backend token expiry when available

The backend JWT is not returned to browser JavaScript by the current Next login route. Token values must not be logged.

Session and Health Core proxy route responses are marked with:

`Cache-Control: no-store`

This applies to:

- `/api/admin-auth/login`
- `/api/admin-auth/me`
- `/api/admin-auth/logout`
- `/api/health-core/[...path]`

Phase 92 also adds `Pragma: no-cache`, `Expires: 0`, `X-Content-Type-Options`, `Referrer-Policy`, and `X-Frame-Options` to the Next admin/session/proxy responses. The Next app sets the basic browser security headers for pages through `next.config.ts`. The backend adds basic security headers and `no-store` for `/api/health-core` responses.

## Logout and Session Clearing

`POST /api/admin-auth/logout` clears the httpOnly cookie with matching cookie options and returns a simple success response.

When `/api/admin-auth/me` or `/api/health-core/[...path]` receives `401` from the backend, the Next layer clears the session cookie.

The frontend API helpers also clear the legacy localStorage token cleanup state on `401` or logout.

Known limitation:

- JWT access tokens are stateless and remain valid until expiry unless a future revocation/session store is implemented.

Future hardening should consider:

- token revocation or session versioning for admin users
- refresh-token strategy only after lockout/rate-limit policy is defined
- visible logout UI
- login/session monitoring

## Login Failure Handling

Current behavior:

- invalid login responses are generic
- username existence is not revealed
- inactive admin users receive the same generic response
- successful and failed login attempts are audited
- passwords are never logged
- repeated failed login attempts are throttled in memory

Phase 92 adds a minimal in-memory admin login throttle:

- default failed attempts: `5`
- default window: `15` minutes
- default lockout: `5` minutes
- throttled attempts return the same generic login failure
- throttled attempts are audited as failed login attempts

This is not a final production lockout system because it is process-local and does not coordinate across multiple backend instances.

Recommended future controls:

- persistent/shared IP/user-based login throttling
- persistent/shared temporary lockout after repeated failures
- alerting for repeated failures
- optional MFA for `SuperAdmin`
- audit of lockout and unlock events

## CSRF Considerations

The frontend now sends browser mutations through a cookie-backed proxy. This improves token secrecy but introduces CSRF considerations.

Current mitigations:

- `SameSite=Lax` cookie policy
- Phase 92 same-origin mutation guard for `POST /api/admin-auth/login`
- Phase 92 same-origin mutation guard for `POST /api/admin-auth/logout`
- Phase 92 same-origin mutation guard for `POST`, `PUT`, `PATCH`, and `DELETE` through `/api/health-core/[...path]`
- no cross-origin credentialed frontend flow is introduced
- backend CORS still does not need to trust browser identity headers for the Next proxy path

Required before public production:

- consider a formal CSRF token strategy for state-changing proxy requests
- ensure deployment does not allow cross-site credentialed requests unexpectedly
- keep `Secure` cookies outside Development
- confirm CORS policy matches the final deployment model
- avoid logging request cookies or bearer tokens

The current origin/fetch-site guard is a practical minimum. It is not a replacement for a deliberate CSRF policy if the admin panel becomes publicly exposed or spans multiple trusted domains.

## Legacy localStorage Status

The legacy helper remains at:

`frontend/src/lib/auth/admin-auth.ts`

Current status:

- no longer the primary auth path
- not used to attach bearer tokens for ordinary Health Core API calls
- kept only to clear older stored tokens on `401`, logout, or login transition

Do not rely on localStorage for production admin auth. Remove the helper after the fallback-off/session path is stable and no transitional code imports it.

## Production Safety Checklist

Before production:

- `HealthCoreAuth:AllowHeaderFallback = false`
- `HealthCoreAuth:AllowDefaultDevFallback = false`
- `AdminAuth:BootstrapAdmin:Enabled = false`
- real JWT authority or signing key configured outside repository
- Development placeholder signing key not used
- HTTPS enabled end to end
- admin cookie is `httpOnly`, `Secure`, and `SameSite`
- token lifetime reviewed and intentionally short
- successful and failed login audit events enabled
- fallback-off JWT smoke passes
- frontend login/session smoke passes
- no ordinary admin workflow depends on localStorage auth
- logout UI exists and clears session
- process-local login throttle reviewed and a distributed production throttle implemented or explicitly accepted before launch
- same-origin mutation guard reviewed and a formal CSRF token strategy implemented or explicitly accepted before launch
- audit log access remains admin-only if exposed later

See [Final production readiness checklist](final-production-readiness-checklist.md) for the consolidated pre-deployment checklist across backend, frontend session/proxy, service auth, grants, and audit logging.

See [Production security hardening](production-security-hardening.md) for the Phase 92 implementation details.

## Remaining Work

- visible frontend logout control
- production rate limiting / lockout
- CSRF token implementation
- session revocation or admin session versioning
- refresh-token/session renewal decision
- central SSO decision
- service-to-service token issuing and service account lifecycle
- monitoring and alerting for auth failures

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

Full rate limiting and account lockout are not implemented in this phase.

Decision:

- Do not add ad hoc in-memory rate limiting yet.
- Add proper rate limiting/lockout before production using a deliberate policy that works in multi-instance deployments.

Recommended future controls:

- IP/user-based login throttling
- temporary lockout after repeated failures
- alerting for repeated failures
- optional MFA for `SuperAdmin`
- audit of lockout and unlock events

## CSRF Considerations

The frontend now sends browser mutations through a cookie-backed proxy. This improves token secrecy but introduces CSRF considerations.

Current mitigations:

- `SameSite=Lax` cookie policy
- no cross-origin credentialed frontend flow is introduced
- backend CORS still does not need to trust browser identity headers for the Next proxy path

Required before public production:

- define CSRF token strategy for state-changing proxy requests
- ensure deployment does not allow cross-site credentialed requests unexpectedly
- keep `Secure` cookies outside Development
- confirm CORS policy matches the final deployment model
- avoid logging request cookies or bearer tokens

Full CSRF protection is intentionally documented, not implemented, in this phase to avoid breaking existing forms.

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
- rate limiting/lockout plan implemented or explicitly accepted before launch
- CSRF strategy implemented for cookie-backed mutations
- audit log access remains admin-only if exposed later

See [Final production readiness checklist](final-production-readiness-checklist.md) for the consolidated pre-deployment checklist across backend, frontend session/proxy, service auth, grants, and audit logging.

## Remaining Work

- visible frontend logout control
- production rate limiting / lockout
- CSRF token implementation
- session revocation or admin session versioning
- refresh-token/session renewal decision
- central SSO decision
- service-to-service token issuing and service account lifecycle
- monitoring and alerting for auth failures

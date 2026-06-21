# Admin Login and Frontend JWT Integration Strategy

Phase 87D defines the admin login and frontend token integration strategy for Health Core. It does not implement login, add authentication endpoints, change frontend behavior, or change backend API behavior.

## 1. Executive Summary

Health Core now has JWT bearer authentication wired in the backend and endpoint-level authorization through `IHealthCoreAuthorizationService`. The missing piece is a real admin login flow and frontend token handling.

The current admin panel can continue to work locally through the configured Development fallback, but that path is not production-safe. The next implementation should introduce an admin authentication flow that issues or receives JWTs containing `InternalAdmin` product context and a Health Core admin role, then teach the frontend API client to send those credentials.

Recommended direction:

- Use a small staged admin-auth implementation for the near-term internal panel.
- Keep JWT compatibility with the production claim contract from Phase 87A/87C.
- Prefer httpOnly secure cookie storage if deployment topology allows it.
- Keep bearer-token-in-JavaScript storage only as a temporary development compromise if needed.
- Defer broad SSO and product-wide identity federation until the Health Core admin panel is stable.

## 2. Current State

Backend state:

- JWT bearer authentication is wired in `Program.cs`.
- `UseAuthentication()` and `UseAuthorization()` run before controllers.
- Controllers do not broadly use `[Authorize]`.
- Protected endpoints still call `IHealthCoreAuthorizationService` explicitly.
- `HttpHealthCoreRequestContextProvider` prefers authenticated claims when a valid bearer token is present.
- Development header/default fallback remains config-gated through `HealthCoreAuth`.
- Production ignores fallback even if configuration accidentally enables it.
- There is no `AuthController`, login endpoint, refresh endpoint, logout endpoint, or admin-user endpoint.

Backend config state:

- `Jwt` config exists with issuer, audience, and a development signing key placeholder.
- `HealthCoreAuth` base/default config disables fallback.
- `HealthCoreAuth` Development config enables fallback.

Frontend state:

- The frontend is a Next app under `frontend/src/app`.
- There is no login page or auth route.
- There is no auth context/provider.
- There is no token storage logic.
- There is no `Authorization: Bearer` attachment.
- `frontend/src/lib/api/client.ts` is the central `fetch` wrapper.
- Patient list/detail server components call API helpers directly.
- Create/edit modules are client components and also use the same API helper layer.
- There are no current frontend auth libraries beyond Next/React.

Implication:

The least disruptive frontend integration point is `frontend/src/lib/api/client.ts`, but server-rendered patient pages need a token strategy that also works on the server. That makes secure httpOnly cookies preferable when feasible.

## 3. Admin User Types and Roles

Initial admin-facing roles should map to existing Health Core product roles:

- `SuperAdmin`: full internal administrative access.
- `HealthCoreAdmin`: standard internal admin panel access.
- `ReadOnlyAuditor`: audit/compliance read-only role.
- `SupportOperator`: limited support role where product profiles allow it.

Recommended admin user categories:

- Internal platform admin.
- Health Core operator.
- Compliance/audit reviewer.
- Support operator.
- Future product admin mapped through product context, not automatically InternalAdmin.

Admin roles must map to `ProductCode = InternalAdmin` and one of the approved `ProductRoles`.

## 4. Recommended Login Model

Health Core has three plausible login paths.

### Option 1: Simple Internal Admin Username/Password Login

Advantages:

- Fastest to implement.
- Works without external vendors.
- Good for early internal admin panel access.
- Easy to test locally.

Costs and risks:

- Requires password hashing, reset policy, lockout, rate limiting, and secure admin provisioning.
- Health Core becomes responsible for credential security.
- Weak password policies become a compliance risk.

Best fit:

- Short-term internal bootstrap only.
- Small trusted admin group.
- Must be designed so it can later migrate to central auth/SSO.

### Option 2: OTP / Phone-Based Admin Login

Advantages:

- Operationally familiar for Iranian service workflows.
- Avoids long-lived passwords.
- Good fit if zibzie already has a reliable SMS/OTP platform.

Costs and risks:

- Requires SMS provider integration.
- Needs OTP rate limits, replay prevention, expiry, abuse monitoring, and support flows.
- Phone ownership and admin provisioning still need governance.

Best fit:

- Near-term production-ish flow if a zibzie OTP service already exists.
- Admin identities are phone-number based.

### Option 3: External Identity Provider / SSO

Advantages:

- Best long-term production and compliance posture.
- Centralizes MFA, password policy, lifecycle, revocation, and device/session controls.
- Reduces custom auth responsibility inside Health Core.

Costs and risks:

- More setup.
- Requires identity provider selection and operations.
- May slow the current Health Core admin rollout.

Best fit:

- Long-term production target.
- Larger operations/compliance environment.

Recommended staged approach:

1. For the next coding phase, implement one controlled internal admin auth path only.
2. If zibzie already has a production OTP/auth service, prefer OTP-backed admin login.
3. If no such service exists, use a tightly scoped username/password bootstrap with strong hashing, rate limiting, and short-lived JWTs.
4. Keep the JWT claim contract compatible with future central SSO.
5. Do not overbuild SSO inside Health Core now.

## 5. JWT Issuing Strategy

Near-term recommendation:

- Health Core may issue internal admin JWTs for its own admin panel as an interim solution.
- Issued tokens should be valid only for `aud = Zibzie.HealthCore`.
- Claims should use `ProductCode = InternalAdmin`.
- Tokens should be short-lived.
- Refresh/session handling should be minimal at first and hardened in a later phase.

Long-term recommendation:

- A central zibzie auth service or identity provider should issue user tokens for all products.
- Health Core should validate trusted tokens rather than own all user authentication.
- Service-to-service tokens should be separate from human admin tokens.
- Product services should receive service-account tokens, not reusable human admin tokens.

Tradeoffs:

- Health Core-issued admin JWTs are faster but increase responsibility for credential lifecycle and auth security.
- Central auth is safer long-term but depends on platform readiness.
- A clean claim contract now reduces migration cost later.

## 6. Frontend Token Storage and Request Strategy

Preferred production model:

- Store the admin session/token in a secure, httpOnly, SameSite cookie.
- Use HTTPS-only cookies in deployed environments.
- Let server-rendered Next pages authenticate API requests without exposing the token to browser JavaScript.
- Keep access tokens short-lived.
- Use refresh/session cookies only if a refresh flow is implemented and protected.

Interim development model, only if necessary:

- Store a bearer token in memory.
- If persistence is required temporarily, localStorage/sessionStorage can be used only with clear risk documentation.
- Avoid localStorage for production because XSS can expose the token.

Frontend request strategy:

- Centralize credential attachment in `frontend/src/lib/api/client.ts`.
- For bearer tokens, add `Authorization: Bearer <token>` there.
- For cookie-based auth through the same site or proxy, use `credentials: "include"` where required.
- Keep API helpers unaware of auth details where possible.
- Add uniform handling for `401` and `403`.

Recommended `401` / `403` behavior:

- `401`: session missing/expired; redirect to login or show a login-required state.
- `403`: authenticated but not authorized; show an access-denied state and do not retry endlessly.
- Keep messages safe and avoid leaking patient existence.

Logout behavior:

- Clear client auth state.
- In cookie model, call backend logout/session endpoint to clear httpOnly cookie.
- In bearer model, remove token from memory/storage.
- Later hardening should support token/session revocation.

## 7. Token Claims Contract for Admin Users

Required admin JWT claims:

| Claim | Example | Purpose |
| --- | --- | --- |
| `sub` or `user_id` | admin user GUID | maps to `HealthCoreRequestContext.UserId` |
| `product_code` | `InternalAdmin` | maps to `HealthCoreRequestContext.ProductCode` |
| `product_role` | `HealthCoreAdmin` or `SuperAdmin` | maps to `HealthCoreRequestContext.ProductRole` |
| `jti` | token id | replay/audit investigation |
| `iat` | issued-at timestamp | token validation/audit context |
| `exp` | expiry timestamp | token lifetime enforcement |
| `iss` | configured issuer | token validation |
| `aud` | `Zibzie.HealthCore` | token validation |

Optional admin JWT claims:

- `name`
- `mobile`
- `email`
- `session_id`
- `auth_level`
- `amr`

`service_account_id` should not be present for ordinary human admin login. It should be used only for service-originated calls. If a product backend performs a user-initiated action, a future token/context model may include both a human user id and a service account id.

## 8. Backend Request Context Mapping

The existing request context provider already supports the planned admin claims:

- `sub`, `user_id`, or `ClaimTypes.NameIdentifier` -> `UserId`
- `product_code` or `product` -> `ProductCode`
- `product_role`, `role`, or `ClaimTypes.Role` -> `ProductRole`
- `service_account_id` or `client_id` -> `ServiceAccountId`
- `X-Correlation-ID` -> `CorrelationId`

Access decisions still flow through:

1. JWT bearer middleware validates token and populates `HttpContext.User`.
2. `HttpHealthCoreRequestContextProvider` reads claims into `HealthCoreRequestContext`.
3. Controllers build `HealthCoreAuthorizationContext`.
4. `IHealthCoreAuthorizationService` checks permissions, product access profile, grants, scopes, and sensitivity.
5. Controllers write AuditLog for success/denial.

## 9. Development Fallback Transition Plan

Current Development:

- Keep `HealthCoreAuth:AllowHeaderFallback = true`.
- Keep `HealthCoreAuth:AllowDefaultDevFallback = true`.
- Existing local admin panel remains usable.

After admin login backend exists:

- Frontend login receives or establishes a JWT-backed session.
- API client sends authenticated requests.
- Fallback remains available only for local troubleshooting.

After frontend token integration exists:

- Add a local test mode with fallback disabled.
- Run security smoke tests with JWT context.
- Keep old fallback smoke as a local-only diagnostic.

Staging:

- Fallback off by default.
- JWT required.
- Admin users must authenticate through the selected admin login path.

Production:

- Fallback off.
- JWT required.
- No arbitrary identity/product headers accepted from clients.

## 10. Security Risks and Mitigations

Token theft:

- Prefer httpOnly secure cookies.
- Avoid localStorage in production.
- Keep access tokens short-lived.

Long-lived tokens:

- Use short expiry.
- Add refresh/session hardening later.
- Audit suspicious use.

Missing logout/revocation:

- Add backend logout/session invalidation if cookie/session model is used.
- Add revocation/blacklist or short-lived-token strategy for high-risk admin roles.

Weak admin password policy:

- Use strong password hashing.
- Add rate limiting and lockout.
- Require secure admin provisioning.
- Prefer SSO/OTP when available.

OTP abuse:

- Rate limit OTP requests and verification attempts.
- Expire OTP quickly.
- Bind OTP to admin identity and session attempt.
- Audit failed attempts.

Role escalation through claims:

- Only trusted issuer should create `product_role` claims.
- Backend should not accept role headers in production.
- Audit admin role usage.

LocalStorage exposure:

- Avoid localStorage for production.
- If used temporarily, document XSS/token theft risk and keep token lifetime short.

Development fallback misuse:

- Keep fallback disabled in base/default config.
- Keep Production hard guard.
- Add deployment checks before staging/production.

Audit:

- Login success/failure should be audited.
- Admin read/write actions are already audited at protected endpoints.
- Future auth endpoints should not write patient Timeline events for security activity.

## 11. Implementation Phases

Recommended next phases:

### 87E1: Admin Auth Backend Foundation - implemented

- Added an internal admin username/password auth backend.
- Added `AdminUser`, password hashing, JWT issuing, and login audit events.
- Added `POST /api/health-core/auth/admin/login`.
- Added `GET /api/health-core/auth/admin/me`.
- Kept existing health-record endpoint permissions unchanged.
- See [Admin auth backend foundation](admin-auth-backend-foundation.md).

### 87E2: Frontend Admin Token Integration

- Add login page.
- Add auth state/session handling.
- Update `frontend/src/lib/api/client.ts` to attach credentials.
- Add logout.
- Add `401` and `403` handling.
- Preserve existing patient-record UI behavior for authenticated admins.

### 87E3: Disable Fallback Outside Development/Test

- Verify staging config has fallback disabled.
- Add JWT-backed security smoke path.
- Keep header fallback smoke only as local Development tooling.

### 87E4: Admin Session and Security Hardening

- Tune token lifetime.
- Add refresh/session strategy if needed.
- Add lockout/rate limiting.
- Add auth failure audit/monitoring.
- Add admin credential lifecycle and reset procedures.

### 87F: Service-to-Service Auth

- Add service account credential model.
- Support product backend tokens.
- Preserve user/service identity in AuditLog when both are available.

## 12. Open Questions

- Does zibzie already have an OTP/auth service Health Core can reuse?
- Should the first admin login be username/password or OTP?
- Where should admin identities live for the interim phase?
- Should Health Core issue admin JWTs directly or delegate immediately to a central auth service?
- Will the frontend and backend share a first-party domain suitable for secure httpOnly cookies?
- Should server components call Health Core directly with cookies, or should a Next route/proxy layer mediate API calls?
- What token lifetime is acceptable for internal care-team/admin workflows?
- What MFA requirement applies to `SuperAdmin`?
- What audit events are required for login success, login failure, logout, lockout, and token refresh?
- How will admin users be provisioned, deactivated, and role-changed?

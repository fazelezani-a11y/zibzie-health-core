# Admin Auth Backend Foundation

Phase 87E1 adds the first controlled backend path for internal Health Core admin authentication. It is intentionally narrow: it supports internal admin username/password login and JWT issuance only. It does not add frontend login, refresh tokens, password reset, patient-facing accounts, service-to-service credentials, or changes to existing health-record endpoint authorization decisions.

## Scope

Implemented in this phase:

- `AdminUser` domain entity and `AdminUsers` table.
- ASP.NET Core `PasswordHasher<AdminUser>` for password hashing.
- Internal admin login service.
- JWT access-token issuing service.
- `POST /api/health-core/auth/admin/login`.
- `GET /api/health-core/auth/admin/me`.
- Optional non-Production bootstrap seed controlled by configuration.
- Audit logging for successful and failed login attempts.

Not implemented in this phase:

- frontend login/token storage
- refresh tokens
- password reset
- lockout/rate limiting
- MFA
- central SSO
- service account credential lifecycle
- removal of Development fallback

## AdminUser Model

`AdminUser` stores only internal admin identities:

- `Id`
- `Username`
- `DisplayName`
- `PasswordHash`
- `ProductRole`
- `IsActive`
- `CreatedAt`
- `LastLoginAt`

`ProductCode` is implicitly `InternalAdmin` for this auth path. `ProductRole` must be one of the known internal roles from `ProductRoles.InternalAdminRoles`, such as `HealthCoreAdmin` or `SuperAdmin`.

Usernames are normalized to lowercase by the login and bootstrap services. Passwords are never stored in plaintext.

## Password Storage

Passwords are hashed with ASP.NET Core Identity's `PasswordHasher<AdminUser>`.

The implementation does not define custom cryptography. If the password hasher reports `SuccessRehashNeeded`, the stored hash is refreshed during successful login.

## JWT Issuing

Admin login issues a short-lived bearer token. The default access-token lifetime is:

- `Jwt:AccessTokenMinutes = 60`

The token includes:

- `sub`
- `user_id`
- `product_code = InternalAdmin`
- `product_role`
- `jti`
- `iat`
- `exp`
- `iss`
- `aud`
- optional `name`

These claims are compatible with `HttpHealthCoreRequestContextProvider`, which maps the authenticated principal into `HealthCoreRequestContext`.

The base appsettings file does not contain a production signing secret. Development keeps a local placeholder key. Production signing keys must come from environment configuration or a secret store.

## Auth Endpoints

### POST `/api/health-core/auth/admin/login`

Request:

- `username`
- `password`

Response on success:

- `accessToken`
- `tokenType = Bearer`
- `expiresAt`
- `productCode`
- `productRole`
- safe admin display info

Failures return a generic invalid-credentials response. The endpoint does not reveal whether a username exists, whether an admin is inactive, or whether the password was wrong.

### GET `/api/health-core/auth/admin/me`

Returns the current authenticated admin context when a valid admin JWT is present.

This endpoint is intentionally simple and only reads the authenticated request context. It does not replace endpoint-level authorization on health-record APIs.

## Bootstrap Admin

Bootstrap is controlled by:

`AdminAuth:BootstrapAdmin`

Fields:

- `Enabled`
- `Username`
- `Password`
- `DisplayName`
- `ProductRole`

The bootstrap seed:

- never runs in Production
- only runs when explicitly enabled
- requires username and password
- requires an internal admin product role
- hashes the password before saving
- does nothing if the normalized username already exists

Base/default configuration has bootstrap disabled. Development configuration also keeps it disabled by default. A local developer may enable it with environment-specific configuration for initial setup.

## Audit Behavior

Successful admin login is audited as:

- `ActionType = Login`
- `ResourceType = SecuritySettings`
- `UserId = admin user id`
- `ProductCode = InternalAdmin`
- `ProductRole = admin role`
- `Succeeded = true`

Failed login is audited as:

- `ActionType = Login`
- `ResourceType = SecuritySettings`
- `Succeeded = false`
- generic internal failure reason

Passwords are never logged. Login audit events are security/compliance records and must not be written to patient Timeline.

## Relationship to Development Fallback

Development fallback remains available when configured through `HealthCoreAuth`. This preserves local admin-panel behavior during the transition.

The new admin login backend is the first replacement path for fallback-based admin use. Phase 87E2 should teach the frontend to call the login endpoint and send the bearer token.

Production must not rely on header/default fallback.

## Future Hardening

Before production use, Health Core still needs:

- frontend login and token handling
- lockout/rate limiting
- password reset or admin provisioning workflow
- optional MFA for high-privilege roles
- token refresh/session strategy
- logout/session invalidation strategy
- stronger admin credential lifecycle
- central auth/SSO decision
- service-to-service credential model
- monitoring for login failures and fallback use

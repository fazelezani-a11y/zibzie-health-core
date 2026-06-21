# Production Auth and JWT Strategy

Phase 87A defines the production authentication strategy for Health Core. It does not implement JWT authentication, remove the development fallback, or change endpoint authorization decisions.

## 1. Executive Summary

Health Core now has endpoint authorization and audit logging across the major patient-record API surface, but the caller identity model is still development-oriented.

Current protected endpoints rely on `IHealthCoreRequestContextProvider` to resolve:

- user identity or service account identity
- product context
- product role
- correlation and HTTP request metadata

The provider reads claims from an authenticated principal when a valid bearer token is present. It also supports temporary headers and a default development fallback so the current admin panel remains usable during local development.

Before production use, Health Core should require signed user or service identity, map trusted claims into `HealthCoreRequestContext`, and disable arbitrary header/default fallback outside explicitly approved development environments.

## 2. Current State

Current implementation pieces:

- `HealthCoreRequestContext` contains `UserId`, `ServiceAccountId`, `ProductCode`, `ProductRole`, `CorrelationId`, `IpAddress`, `UserAgent`, `RequestPath`, `HttpMethod`, `IsAuthenticated`, and `IsFallbackContext`.
- `IHealthCoreRequestContextProvider.GetCurrent()` returns the current request context.
- `IHealthCoreRequestContextProvider.CreateAuthorizationContext(Guid patientId)` maps the request context into `HealthCoreAuthorizationContext`.
- `HttpHealthCoreRequestContextProvider` uses `IHttpContextAccessor`.
- `HealthCoreAuthOptions` configures whether header fallback and default development fallback are allowed.
- `JwtOptions` configures JWT issuer/audience/authority/signing-key validation settings.
- `Program.cs` registers `IHttpContextAccessor`, JWT bearer authentication, `IHealthCoreRequestContextProvider`, `IHealthCoreAuthorizationService`, and `IAuditLogService`.

Current auth configuration:

- `appsettings.json` contains a `Jwt` section with issuer, audience, and a development key.
- `Program.cs` calls `AddAuthentication`, `AddJwtBearer`, `UseAuthentication`, and `UseAuthorization`.
- Launch settings allow anonymous authentication.
- Controllers perform explicit authorization checks through `IHealthCoreAuthorizationService`; they do not rely on ASP.NET Core authorization attributes.

Current request-context resolution order:

1. Claims from `HttpContext.User`, when present:
   - user id: `ClaimTypes.NameIdentifier`, `sub`, `user_id`
   - product code: `product_code`, `product`
   - product role: `product_role`, `role`, `ClaimTypes.Role`
   - service account id: `service_account_id`, `client_id`
2. Temporary fallback headers, only when `HealthCoreAuth:AllowHeaderFallback` is enabled and the environment is not Production:
   - `X-HealthCore-Product`
   - `X-HealthCore-Product-Role`
   - `X-HealthCore-Service-Account`
   - `X-Correlation-ID`
3. Default development fallback, only when `HealthCoreAuth:AllowDefaultDevFallback` is enabled and the environment is not Production:
   - `ProductCode = InternalAdmin`
   - `ProductRole = HealthCoreAdmin`
   - `ServiceAccountId = dev-admin`

`X-Correlation-ID` is used when provided. Otherwise the ASP.NET Core trace identifier is used.

`IsFallbackContext` is true when the context uses fallback headers or default development values.

Base/default configuration disables both fallback paths. Development configuration enables them for local use. The provider ignores fallback in Production even if configuration accidentally enables it.

## 3. Why Dev/Header Fallback Is Not Production-Safe

The temporary fallback is useful for local development, but it is not a trustworthy authentication mechanism.

Risks:

- Any caller that can reach the API could send arbitrary product and role headers.
- Header values are not signed, issued, expired, or audience-bound.
- The default fallback grants InternalAdmin context even when no authenticated identity exists.
- AuditLog would record a fallback identity rather than a verified human or service identity.
- PatientAccessGrant and product access profiles become less meaningful if identity can be spoofed at the network edge.

Production must not trust arbitrary `X-HealthCore-*` headers for identity, product role, or service account identity unless those headers are added by a trusted gateway after successful authentication and are protected from client spoofing.

## 4. Required Production Identity Model

Production Health Core needs a model that supports:

- authenticated human users
- authenticated service accounts
- product-scoped callers
- stable product role mapping
- patient-scoped grants
- auditable access and denial events

Minimum production requirements:

- signed and validated JWT or equivalent service-to-service identity
- trusted issuer and audience validation
- token expiry validation
- stable subject identifier
- product context claim or trusted server-side product mapping
- product role claim or trusted server-side role mapping
- correlation id propagation
- authentication failure logging strategy
- environment-based fallback controls

Authorization should continue to use:

- `HealthPermissions`
- `ProductAccessProfiles`
- `PatientAccessGrant`
- sensitivity rules
- AuditLog

Authentication proves who the caller is. Authorization decides what that caller may do.

## 5. Recommended JWT Claims

Required or strongly recommended claims:

| Claim | Purpose | Maps to |
| --- | --- | --- |
| `sub` or `user_id` | stable human user id | `HealthCoreRequestContext.UserId` when it is a GUID |
| `product_code` | Health Core product context | `HealthCoreRequestContext.ProductCode` |
| `product_role` | Health Core product role code | `HealthCoreRequestContext.ProductRole` |
| `service_account_id` or `client_id` | machine/service identity | `HealthCoreRequestContext.ServiceAccountId` |
| `jti` | token id for replay/audit investigation | future AuditLog metadata |
| `iat` | issued-at time | token validation/audit metadata |
| `exp` | expiry time | token validation |
| `iss` | trusted issuer | token validation |
| `aud` | intended audience | token validation |

Optional or future claims:

| Claim | Purpose |
| --- | --- |
| `scope` | OAuth/service scopes if an upstream identity model uses them |
| `permissions` | optional upstream permissions, but Health Core should still use its own `HealthPermissions` catalog for decisions |
| `tenant_id` | future tenant/organization boundary if introduced |
| `organization_id` | future clinic/provider organization boundary |
| `session_id` | user session traceability |
| `auth_level` | authentication strength or assurance level |
| `amr` | authentication methods, such as password, OTP, SSO |
| `patient_owner_id` | future patient-facing ownership mapping if needed |

Claim mapping rules:

- `UserId`: read from `sub`, `user_id`, or `ClaimTypes.NameIdentifier`; production should use one canonical GUID-compatible value.
- `ServiceAccountId`: read from `service_account_id` or `client_id`.
- `ProductCode`: read from `product_code`; `product` may remain as compatibility alias.
- `ProductRole`: read from `product_role`; `role` and `ClaimTypes.Role` may remain as compatibility aliases.
- `CorrelationId`: prefer inbound `X-Correlation-ID` for trace continuity, but do not treat it as identity.

Important: JWT claims should not directly bypass `ProductAccessProfiles` or `PatientAccessGrant`. Claims establish caller context; Health Core authorization still evaluates permissions and patient scope.

## 6. Product Context Strategy

Each zibzie product must identify itself with a stable `ProductCode` and map local roles into `ProductRoles`.

| Product | Caller type | Required product code | Product role examples | PatientAccessGrant required? | Broad directory access? | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Internal Admin | human admin, support service | `InternalAdmin` | `SuperAdmin`, `HealthCoreAdmin`, `ReadOnlyAuditor`, `SupportOperator` | Not for narrow AllPatients internal exception | Yes for admin roles only | Highest trust boundary; must use strong admin authentication and audit. |
| DigiCare | mixed human and product backend | `DigiCare` | `DigiCareCaseManager`, `DigiCareClinician`, `DigiCarePersonalDoctor` | Yes, except any explicitly approved future internal operation | Depends on operating model | Care-team access should be assigned-patient or organization-scoped, not global by default. |
| HomeVisit | mixed doctor/dispatcher/product backend | `HomeVisit` | `HomeVisitDoctor`, `HomeVisitDispatcher` | Yes | No broad directory by default | Visit-scoped temporary access should be preferred. |
| Second Opinion | mixed case manager, specialist, product backend | `SecondOpinion` | `SecondOpinionCaseManager`, `SecondOpinionLeadSpecialist`, `SecondOpinionInvitedSpecialist` | Yes | No broad directory by default | Case-scoped invited access is the expected model. |
| Personal Health Record | patient, family member, shared provider | `PersonalHealthRecord` | `PersonalHealthRecordOwner`, `PersonalHealthRecordFamilyViewer`, `PersonalHealthRecordSharedProvider` | Yes | No broad directory | Own-record and explicitly shared access only. |
| Clinic Queue | receptionist, clinic admin, product backend | `ClinicQueue` | `ClinicQueueReceptionist`, `ClinicQueueClinicAdmin` | Usually yes or appointment scoped | No clinical directory by default | Should receive minimal identity/appointment context, not deep medical record access. |

## 7. User Identity vs Service Account Identity

Human user identity represents a person:

- admin
- case manager
- care-team member
- doctor
- specialist
- patient
- family member

Service account identity represents a machine or backend process:

- product backend calling Health Core
- scheduler or automation worker
- future integration service
- import or migration worker
- system-generated reminder or rule engine process

Recommendations:

- Human actions should include `UserId` whenever possible.
- Service actions should include `ServiceAccountId`.
- User-initiated product backend calls may include both a user id and service account id.
- AuditLog should preserve both when available.
- Service accounts should be scoped to one product or one operational responsibility where possible.
- Service accounts should not impersonate broad admin roles unless explicitly approved and audited.

## 8. Admin/Internal User Authentication

Internal admin users should authenticate through a production-grade identity provider.

Recommended requirements:

- SSO or equivalent centralized identity
- MFA for privileged roles
- short-lived access tokens
- refresh/session handling outside Health Core or through a dedicated auth service
- role mapping to `SuperAdmin`, `HealthCoreAdmin`, `ReadOnlyAuditor`, or `SupportOperator`
- audit of admin reads, writes, access denied, and security-sensitive actions

Admin tokens should include:

- stable user id
- `product_code = InternalAdmin`
- `product_role`
- issuer/audience/expiry
- optional session id and auth strength metadata

## 9. Service-to-Service Authentication

Product backends and system workers should authenticate with service identity, not arbitrary headers.

Possible models:

- client credentials JWT issued by a trusted identity provider
- signed internal service token
- gateway-authenticated service identity with trusted downstream claims
- mutual TLS plus server-side service mapping, if the deployment platform supports it

Service tokens should include:

- `client_id` or `service_account_id`
- `product_code`
- `product_role` for the service role
- issuer, audience, issued-at, expiry, and token id

Service-to-service calls still require Health Core authorization checks. A product service account may need `PatientAccessGrant` unless it is an explicitly trusted internal/system role.

## 10. Patient/Family/User-Facing Authentication Considerations

Patient-facing products need extra care because patient identity and record ownership are not the same concept as admin identity.

Future considerations:

- patient account id may differ from `PatientProfile.Id`
- ownership mapping may require a separate patient-account link
- family access should use explicit consent or grant records
- shared-provider access should be temporary and scoped
- PHR uploads should be audited as patient-originated or shared-originated actions
- patient-facing JWTs should not contain broad product roles

Patient and family flows should rely on `PatientAccessGrant` or a future ownership/consent model rather than hardcoded assumptions.

## 11. Product-Specific Authentication Examples

### Internal Admin

- Admin logs into internal admin identity provider.
- Token includes `sub`, `product_code = InternalAdmin`, and `product_role = HealthCoreAdmin`.
- Health Core resolves `UserId`, product context, and role from claims.
- Authorization service allows broad access only for approved InternalAdmin roles and permissions.

### DigiCare

- Care-team user logs into DigiCare.
- DigiCare maps local role to a Health Core product role, such as `DigiCareCaseManager` or `DigiCareClinician`.
- Token includes `product_code = DigiCare` and mapped `product_role`.
- Patient access is allowed only when role permissions and active grants/scopes allow it.

### HomeVisit

- Doctor or dispatcher authenticates through HomeVisit.
- Token includes `product_code = HomeVisit` and a role such as `HomeVisitDoctor`.
- Access should be temporary or visit-scoped through `PatientAccessGrant`.
- Broad longitudinal browsing should not be available by default.

### Second Opinion

- Case manager or specialist authenticates through Second Opinion.
- Token includes `product_code = SecondOpinion`.
- Specialists should use invited-case grants.
- Invited specialists should see only the prepared case data and allowed patient-record sections.

### Personal Health Record

- Patient or family member authenticates through PHR.
- Token includes `product_code = PersonalHealthRecord`.
- Owner access should map to own-record grant/ownership.
- Family/shared provider access should map to explicit grants.

### Clinic Queue

- Receptionist or clinic admin authenticates through Clinic Queue.
- Token includes `product_code = ClinicQueue`.
- Access should be minimal and appointment/logistics scoped.
- Deep health-record permissions should not be granted by default.

## 12. Environment-Based Fallback Policy

Recommended policy:

| Environment | Header fallback | Default dev fallback | Production JWT/service auth |
| --- | --- | --- | --- |
| Development | Allowed for local development | Allowed | Wired in Phase 87C; optional for local flows until frontend/service integration |
| Test/CI | Allowed only for explicit security tests | Allowed only when configured | Recommended for integration tests once available |
| Staging | Disabled by default; enable only with explicit config and access controls | Disabled | Required |
| Production | Disabled | Disabled | Required |

Rules:

- Development can keep header/default fallback for speed, but requests must remain marked with `IsFallbackContext = true`.
- Staging should behave like production unless an explicit temporary test flag enables fallback.
- Production must reject missing or invalid authentication before protected endpoint authorization.
- Production must not trust arbitrary product/role/service headers from clients.
- If a trusted gateway injects identity headers after authentication, Health Core must only accept them from that gateway boundary and should still prefer signed claims where feasible.

## 13. Migration Plan from Dev Fallback to Production Auth

Recommended migration:

1. Keep current provider behavior unchanged while documenting the policy.
2. Add auth-mode configuration without changing endpoint decisions. Completed in Phase 87B.
3. Add JWT bearer validation and map claims into the existing request context. Completed in Phase 87C.
4. Keep fallback enabled only in Development and explicitly configured test environments.
5. Update local and CI smoke tests to include JWT-backed contexts.
6. Update admin frontend to send authenticated requests.
7. Disable header/default fallback in staging.
8. Disable header/default fallback in production.
9. Add service-to-service credentials for product backend calls.
10. Monitor AuditLog for fallback contexts and authentication failures during rollout.

## 14. Testing Strategy

Near-term tests:

- provider maps `sub`/`user_id` to `UserId`
- provider maps `product_code` and `product_role`
- provider maps `service_account_id`/`client_id`
- provider marks header/default context as fallback
- protected endpoint smoke still passes with development fallback

Production-style tests after JWT is implemented:

- missing token returns `401` before protected endpoint execution
- invalid signature returns `401`
- wrong issuer/audience returns `401`
- expired token returns `401`
- valid token without permission returns `403`
- valid product role without active grant returns `403`
- valid product role with active grant succeeds
- denied access writes AuditLog
- fallback context is rejected outside Development
- service token maps to `ServiceAccountId`
- user-initiated service token can preserve both user and service identity

Security smoke tests should continue to verify allowed and denied behavior, but future production tests should stop relying on arbitrary fallback headers.

## 15. Open Questions

- Which identity provider will be used first for InternalAdmin?
- Will Health Core validate tokens directly or trust an API gateway/auth proxy?
- Will product frontends call Health Core directly, or only through product backends?
- What is the canonical user id format across zibzie products?
- How will local product roles be mapped to Health Core product roles?
- Should product roles live in tokens, or should Health Core resolve them server-side from a role mapping service?
- How will patient account ownership map to `PatientProfile.Id`?
- Which products need service accounts first?
- What token lifetime and refresh strategy is acceptable for care-team workflows?
- What authentication failure events should be written to AuditLog before controller execution?

## 16. Recommended Next Implementation Phases

Phase 87B: Auth configuration foundation - implemented

- add explicit auth mode and fallback configuration
- keep current behavior in Development
- keep fallback disabled in base/default configuration
- ignore fallback in Production even if misconfigured
- document operational settings

Phase 87C: JWT bearer authentication - implemented

- add JWT validation
- map claims to request context
- keep dev fallback only in Development
- add tests for token validation and request-context mapping

Phase 87D: Admin login/frontend integration

- admin UI sends authenticated requests
- remove reliance on header/default fallback for normal admin use
- keep local developer override only where explicitly configured

Phase 87E: Service-to-service auth

- define service account credential model
- support product backend and automation callers
- audit service account usage

Phase 87F: Production hardening

- issuer/audience/key rotation
- HTTPS and reverse proxy header policy
- authentication failure audit strategy
- monitoring and alerting
- fallback-context detection and reporting

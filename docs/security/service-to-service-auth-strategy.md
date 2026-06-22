# Service-to-Service Auth Strategy

Phase 87F defines how zibzie products should authenticate to Health Core without reusing human admin tokens.

This phase adds a strategy and tests for the existing service-account authorization path. It does not add a `ServiceAccount` table, service token issuing endpoint, central SSO, public patient login, or endpoint permission changes.

## 1. Executive Summary

Health Core will be consumed by multiple zibzie products, including DigiCare, HomeVisit, Second Opinion, Personal Health Record, and Clinic Queue.

Those products need service-to-service authentication. They must not call Health Core with `InternalAdmin` human admin tokens.

The recommended model is:

- products authenticate with signed JWTs or a future trusted identity provider
- service tokens carry `service_account_id` / `client_id`
- service tokens carry product context through `product_code`
- service tokens carry a product role through `product_role` or a future `service_role`
- `ProductAccessProfiles` defines the permission set
- `PatientAccessGrant` still gates patient-level access for non-internal products
- AuditLog records service identity separately from human user identity

## 2. Current State

Already implemented:

- JWT bearer authentication is wired in the backend.
- `HttpHealthCoreRequestContextProvider` maps:
  - `sub` / `user_id` to `UserId`
  - `service_account_id` / `client_id` to `ServiceAccountId`
  - `product_code` / `product` to `ProductCode`
  - `product_role` / `role` to `ProductRole`
- `HealthCoreAuthorizationService` accepts either `UserId` or `ServiceAccountId`.
- `PatientAccessGrant` can store `ServiceAccountId`.
- The grant lookup matches active grants by patient, product, product role, and either user id or service account id.
- AuditLog requests and entries include `ServiceAccountId`.
- Production startup validation requires safe JWT configuration and disables fallback/bootstrap in Production.

Human admin auth is separate:

- backend admin login issues `InternalAdmin` JWTs
- admin JWTs are for the Health Core admin panel only
- admin tokens should not be used by product services

## 3. Why Service Tokens Must Be Separate From Admin Tokens

Service calls and human admin actions have different risk models.

Human admin token:

- represents a person
- uses `ProductCode = InternalAdmin`
- may have broad admin permissions
- should be tied to admin login, password policy, and admin audit trails

Service token:

- represents a product backend, worker, scheduler, integration service, or automation process
- uses the consuming product code, such as `DigiCare` or `HomeVisit`
- should be least-privilege and product-scoped
- should require `PatientAccessGrant` for patient-level access unless a future carefully reviewed system role says otherwise

Reusing admin tokens for product services would bypass product-level isolation and make audit accountability much weaker.

## 4. Service Identity Model

A service identity should identify:

- product: which zibzie product is calling
- service account: which backend/client/worker is calling
- role/profile: what kind of access the product service is allowed to request
- environment and issuer: where the token came from

Examples:

| Product | Example service account id | Product role/profile direction | Typical access |
| --- | --- | --- | --- |
| DigiCare | `digicare-careteam-api` | care-team/case-management service profile | assigned patients with grants |
| DigiCare | `digicare-reminder-worker` | automation profile, future | care-plan/reminder automation only |
| HomeVisit | `homevisit-visit-api` | visit service profile | temporary visit-scoped grants |
| SecondOpinion | `secondopinion-case-api` | case management service profile | invited case grants |
| PersonalHealthRecord | `phr-api` | patient-owned/shared access profile | own/shared records only |
| ClinicQueue | `clinicqueue-api` | queue/logistics profile | minimal identity/appointment context |

The exact service role constants are intentionally not added in this phase. Until product/service boundaries are finalized, service tokens should use existing conservative product role profiles only when that mapping is explicitly approved.

## 5. Token Claims Contract

Required or strongly recommended service token claims:

- `sub` or `service_account_id`
- `client_id`
- `product_code`
- `product_role` or future `service_role`
- `jti`
- `iat`
- `exp`
- `iss`
- `aud`

Optional/future claims:

- `tenant_id`
- `environment`
- `scope`
- `app_instance_id`
- `kid` / key id
- `amr` or authentication method

Claim mapping:

| Claim | Health Core field |
| --- | --- |
| `service_account_id` | `HealthCoreRequestContext.ServiceAccountId` |
| `client_id` | `HealthCoreRequestContext.ServiceAccountId` fallback |
| `product_code` / `product` | `HealthCoreRequestContext.ProductCode` |
| `product_role` / `role` | `HealthCoreRequestContext.ProductRole` |
| `sub` / `user_id` | `HealthCoreRequestContext.UserId`, only when it is a human/user id |

For pure service calls, prefer `service_account_id` or `client_id` and do not place a human id in `user_id`.

For user-initiated product-backend calls, future tokens may include both:

- human user id
- service account id

AuditLog should preserve both when available.

## 6. Product-Scoped Authorization Model

Service authorization should use the same core decision model as human product users:

1. JWT must validate.
2. Request context must include `ServiceAccountId`, `ProductCode`, and `ProductRole`.
3. `ProductAccessProfiles.GetRoleProfile(productCode, productRole)` must exist.
4. The requested permission must be included in the product role profile.
5. For non-internal products, an active `PatientAccessGrant` must exist for the service account and patient.
6. Grant scope must match the product role profile scope.
7. sensitivity/restricted-data rules remain unchanged.

Service tokens alone should not automatically access all patients.

`InternalAdmin` remains a human/admin context. Product services should not use `InternalAdmin` unless the service is a tightly controlled internal admin automation and has a dedicated future profile.

## 7. PatientAccessGrant Interaction

For non-internal product service calls, `PatientAccessGrant` should define:

- patient id
- service account id
- product code
- product role
- scope
- authorization reason
- validity window
- grant/revoke metadata

Examples:

- DigiCare care-team backend receives a grant for an assigned patient.
- HomeVisit visit backend receives a temporary grant for a scheduled visit.
- Second Opinion case backend receives an invited-case grant.
- Personal Health Record sharing creates a temporary/family/provider grant.

If no active grant exists, authorization should deny access even when the service token is otherwise valid.

## 8. Service Account Lifecycle

Not implemented yet:

- `ServiceAccount` table
- service account creation API
- service account rotation workflow
- client secret storage
- per-service enabled/disabled state
- service account admin UI

Future lifecycle model should include:

- unique service account id
- product code
- allowed product roles/service roles
- active/inactive state
- created/revoked metadata
- key or credential references, not plaintext secrets
- rotation history
- owner/team contact

Do not store service secrets in repository configuration.

## 9. Secrets, Keys, and Rotation

Recommended staged approach:

1. Near term:
   - Health Core validates signed service JWTs through existing JWT bearer validation.
   - Tokens come from a trusted local/staging issuer or deployment-controlled secret.
   - No public service-token issuing endpoint is added.
2. Medium term:
   - central zibzie auth service or identity provider issues product service tokens.
   - Health Core validates issuer, audience, lifetime, and signing keys.
   - service tokens use a separate audience where possible, such as `Zibzie.HealthCore.Service`.
3. Production:
   - key rotation is documented and tested.
   - signing keys are supplied through secret store or identity provider metadata.
   - token lifetime is short.
   - revoked/disabled service accounts cannot get new tokens.

## 10. Audit Requirements

Service-originated actions must record:

- `ServiceAccountId`
- `ProductCode`
- `ProductRole`
- `PatientId` when applicable
- `ActionType`
- `ResourceType`
- `ResourceId` when available
- permission/scope/reason when available
- correlation id
- request path and method
- success/failure and failure reason

Human user id and service account id must not be confused.

For user-initiated product backend calls, future audit entries should include both the human user id and the product service account id when both are available.

## 11. Production Requirements

Before service tokens are used in production:

- Development fallback disabled
- bootstrap admin disabled
- real JWT authority/signing validation configured
- issuer and audience reviewed for service-token use
- product service roles/profile mappings approved
- PatientAccessGrant creation/revocation workflow exists
- service account lifecycle and rotation policy exists
- denied access is audited
- sensitive/restricted data policy reviewed
- smoke/integration tests cover service token grants and denial cases

## 12. Implementation Phases

Recommended next phases:

87F1: Service auth strategy and foundation - this phase

- document service-to-service model
- verify request context maps service claims
- verify authorization works with `ServiceAccountId` grants

87F2: Service role/profile catalog

- define conservative service role constants
- add product access profiles for product backend/service roles
- avoid broad all-patient access

87F3: Service token validation profile

- define service token audience/issuer expectations
- optionally support separate audience for human admin and service tokens
- add integration tests for service JWTs

87F4: Service account lifecycle model

- add `ServiceAccount` entity/table only after requirements are clear
- store metadata, status, product, allowed roles, and rotation metadata
- do not store plaintext secrets

87F5: Grant workflows

- implemented by Phase 88 for internal admin grant administration
- future product-specific consent/sharing workflows still need separate design

87F6: Product integration pilots

- start with one product and one scoped service flow
- run fallback-off and service-token smoke tests

## 13. Open Questions

- Which product integrates first with service-to-service auth?
- Will service tokens use the same audience as admin JWTs or a separate service audience?
- Will a central zibzie auth service issue service tokens first?
- Which product backend roles need to exist as stable `ProductRoles`?
- How will service account owners and rotation contacts be managed?
- How will user-initiated product-backend calls preserve both human and service identity?
- What is the minimum grant workflow needed for DigiCare/HomeVisit pilots?
- Should emergency service access exist, and who can issue it?

## Phase 88 Update

Phase 88 adds an internal-admin PatientAccessGrant management API. It can create
patient-scoped grants for service account ids, but it does not issue service
tokens or create a ServiceAccount lifecycle model.

See [PatientAccessGrant admin workflow](patient-access-grant-admin-workflow.md).

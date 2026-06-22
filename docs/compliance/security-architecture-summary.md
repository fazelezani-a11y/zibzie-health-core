# Security Architecture Summary

Health Core uses a centralized security model:

Role + Permission + Product Context + Scope + PatientAccessGrant + Sensitivity + AuditLog

## Core Model

- Role: the caller's responsibility inside a product, such as `HealthCoreAdmin` or `DigiCareCaseManager`.
- Permission: the action being requested, such as `ViewDocuments` or `CreatePatient`.
- Product context: the product making the request, such as `InternalAdmin`, `DigiCare`, or `HomeVisit`.
- Scope: the patient boundary, such as `AllPatients`, `AssignedPatientsOnly`, or `TemporaryAccess`.
- PatientAccessGrant: the patient-specific access record.
- Sensitivity: the data sensitivity level, currently normal/sensitive with restricted planned.
- AuditLog: security and compliance evidence for allowed and denied actions.

Identity may be a human `UserId`, a product `ServiceAccountId`, or both for future user-initiated product-backend calls.

## Request Context Flow

The intended request flow is:

1. API request enters Health Core.
2. `IHealthCoreRequestContextProvider` resolves request metadata:
   - user id or service account id
   - product code
   - product role
   - correlation id
   - IP address
   - user agent
   - request path and HTTP method
3. Controller builds a `HealthCoreAuthorizationContext`.
4. Controller calls `IHealthCoreAuthorizationService`.
5. Authorization service checks:
   - valid context
   - product role profile
   - requested permission
   - active patient access grant, except narrow InternalAdmin cases
   - scope compatibility
   - sensitivity rules
6. Controller allows or denies the request.
7. Controller writes `AuditLog` for successful access or denied access.

## Timeline vs AuditLog

Timeline is patient/care-team facing clinical and operational history. It helps users understand what happened in the patient record.

AuditLog is security, legal, compliance, and system accountability evidence. It records who accessed or changed data and whether the decision succeeded or failed.

Timeline must not be used as a substitute for AuditLog.

## Development Fallback Caveat

The current request-context provider includes a development fallback so the local admin panel can keep working before real authentication exists.

JWT bearer authentication is wired, and Phase 87E1 adds a minimal internal admin login backend that can issue `InternalAdmin` JWTs. The fallback remains available in configured non-Production environments for local development. The fallback is not production-safe. Production must use authenticated user/service identity, preferably signed JWT or service-to-service credentials, and must provide trusted product and role context.

The proposed production claim contract, product context model, and environment fallback policy are documented in [Production auth and JWT strategy](../security/production-auth-jwt-strategy.md).

The admin login and frontend token adoption path is documented in [Admin login and frontend JWT integration strategy](../security/admin-login-frontend-integration-strategy.md), and the backend admin-auth foundation is documented in [Admin auth backend foundation](../security/admin-auth-backend-foundation.md).

Service-to-service auth is documented in [Service-to-service auth strategy](../security/service-to-service-auth-strategy.md). Product services should not reuse `InternalAdmin` human admin tokens. They should present signed service JWTs with product context and service account identity, then rely on product profiles plus patient grants.

## Data Minimization Notes

Current patient list/detail endpoints preserve existing DTO shapes for frontend compatibility. Future work should minimize directory results so broad list/search does not expose national code, mobile number, email, or address unless the caller has a stronger permission and scope.

Patient Summary is currently all-or-nothing. Future work should support section-level filtering/redaction.

## Soft Deactivation

Patient deletion currently means soft deactivation with `IsActive = false`. This is preferred over hard delete for medical records unless a formal legal, retention, and operational policy defines hard-delete rules.

Future patient lifecycle workflows should keep auditability and retention requirements explicit.

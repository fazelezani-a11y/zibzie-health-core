# Health Core Authorization Service

Phase 82 added the first Health Core authorization decision service.

The service is the central authorization decision engine. It is registered in
dependency injection and is now used by protected endpoint groups across the
current patient-record API surface.

## Inputs

Authorization decisions use:

- caller identity: `UserId` or `ServiceAccountId`
- `PatientId`
- `ProductCode`
- `ProductRole`
- requested `HealthPermissions` value
- optional sensitivity level
- `ProductAccessProfiles`
- active `PatientAccessGrant` records

## Default Behavior

The service denies by default.

A request is allowed only when:

- the authorization context is valid
- the product role exists in `ProductAccessProfiles`
- the role profile includes the requested permission
- a matching active `PatientAccessGrant` exists for non-internal roles
- grant scope matches the product role profile scope
- sensitivity rules allow the requested data level

## Internal Admin Exception

The only no-grant shortcut is intentionally narrow:

- product context is `InternalAdmin`
- profile scope is `AllPatients`
- role is `SuperAdmin`, `HealthCoreAdmin`, or `ReadOnlyAuditor`
- requested permission exists in the role profile

Emergency access is not automatically allowed by this shortcut.

## Sensitivity Rules

- `Normal` or null sensitivity requires the normal permission/profile/grant path.
- `Sensitive` requires `ViewSensitiveMedicalHistory` or `ViewRestrictedData`.
- `Restricted` requires `ViewRestrictedData`.
- Unknown sensitivity is treated conservatively as sensitive.

Existing raw sensitivity string values are preserved.

## Emergency Access

`EmergencyAccess` is not an automatic bypass.

It requires the role profile to include `EmergencyAccess` and an active grant with either:

- `AuthorizationReason = Emergency`
- or `AccessScope = EmergencyAccess`

## Not Implemented Yet

- No authorization attributes.
- No frontend behavior changes.
- No emergency break-glass workflow.
- No policy-based ASP.NET Core authorization integration.
- No production JWT/service identity integration.

## Future Integration

Phase 84 applied this service to current high-risk endpoint groups and patient
profile endpoints. Future work should replace development request-context
fallbacks with production identity, add grant-management workflows, and improve
sensitivity/redaction behavior.

Production identity planning is documented in [Production auth and JWT strategy](production-auth-jwt-strategy.md).

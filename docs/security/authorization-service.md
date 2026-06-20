# Health Core Authorization Service

Phase 82 adds the first Health Core authorization decision service.

The service is a decision engine only. It is registered in dependency injection, but controllers and endpoints do not enforce it yet.

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

- No controller or endpoint enforcement.
- No authorization attributes.
- No frontend behavior changes.
- No security audit log writes.
- No emergency break-glass workflow.
- No policy-based ASP.NET Core authorization integration.

## Future Integration

Phase 84 should apply this service to high-risk endpoints first, such as documents, paraclinical results, medical history, care plan, and patient summary.

Audit logging should be added before or alongside endpoint enforcement so allowed and denied access decisions become compliance evidence.

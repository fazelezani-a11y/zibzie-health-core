# PatientAccessGrant Admin Workflow

Phase 88 adds the first internal-admin workflow/API for managing `PatientAccessGrant` records.

This workflow is intentionally narrow. It does not create a public consent UI, does not issue service tokens, does not implement central SSO, and does not give products broad all-patient access.

## Purpose

`PatientAccessGrant` is the patient-scoped access record used by `IHealthCoreAuthorizationService`.

Non-internal products and services may have product role permissions, but they still need an active matching grant to access a specific patient record.

The admin workflow lets trusted internal administrators:

- list grants for a patient
- view a grant
- create a grant for a user or service account
- revoke an active grant
- audit grant lifecycle operations

## Endpoints

### List Patient Grants

`GET /api/health-core/patients/{patientId}/access-grants`

Required permission:

`ViewPatientAccessGrants`

### Grant Detail

`GET /api/health-core/access-grants/{grantId}`

Required permission:

`ViewPatientAccessGrants`

### Create Grant

`POST /api/health-core/patients/{patientId}/access-grants`

Required permission:

`CreatePatientAccessGrant`

### Revoke Grant

`POST /api/health-core/access-grants/{grantId}/revoke`

Required permission:

`RevokePatientAccessGrant`

## Authorization Policy

Grant-management endpoints use the existing authorization service.

The new grant-management permissions are intentionally internal-admin oriented:

- `SuperAdmin` receives them through `HealthPermissions.All`.
- `HealthCoreAdmin` receives them through the existing broad internal admin profile.
- non-internal products do not receive these permissions by default.

This phase does not allow products or services to grant themselves access.

`InternalAdmin` grants cannot be created through this workflow. Internal admin access is handled by the admin auth model, not patient access grants.

## Create Request

Create requests include:

- `granteeUserId` optional
- `serviceAccountId` optional
- `productCode`
- `productRole`
- `scope`
- `reason`
- `validFrom` optional
- `validUntil` optional
- `notes` optional

At least one of `granteeUserId` or `serviceAccountId` is required.

## Validation Rules

The service validates:

- patient exists and is active
- grantee user id or service account id is supplied
- product code is known
- product code is not `InternalAdmin`
- product role is known
- product role belongs to the selected product profile
- scope is known
- scope matches the selected product role profile
- reason is known
- `validUntil` is after `validFrom`
- `validUntil` is not already expired
- notes and service account id stay within configured field lengths
- no overlapping active grant exists for the same patient, product, role, scope, and grantee

Duplicate prevention checks overlapping unrevoked validity windows, not only grants active at the current instant.

## Revocation Behavior

Revocation sets:

- `RevokedAt`
- `RevokedByUserId` when the request context has a user id
- `RevokeReason`
- `UpdatedAt`

Revoking a missing grant returns `404`.

Revoking an already revoked grant returns `409 Conflict`. This makes repeated revoke attempts explicit instead of silently changing history.

## Actor Metadata

The existing `PatientAccessGrant` table stores:

- `GrantedByUserId`
- `RevokedByUserId`

It does not currently store `GrantedByServiceAccountId` or `RevokedByServiceAccountId`. No migration was added in Phase 88.

Service-account actor metadata is still captured in AuditLog for grant lifecycle requests through `AuditLogEntry.ServiceAccountId`.

## Audit Behavior

Grant workflow endpoints audit:

- list success and denied access
- detail success and denied access
- create success, denied access, and validation/conflict/not-found failures
- revoke success, denied access, already-revoked conflicts, and not-found failures

Audit fields include:

- request user/service identity
- patient id when available
- grant id when available
- action type:
  - `View`
  - `GrantAccess`
  - `RevokeAccess`
  - `AccessDenied`
- resource type:
  - `PatientAccessGrant`
- permission attempted
- request metadata
- safe JSON metadata with product, role, scope, reason, grantee user id, and service account id

No secrets, passwords, or tokens are logged.

## Relationship With ProductAccessProfiles

Grants do not contain permission lists.

Permissions come from `ProductAccessProfiles`. A grant only says that a user/service may access a patient in a product context, under a product role, scope, reason, and validity window.

Authorization still requires:

- valid product role profile
- requested permission in the profile
- active matching grant
- scope compatibility
- sensitivity rules

## Relationship With Service-to-Service Auth

Service tokens should carry:

- `service_account_id` or `client_id`
- `product_code`
- `product_role`

The grant workflow can create patient-scoped grants for those service account ids. The service token still needs to validate through JWT auth; the grant does not issue credentials.

## Not Implemented

- public patient consent UI
- family/patient-facing sharing UI
- service token issuer
- central SSO
- ServiceAccount lifecycle table
- emergency access workflow UI
- broad product all-patient access
- grant-scoped patient directory filtering

# Audit Review Tools

Phase 94 adds the first read-only Health Core audit review surface for internal admin and compliance workflows.

This is not a clinical timeline feature. AuditLog remains security and compliance evidence. Timeline remains patient/care-team clinical history.

## What Was Added

- `GET /api/health-core/audit-log`
- bounded pagination
- read-only filtering
- patient-scoped review UI under the patient Security & Access tab
- audit logging for audit-log review itself

## Authorization

Audit review requires:

- `HealthPermissions.ViewAuditLog`

Product access profiles keep this permission restricted to internal administrative/compliance roles such as:

- `InternalAdmin` / `SuperAdmin`
- `InternalAdmin` / `HealthCoreAdmin`
- `InternalAdmin` / `ReadOnlyAuditor`

Product and service roles should not receive broad audit review permission by default.

## API Filters

The read-only endpoint supports:

- `patientId`
- `actorUserId`
- `actorServiceAccountId`
- `actionType`
- `resourceType`
- `from`
- `to`
- `succeeded`
- `page`
- `pageSize`

Pagination is bounded. `pageSize` is capped by the API to prevent accidental large audit exports through the review endpoint.

## Safe Response Shape

The endpoint returns audit event metadata needed for review:

- actor user id or service account id
- patient id when available
- product code and product role
- action type
- resource type and resource id
- permission and access scope
- success/failure and failure reason
- request metadata such as path, method, IP, user agent, and correlation id
- creation timestamp

The review endpoint does not return `MetadataJson`. Metadata may contain operational details useful for internal evidence but should not be exposed through the first UI surface without a separate redaction review.

The endpoint must never return:

- passwords
- tokens
- secrets
- request bodies
- raw clinical payloads
- unrestricted internal metadata

## UI Behavior

The first UI is patient-scoped and appears in the patient Security & Access section.

It shows:

- recent audit events for the current patient
- success/failed outcome
- action/resource type
- actor identity
- product/role context
- permission/access scope
- request path/method
- failure reason when an event was denied or failed
- correlation id when available

The UI labels the section as `AuditLog` / security review and explicitly distinguishes it from the clinical Timeline.

## Audit of Audit Review

Audit review is itself audited:

- successful review: `AuditActionTypes.View` / `AuditResourceTypes.AuditLog`
- denied review: `AuditActionTypes.AccessDenied` / `AuditResourceTypes.AuditLog`
- permission: `ViewAuditLog`
- patient id included when the review is patient-scoped

## Remaining Monitoring Gaps

Phase 94 adds human review capability, not full production observability.

Remaining gaps include:

- centralized application and infrastructure logs
- alerting on failed login bursts
- alerting on access-denied spikes
- alerting on PatientAccessGrant lifecycle events
- alerting on backup failures
- audit anomaly detection
- uptime and dependency monitoring
- incident response runbook
- SIEM/log retention policy
- tamper-resistance and audit integrity controls

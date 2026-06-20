# Audit Log

Phase 83 adds the first real Health Core `AuditLogEntry` foundation.

AuditLog is separate from patient timeline.

## Timeline vs AuditLog

`PatientTimelineEvent` is patient/care-team facing clinical and operational history. It helps people understand what happened in a patient record.

`AuditLogEntry` is security, legal, compliance, and system accountability evidence. It records who accessed or changed sensitive health data, whether the action succeeded, and the context around the decision.

Audit logs should not be ordinary patient-facing timeline content.

## What AuditLog Records

Audit entries can record:

- user or service account id
- patient id when applicable
- product context and product role
- action type
- resource type and resource id
- permission, access scope, and authorization reason
- success or failure
- failure reason for denied or failed actions
- IP address, user agent, request path, HTTP method, and correlation id
- optional structured metadata as JSON text
- creation timestamp

## Current Implementation Status

Phase 83 created:

- domain constants for audit action/resource types
- `AuditLogEntry`
- EF table/configuration/migration
- `IAuditLogService`
- `AuditLogService`
- focused service tests

Later endpoint enforcement phases now write audit logs for protected endpoint groups,
including successful access and denied access events. Endpoint groups that have not yet
been protected should follow the same pattern when enforcement is added.

## Future High-Risk Actions to Audit

Future endpoint integration should prioritize:

- viewing patient summary
- viewing documents
- viewing paraclinical results
- editing medical history
- creating/updating care plan
- sharing or exporting patient records
- granting or revoking access
- access denied decisions

## Protection Expectations

Audit logs are sensitive compliance records. They should be protected from ordinary users, exposed only through restricted admin/compliance workflows, and included in backup, retention, and incident response planning.

Audit logging should be added before or alongside endpoint authorization enforcement so Health Core can explain both allowed and denied access decisions.

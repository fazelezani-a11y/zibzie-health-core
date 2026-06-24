# Monitoring / Incident Response Runbook

Phase 102 defines a monitoring and incident response baseline for Zibzie Health
Core.

This is an operational runbook, not a monitoring platform implementation. It
does not configure a vendor, webhook, SIEM, WAF, alert channel, Ministry /
PGSB / SHAMS integration, or legal approval. All names, channels, and tools are
placeholders until an approved production operating model exists.

## Purpose and Scope

Health Core stores sensitive health data. Monitoring and incident response must
help the team detect service failure, suspicious access, backup failure,
configuration mistakes, and compliance evidence gaps without copying patient
data or secrets into incident records.

This runbook covers:

- what must be monitored
- alert categories and severity levels
- incident response lifecycle
- evidence preservation rules
- security, backup/data-safety, and operational incident examples
- roles and responsibilities
- escalation rules
- minimum incident record template
- remaining production gaps

## Current Monitoring Status

Implemented now:

- `/health` endpoint exists for basic availability checks.
- AuditLog records security-sensitive endpoint actions, admin login outcomes,
  grant lifecycle activity, and audit review access.
- Internal admin audit review endpoint/UI exists and is protected.
- Fallback-off local JWT smoke evidence exists.
- Local PostgreSQL backup/restore drill evidence exists.
- Production backup/offsite/restore plan exists.
- Production startup validation blocks unsafe auth fallback/bootstrap/JWT
  settings.

Not implemented now:

- centralized production log aggregation
- production alert delivery channel
- monitoring dashboard
- failed-login/access-denied spike alerts
- backup job monitoring integration
- WAF/reverse-proxy logs and alerts
- SIEM/log retention policy
- audit tamper-resistance controls
- formal legal/privacy incident procedure

## What Must Be Monitored

### Availability and Dependency Health

- backend `/health` endpoint
- backend API availability
- frontend availability
- database availability
- database connection errors
- deployment/startup failures
- unhandled exception rate
- latency/error-rate trends for protected APIs
- Next.js BFF/proxy availability

### Authentication and Authorization

- failed admin login spikes
- admin login throttling events
- 401/403 spikes on protected endpoints
- access-denied spikes by product/role/service account
- fallback unexpectedly enabled outside Development
- startup validation failures
- JWT validation misconfiguration or repeated invalid-token failures
- suspicious service account/product-role usage

### Patient Access and Security Events

- PatientAccessGrant create/revoke activity
- unexpected grant creation for sensitive scopes/reasons
- emergency/break-glass grant activity if enabled
- AuditLog review activity
- unauthorized audit-log review attempts
- unusually broad patient directory access
- repeated access to many patient records by one actor
- unusual access to restricted/sensitive records

### Backup and Data Safety

- scheduled backup success/failure
- offsite upload success/failure
- backup encryption success/failure
- stale backup age
- unusually small or large backup files
- missing uploaded-document/file backups
- restore drill schedule and evidence status
- restore drill failures
- backup storage lifecycle/deletion errors

### Compliance and Evidence

- missing audit events for protected workflows
- audit write failures
- stale smoke evidence
- missing restore evidence
- missing production fallback-off smoke evidence
- expired or undocumented key rotation
- incident records missing evidence or closure notes

## Alert Categories

| Category | Examples |
| --- | --- |
| Security | failed login spike, denied-access spike, unexpected grant creation, unauthorized audit-log review, JWT/fallback misconfiguration |
| Availability | backend down, frontend down, database unavailable, high 5xx rate, deployment startup failure |
| Backup / data safety | backup job failed, offsite upload failed, restore drill failed, backup too old, document storage not backed up |
| Configuration / secrets | fallback enabled, bootstrap enabled, JWT validation unsafe, key rotation overdue, secret exposure suspected |
| Compliance / evidence | audit review overdue, missing restore evidence, stale smoke evidence, incident postmortem missing |

## Severity Levels

| Severity | Meaning | Examples | Initial response target |
| --- | --- | --- | --- |
| P0 Critical | Active or likely compromise, major data loss risk, production service outage, or real patient-data exposure risk. | production DB unavailable, suspected credential/token leak, backup restore needed, widespread unauthorized access, fallback enabled in production | Immediate response by technical operator and security/product owners. |
| P1 High | Security or availability issue with limited scope or strong potential to become critical. | repeated failed admin logins, suspicious grant creation, failed backup, audit write failures, high 5xx rate | Same-day triage and containment. |
| P2 Medium | Degraded control, missing evidence, or operational issue without immediate patient-data exposure. | restore drill overdue, smoke evidence stale, single service account anomaly, monitoring gap | Planned remediation with owner/date. |
| P3 Low | Documentation, hygiene, or improvement item. | runbook wording stale, dashboard refinement, non-blocking alert tuning | Backlog with periodic review. |

## Incident Response Lifecycle

1. Detect
   - alert fires, smoke fails, audit review finds anomaly, operator observes issue, or user reports issue.
2. Triage
   - confirm scope, severity, affected systems, whether patient data or secrets may be involved.
3. Contain
   - disable affected account/service, revoke grant, rotate secret, stop deployment, block source, or isolate restore environment as appropriate.
4. Investigate
   - review AuditLog, application logs, deployment logs, backup logs, smoke evidence, and configuration history.
5. Communicate
   - notify internal stakeholders based on severity and legal/privacy guidance.
6. Recover
   - restore service, rerun smoke tests, verify backups, rotate keys, or correct configuration.
7. Document
   - record timeline, evidence, actions, decisions, remaining risk, and whether patient data/secrets were involved.
8. Postmortem
   - identify root cause, prevention tasks, monitoring gaps, and policy updates.

## Evidence Preservation

Preserve:

- AuditLog entries and correlation ids
- application logs
- deployment/startup logs
- authentication/authorization failure summaries
- backup job logs
- restore drill evidence
- fallback-off smoke evidence
- database availability/backup status evidence
- configuration change records
- screenshots only when they do not expose patient data, secrets, tokens, or
  internal-only payloads

Do not copy into incident docs:

- raw patient data
- passwords
- JWTs or bearer tokens
- database connection strings
- secret-store values
- backup encryption keys
- full request/response bodies with clinical payloads
- unrestricted AuditLog metadata without redaction review

Incident evidence should use identifiers only when necessary and should prefer
correlation ids, event ids, timestamps, and redacted summaries.

## Security Incident Examples

### Repeated Failed Admin Logins

Potential signals:

- many failed login audit entries for one username or IP
- admin login throttle events
- multiple usernames from one IP/source

Initial actions:

- classify severity based on volume and target account
- review AuditLog and application logs
- verify no successful login followed the failures
- consider temporarily disabling affected account if supported
- add edge/WAF block if infrastructure supports it
- preserve evidence without passwords or tokens

### Access-Denied Spike

Potential signals:

- sudden increase in `AccessDenied`
- repeated denied access for one product role/service account
- denied access across many patients

Initial actions:

- identify actor, product context, permission, and affected patient count
- check whether a deployment/config change caused the spike
- confirm whether access was correctly denied
- investigate suspicious actor behavior if unexplained

### Unexpected PatientAccessGrant Creation

Potential signals:

- grant created for unusual product/role/scope/reason
- grant created outside expected admin workflow
- rapid grant creation/revocation bursts

Initial actions:

- review grant AuditLog entries
- identify admin actor and request context
- confirm business justification with product owner
- revoke suspicious grant if appropriate
- preserve grant id and audit correlation ids

### Unauthorized AuditLog Review Attempt

Potential signals:

- denied `ViewAuditLog`
- audit review by unexpected internal actor
- repeated probing of audit endpoint

Initial actions:

- verify actor role and product context
- confirm endpoint denied access
- review related patient/access activity
- escalate to security reviewer if repeated or broad

### Fallback Unexpectedly Enabled

Potential signals:

- fallback-off smoke fails
- Production startup validation failure
- protected endpoints allow dev headers in staging/production

Initial actions:

- treat as P0/P1 depending environment
- stop or isolate deployment if production-like
- inspect `HealthCoreAuth` settings
- rerun fallback-off smoke after correction
- preserve configuration evidence with secret values redacted

### JWT Validation Misconfiguration

Potential signals:

- startup validation failure
- invalid tokens accepted or valid tokens rejected
- missing issuer/audience/lifetime validation

Initial actions:

- stop unsafe deployment if needed
- verify JWT settings from secret/config source
- rotate key if compromise suspected
- rerun admin login and fallback-off smoke

## Backup / Data Incident Examples

### Scheduled Backup Failed

Initial actions:

- confirm whether last good backup is within RPO
- review backup job logs without exposing secrets
- rerun backup if safe
- alert product/security owner if RPO is at risk
- document issue and fix

### Offsite Upload Failed

Initial actions:

- confirm local backup exists and is encrypted
- check storage credentials/permissions without exposing values
- retry upload after correcting issue
- verify offsite object exists through safe metadata

### Restore Drill Failed

Initial actions:

- mark backup as not fully validated
- preserve restore logs
- identify whether failure is backup corruption, environment issue, key issue, or procedure issue
- rerun drill with a known-good backup if needed
- update runbook

### Corrupted Backup

Initial actions:

- do not delete corrupted artifact until investigation completes
- find last known-good backup
- test restore from last known-good backup
- investigate backup job and storage integrity
- escalate if RPO/RTO is breached

### Document Storage Missing or Not Backed Up

Initial actions:

- identify whether metadata exists without binary file
- stop deletion/lifecycle policy if causing loss
- verify object/file backup job and restore order
- document affected scope without listing patient data

## Operational Incident Examples

### API Down

Initial actions:

- check `/health`
- check deployment/startup logs
- check database connectivity
- check recent config/secret changes
- rerun smoke after recovery

### Database Down

Initial actions:

- classify as P0 if production
- check database provider/status
- protect backup state before attempting destructive action
- follow restore/disaster recovery plan only if authorized

### Frontend Down

Initial actions:

- verify Next app availability
- check route-handler/proxy errors
- confirm backend still protects endpoints
- test `/login`, `/patients`, and `/api/admin-auth/me` after recovery

### Dependency Unavailable

Initial actions:

- identify dependency and affected workflows
- degrade safely where possible
- avoid bypassing authorization or audit controls
- document incident and recovery

## Roles and Responsibilities

| Role | Responsibilities |
| --- | --- |
| Technical operator | Monitor systems, triage alerts, collect logs, execute approved restore/redeploy actions, maintain backup/monitoring evidence. |
| Security reviewer | Review suspicious access, AuditLog entries, grant changes, auth anomalies, and evidence preservation. |
| Product owner | Decide workflow impact, user communication, care-team operational priority, and product-level remediation. |
| Legal/privacy reviewer | Decide privacy notifications, patient-data exposure assessment, retention/disclosure implications, and external reporting needs. |
| External infrastructure provider | Provide hosting/database/storage/network status, backup platform evidence, and incident support where applicable. |

Every production incident should have a single incident owner and an explicit
severity.

## Escalation Rules

Escalate immediately when:

- real patient data may have been exposed
- secrets, JWT keys, backup keys, or admin credentials may be compromised
- production database or backup integrity is at risk
- production service is down or severely degraded
- fallback/dev auth is active in production
- a suspicious grant or audit review involves many patients
- legal/privacy notification may be required

Escalation path placeholders:

- technical operator: `<technical-operator-channel>`
- security reviewer: `<security-reviewer-channel>`
- product owner: `<product-owner-channel>`
- legal/privacy reviewer: `<legal-privacy-channel>`
- infrastructure provider: `<infrastructure-support-channel>`

Do not put real webhook URLs, phone numbers, credentials, or secrets in this
repository.

## Minimum Incident Record Template

Use this template without patient row data or secrets:

| Field | Value |
| --- | --- |
| Incident id | `<id>` |
| Opened at | `<timestamp>` |
| Closed at | `<timestamp or open>` |
| Severity | `<P0/P1/P2/P3>` |
| Category | `<security/availability/backup/config/compliance>` |
| Summary | `<short description>` |
| Detection source | `<alert/smoke/audit review/user report/operator>` |
| Affected systems | `<backend/frontend/db/backup/audit/auth>` |
| Patient data involved? | `<unknown/no/yes, reviewed by legal/privacy>` |
| Secret/token involved? | `<unknown/no/yes>` |
| Containment actions | `<summary>` |
| Evidence preserved | `<audit ids/correlation ids/log references/redacted screenshots>` |
| Recovery actions | `<summary>` |
| Validation after recovery | `<smoke/tests/manual checks>` |
| Owner | `<role/person per policy>` |
| Follow-up tasks | `<links or ids>` |
| Legal/privacy review needed? | `<yes/no/unknown>` |
| Postmortem required? | `<yes/no>` |

## Audit Review Procedure

For security investigations:

1. Use the protected AuditLog review endpoint/UI.
2. Filter by patient id, actor user id, service account id, action, resource,
   outcome, and time range.
3. Record only necessary event ids, timestamps, correlation ids, and summaries.
4. Do not export raw metadata unless an approved redaction process exists.
5. Do not use clinical Timeline as a substitute for AuditLog.
6. Audit review access is itself audited and should be part of the evidence.

## Backup Failure Response Procedure

1. Confirm last successful backup timestamp.
2. Compare against approved RPO.
3. Check backup job logs and storage status without exposing secrets.
4. Retry backup if safe.
5. Escalate if RPO is at risk or backup corruption is suspected.
6. Confirm offsite upload and encryption where applicable.
7. Record incident evidence.
8. Schedule a restore drill if backup integrity is uncertain.

## Remaining Gaps

Still required before production:

- centralized logs
- monitoring provider selection
- alert delivery channel
- on-call rotation and escalation contact list
- WAF/reverse-proxy logs and rate-limit alerts
- backup monitoring integration
- SIEM/log retention policy
- audit tamper-resistance and integrity controls
- legal/privacy incident procedures and notification policy
- production uptime/dependency monitoring
- alert tests and incident simulation exercises
- dashboards for failed logins, denied access, grant changes, audit review, and
  backup health

## Non-Claims

This runbook does not claim:

- monitoring is implemented
- alerts are wired
- incident response is legally approved
- centralized logs or SIEM exist
- production backup monitoring exists
- Ministry / PGSB / SHAMS readiness or certification is complete


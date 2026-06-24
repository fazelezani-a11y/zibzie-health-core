# Production Backup / Offsite / Restore Plan

Phase 101 extends the local Health Core backup/restore foundation into a
production-readiness plan.

This document is an operational plan, not a completed production backup
implementation. It does not configure cloud storage, offsite replication,
secrets, backup encryption keys, Ministry / PGSB / SHAMS connectivity, or legal
approval. All infrastructure names, storage targets, key references, and
operator identities below are placeholders until an approved deployment design
exists.

## Purpose and Scope

Health Core stores sensitive health data. Backups inherit the sensitivity of the
live database and must be protected, monitored, retained, restored, and deleted
according to approved policy.

This plan covers:

- PostgreSQL production backup planning
- uploaded medical document/file backup planning
- offsite backup storage expectations
- encryption and key-handling expectations
- retention and secure deletion considerations
- backup monitoring and failed-backup alerting requirements
- local, staging/prod-like, and production restore drill expectations
- evidence template for restore drills
- RPO/RTO placeholders and decision points
- legal/privacy and Ministry-readiness dependencies

## Current Implemented State

Implemented now:

- `scripts/backup-postgres.ps1` creates timestamped local PostgreSQL custom
  format dumps.
- `scripts/restore-postgres.ps1` can restore a specified local dump and requires
  explicit confirmation or `-Force`.
- `backups/` is ignored by git.
- Phase 93B ran a local Docker PostgreSQL backup and restored it into a
  temporary test database.
- The local drill verified table metadata only and did not print row data.
- Backup/restore expectations are documented in
  [Backup / restore / data safety](backup-restore.md).

Not implemented now:

- scheduled production backups
- encrypted offsite backup storage
- production-like restore drill evidence
- uploaded document/file binary backup
- backup failure monitoring and alerting
- final retention/deletion policy
- production backup key rotation and emergency recovery

## Target Production Backup Architecture

Target architecture, using placeholders:

1. Production PostgreSQL database runs in an approved production environment.
2. A scheduled backup job creates encrypted database backups.
3. Backups are copied to an approved offsite target such as
   `<offsite-backup-target>`.
4. Backup encryption keys are managed by `<secret-or-key-management-system>`.
5. Backup job logs and metrics are sent to `<monitoring-alerting-system>`.
6. Failed or stale backups alert `<on-call-or-operations-owner>`.
7. Restore drills run in an isolated restore-test environment.
8. Restore evidence is recorded without exposing patient row data.

The backup target must be outside the primary database host and primary runtime
environment. A database volume snapshot by itself is not enough unless it is
also copied, encrypted, retained, monitored, and restore-tested.

## PostgreSQL Production Backup Plan

Minimum production plan:

- Use a production-grade PostgreSQL backup method approved by the operator:
  - managed database automated backups, and/or
  - scheduled `pg_dump` / `pg_basebackup`, and/or
  - write-ahead-log archiving / point-in-time recovery where supported.
- Define backup schedule based on approved RPO/RTO.
- Encrypt backups before leaving the production trust boundary.
- Store backups offsite or in a separate protected storage account/project.
- Retain enough backups to cover operational recovery and legal obligations.
- Monitor backup creation, upload, age, size, and restore usability.
- Run restore drills on a schedule.

Current local PowerShell scripts may inform operator runbooks, but they are not
a complete production scheduler, encryption layer, storage uploader, or monitor.

## Uploaded Medical Document / File Storage Backup Plan

The current local backup scripts cover PostgreSQL only.

If document binaries are stored outside PostgreSQL, production backup must also
cover:

- object storage bucket/container or file volume
- document encryption keys
- file metadata and database references
- antivirus/scan metadata if later added
- restore ordering between database metadata and file storage
- consistency checks for metadata rows whose binary object is missing
- secure deletion of expired backups and deleted files

Document metadata without the underlying binary file is not a complete restore.

## Offsite Backup Strategy

Offsite backup requirements:

- Use an approved storage target separate from the primary production runtime.
- Use environment-specific paths/prefixes.
- Restrict access to named operators/service accounts.
- Enable storage access logging where available.
- Use lifecycle policies only after retention is legally approved.
- Do not use personal cloud folders, ad hoc shared drives, or public buckets.
- Do not document real bucket names, account ids, credentials, or keys in the
  repository.

Placeholder examples only:

- `<production-db-backup-bucket-or-container>`
- `<production-document-backup-bucket-or-container>`
- `<backup-encryption-key-reference>`
- `<backup-monitoring-dashboard>`

## Encryption Expectations

Before production:

- Encrypt backups at rest.
- Encrypt backups in transit.
- Keep encryption keys outside backup files.
- Separate backup operators from key administrators where practical.
- Rotate backup encryption keys according to approved procedure.
- Test restore with rotated keys.
- Document emergency key recovery.

The local scripts do not encrypt dump files. Production operators must add an
approved encryption step before any backup leaves the protected environment.

## Backup Secret and Key Handling

Secrets that must not be committed or printed:

- database password or connection string
- backup storage credentials
- backup encryption key
- object/file storage credentials
- restore-test database credentials
- monitoring/alerting tokens
- Ministry / PGSB / SHAMS credentials if added later

Required controls:

- use an approved secret store or deployment secret mechanism
- restrict access by operator role
- document rotation owner and cadence
- log secret changes where platform supports it
- use separate credentials for backup creation and restore testing where
  possible

See [Production environment and secrets](production-environment-and-secrets.md).

## Retention and Secure Deletion

No final retention period is defined in this phase.

Planning questions:

- How long should daily, weekly, and monthly backups be retained?
- How long should AuditLog data be retained?
- Do inactive/deactivated patient records require separate retention?
- How should uploaded documents follow database retention?
- How are expired backups securely deleted?
- How are restored temporary environments destroyed after drills?
- Who approves emergency restore and deletion actions?

Retention and deletion policy requires formal legal/privacy review. See
[Legal / privacy / retention baseline](../compliance/legal-privacy-retention-baseline.md).

## Monitoring and Failed-Backup Alerting

Production backup monitoring should alert on:

- backup job failure
- backup upload failure
- backup encryption failure
- stale backup age beyond threshold
- unexpectedly small or large backup size
- missing document/file backup
- restore drill overdue
- offsite storage access errors
- backup storage lifecycle/deletion errors

Minimum evidence:

- monitoring dashboard or scheduled report
- alert route and owner
- test alert evidence
- incident response path for backup failures
- backup job logs with no secrets or patient row data

Backup failure response is documented in
[Monitoring / incident response runbook](monitoring-incident-response-runbook.md).

## Restore Drill Schedule

Suggested schedule until formal policy is approved:

| Environment | Suggested cadence | Purpose |
| --- | --- | --- |
| Local/dev | Before risky local changes and after backup script changes. | Validate local mechanics and script safety. |
| Staging/prod-like | After major releases and at least quarterly. | Validate production-like backup, encryption, offsite retrieval, and restore process without real production impact. |
| Production emergency restore | Only during approved incident response. | Restore service after data loss/outage using approved incident procedure. |

Production restore drills should normally restore into an isolated
restore-test environment, not overwrite the active production database.

## Restore Drill Evidence Template

Record the following without patient row data:

| Field | Evidence |
| --- | --- |
| Drill date/time | `<timestamp>` |
| Environment | `<local/staging/prod-like/production emergency>` |
| Operator | `<role or name, according to policy>` |
| Backup source | `<backup id/path pattern, no secret values>` |
| Backup timestamp | `<timestamp>` |
| Backup type | `<database/document/both>` |
| Encryption verified | `<yes/no/how>` |
| Offsite retrieval verified | `<yes/no/how>` |
| Restore target | `<restore-test environment>` |
| Restore duration | `<duration>` |
| Validation checks | `<metadata checks, smoke checks, API checks>` |
| Sensitive data handling | `<no row data printed/exported>` |
| Issues found | `<summary>` |
| Cleanup completed | `<yes/no>` |
| Decision | `<usable/not usable/follow-up required>` |

Recommended validation checks:

- database restore completes
- migrations table exists
- key security tables exist
- PatientAccessGrant rows exist
- AuditLog rows exist
- Timeline remains separate from AuditLog
- admin login/session path works in the restore-test environment
- fallback-off smoke can run where appropriate
- uploaded document metadata and binaries are consistent if binary storage is in
  scope

## RPO / RTO Decision Points

Placeholders until product/legal/operator approval:

- RPO: `<approved maximum data-loss window>`
- RTO: `<approved maximum recovery time>`
- backup frequency: `<daily/hourly/continuous>`
- offsite retention: `<approved schedule>`
- restore drill cadence: `<approved cadence>`
- document/file backup consistency target: `<approved target>`

RPO/RTO should consider patient safety, operational continuity, legal
requirements, storage cost, and restore complexity.

## Operator Responsibilities

Operators must own:

- production backup scheduler
- offsite storage configuration
- backup encryption and key handling
- restore-test environment
- backup and restore access control
- monitoring and alerting
- restore drill execution and evidence
- secure deletion of expired backups and temporary restores
- incident response escalation
- coordination with legal/privacy owners

Engineering owns application-level documentation, local scripts, restore
validation checklists, and application smoke checks. Engineering does not own
cloud/operator evidence by documentation alone.

## Legal / Privacy Dependencies

Production backup policy depends on:

- approved retention periods
- secure deletion rules
- patient record deactivation/deletion policy
- AuditLog retention policy
- backup access policy
- data export/disclosure rules
- incident response and breach review policy

Do not use backup retention settings as a substitute for legal/privacy approval.

## Ministry / PGSB / SHAMS Readiness Implications

This plan improves readiness, but does not claim Ministry, PGSB, GSB, SHAMS, or
government exchange approval.

Future review may require:

- backup policy evidence
- restore drill evidence
- encrypted offsite storage evidence
- operator access policy
- incident response process
- audit retention and integrity policy
- infrastructure/network evidence
- official current requirements review

## Remaining Gaps

Still required before production:

- choose approved offsite backup storage
- configure scheduled PostgreSQL backups
- configure uploaded document/file backup if binary storage exists
- encrypt backups and manage keys through approved process
- define final retention and secure deletion policy
- configure monitoring and failed-backup alerts
- run staging/prod-like restore drill using encrypted offsite backup
- document restore evidence
- define production emergency restore procedure
- define backup/operator access policy
- integrate backup failures with incident response
- complete formal legal/privacy review

## Non-Claims

This document does not claim:

- production backups are implemented
- offsite backup storage is configured
- backup encryption is configured
- production restore has been tested
- uploaded document/file backup is complete
- retention/legal approval is complete
- Ministry / PGSB / SHAMS readiness or certification is complete

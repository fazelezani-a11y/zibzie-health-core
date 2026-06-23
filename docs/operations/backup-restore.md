# Health Core Backup / Restore / Data Safety

Health Core stores sensitive health information. Backups are therefore
sensitive health data and must be protected with the same seriousness as the
live database.

This runbook provides a local/development PostgreSQL backup and restore
foundation for the current Docker setup. It is not a complete production backup
system.

## Purpose

Backup/restore readiness supports:

- recovery from accidental data loss
- rollback after failed local/development changes
- disaster recovery planning
- audit and compliance readiness
- Ministry / PGSB / SHAMS-style review evidence later

The goal is not only to create backup files. The team must also prove that
backups can be restored and that restored data is complete enough for the
required recovery objective.

## Data-Safety Principles

- Treat backup files as sensitive health data.
- Do not commit backup files.
- Do not store backups in public or personal cloud folders.
- Encrypt backups at rest before production use.
- Restrict backup access to approved operators.
- Test restore regularly.
- Document restore evidence, not only backup success.
- Keep secrets, JWT keys, database passwords, and encryption keys out of the
  repository.
- Avoid using real patient data in development or unapproved test
  environments.

## What Must Be Backed Up

At minimum:

- PostgreSQL database:
  - patient records
  - medical history
  - documents metadata
  - paraclinical results
  - care plan items
  - reminders
  - measurements
  - timeline events
  - patient access grants
  - audit log entries
  - admin users and security tables
- uploaded medical documents or file/blob storage, if/when binary storage is
  implemented outside PostgreSQL
- deployment configuration that is safe to archive
- migration history
- operational runbooks

Do not back up secrets into ordinary documentation repositories. Secret backups
must use an approved secret-management process.

## Current Local PostgreSQL Setup

The current `docker-compose.yml` defines:

- service: `db`
- container: `zibzie-healthcore-db`
- image: `postgres:16-alpine`
- database: `zibzie_healthcore`
- user: `zibzie`
- local port: `5432`
- volume: `healthcore_pgdata`

The local API connection string points at the same database.

## Local Backup Script

Script:

`scripts/backup-postgres.ps1`

Default behavior:

- uses Docker
- runs `pg_dump` inside `zibzie-healthcore-db`
- creates a PostgreSQL custom-format dump
- copies the dump to `.\backups\postgres`
- uses timestamped filenames
- does not print database passwords

Example:

```powershell
.\scripts\backup-postgres.ps1
```

Custom output directory:

```powershell
.\scripts\backup-postgres.ps1 -OutputDirectory "D:\healthcore-backups\local"
```

Local `pg_dump` mode:

```powershell
$env:PGPASSWORD = "<database-password>"
.\scripts\backup-postgres.ps1 -UseLocalPgDump -DbHost localhost -DbPort 5432
Remove-Item Env:\PGPASSWORD
```

Generated local backup files are ignored by `.gitignore` through:

`backups/`

## Local Restore Script

Script:

`scripts/restore-postgres.ps1`

Restore is destructive. The script requires either:

- typing `RESTORE` interactively, or
- passing `-Force`

Example:

```powershell
.\scripts\restore-postgres.ps1 -BackupFile ".\backups\postgres\zibzie_healthcore-20260623-120000.dump"
```

Non-interactive local/dev restore:

```powershell
.\scripts\restore-postgres.ps1 `
  -BackupFile ".\backups\postgres\zibzie_healthcore-20260623-120000.dump" `
  -Force
```

Local `pg_restore` mode:

```powershell
$env:PGPASSWORD = "<database-password>"
.\scripts\restore-postgres.ps1 `
  -BackupFile ".\backups\postgres\zibzie_healthcore-20260623-120000.dump" `
  -UseLocalPgRestore
Remove-Item Env:\PGPASSWORD
```

Use restore only against local/dev or approved restore-test environments unless
a production incident response procedure explicitly authorizes it.

## Restore Validation Checklist

After a restore test:

- API starts successfully.
- EF migrations table is present.
- patient directory loads.
- patient detail loads for a known restored patient.
- protected endpoint authorization still works.
- admin login/session still works, or documented admin recovery path exists.
- PatientAccessGrant rows are present.
- AuditLog rows are present.
- Timeline rows remain separate from AuditLog.
- document metadata rows are present.
- uploaded document binary storage is restored if applicable.
- smoke tests pass where practical.

Record:

- backup file name
- backup timestamp
- restore environment
- restore operator
- restore duration
- validation checks performed
- issues found
- decision: usable / not usable

## Local Drill Status

Latest local drill:

| Field | Result |
| --- | --- |
| Date | 2026-06-23 |
| Environment | Local Windows development workspace with Docker PostgreSQL container `zibzie-healthcore-db` |
| Backup command | `.\scripts\backup-postgres.ps1` |
| Backup result | Succeeded |
| Backup output | `backups/postgres/zibzie_healthcore-20260623-181950.dump` |
| Backup size | 75,371 bytes |
| Git handling | Backup path is ignored by `.gitignore` through `backups/` |
| Restore target | Temporary database `zibzie_healthcore_restore_drill_20260623_181950` |
| Restore command | `.\scripts\restore-postgres.ps1 -BackupFile ".\backups\postgres\zibzie_healthcore-20260623-181950.dump" -DatabaseName "zibzie_healthcore_restore_drill_20260623_181950" -Force` |
| Restore result | Succeeded |
| Restore verification | Metadata-only check confirmed 16 public tables and key security tables including `AdminUsers`, `AuditLogEntries`, and `PatientAccessGrants` |
| Cleanup | Temporary restore database was dropped and verified absent |
| Sensitive data handling | No row data was inspected or printed |

Limitations:

- This was a local/dev drill, not production evidence.
- It validates PostgreSQL backup/restore mechanics only.
- It does not validate uploaded document binary storage restore.
- It does not validate encrypted/offsite backups.
- It does not replace periodic staging/production-like restore drills.

## Release Candidate Status

Phase 96 treats backup/restore as acceptable for the internal release-candidate
baseline because local PostgreSQL backup and restore mechanics have been
documented and drilled.

Backup/restore remains a production blocker until automated encrypted backups,
offsite storage, monitoring, retention approval, document/file storage backup,
and repeated production-like restore evidence are in place.

## Uploaded Medical Documents and File Storage

The current backup scripts only back up PostgreSQL.

If Health Core stores uploaded document binaries outside PostgreSQL later, the
production backup plan must also cover:

- object storage bucket/container
- local file volume
- antivirus scan metadata if applicable
- document encryption keys
- document retention and deletion policy
- restore order between database metadata and binary files

Document metadata without the underlying binary file is not a complete restore.

## Environment, Config, and Secrets

Back up operational configuration separately from secrets.

Do back up:

- deployment manifests
- infrastructure configuration templates
- migration history
- runbooks

Do not commit or casually copy:

- database passwords
- JWT signing keys
- service-account credentials
- encryption keys
- admin bootstrap passwords
- PGSB/GSB/SHAMS integration secrets

Production secrets must come from a secret store or approved deployment
configuration system.

## Backup Frequency Recommendations

Initial recommendations for planning:

- Development: manual backups before risky local work.
- Staging: scheduled daily backups and restore drills after major releases.
- Production: automated backups based on approved RPO/RTO.

Production frequency should be decided by:

- acceptable data-loss window
- patient safety and operational impact
- legal/regulatory expectations
- size and cost of retained backups
- availability requirements

## Retention Recommendations

Retention must be reviewed legally before production.

Planning model:

- short-term daily backups for fast recovery
- longer weekly/monthly retention where legally allowed
- separate retention rules for AuditLog, clinical data, and uploaded documents
- secure deletion process when backups expire

Do not define final medical-record retention in code without legal review.

## Encryption and Access Control

Before production:

- encrypt backups at rest
- encrypt backups in transit
- store backups outside the primary database host
- restrict access to approved operators
- log backup/restore operator actions
- test key rotation and emergency key recovery

The local scripts do not encrypt dump files. Operators must encrypt files before
moving them outside a protected local environment.

## Disaster Recovery Notes

A production disaster recovery plan should define:

- recovery point objective (RPO)
- recovery time objective (RTO)
- primary and secondary restore locations
- database restore sequence
- document/file restore sequence
- identity/secrets restore sequence
- smoke tests after restore
- communication and incident escalation path

The current scripts are suitable for local/dev restore testing only.

## Production Gaps

Still required before production:

- automated scheduled backups
- encrypted offsite backups
- backup failure monitoring and alerting
- restore drill evidence
- backup retention/deletion policy
- backup access-control policy
- database encryption and storage encryption
- document/file storage backup plan
- secret/key backup and rotation plan
- incident response and disaster recovery runbook
- legal/privacy review of retention and deletion requirements
- Ministry / PGSB / SHAMS readiness evidence package updates

## Related Documentation

- [Compliance documentation index](../compliance/README.md)
- [Ministry / PGSB / SHAMS readiness checklist](../compliance/ministry-readiness-checklist.md)
- [Production security hardening](../security/production-security-hardening.md)
- [Final production readiness checklist](../security/final-production-readiness-checklist.md)

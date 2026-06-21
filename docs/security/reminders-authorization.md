# Reminders Authorization

Phase 84E2 protects the Reminder endpoint group with authorization and audit logging.

Documents, Paraclinical Results, current Medical History endpoints, and Care Plan endpoints were protected in earlier phases. This phase is limited to Patient Reminders.

## Protected Routes

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/reminders` | GET | `ViewReminders` | `View` | `Reminder` |
| `/api/health-core/patients/{patientId}/reminders` | POST | `CreateReminder` | `Create` | `Reminder` |
| `/api/health-core/reminders/{reminderId}` | GET | `ViewReminders` | `View` | `Reminder` |
| `/api/health-core/reminders/{reminderId}` | PUT | `EditReminder`, `CompleteReminder`, or `CancelReminder` | `Update` | `Reminder` |
| `/api/health-core/reminders/{reminderId}` | DELETE | `EditReminder` | `Delete` | `Reminder` |

Update uses `CompleteReminder` when the requested status is `Done`, and `CancelReminder` when the requested status is `Cancelled`. Other updates use `EditReminder`.

## Audit Logging

Successful actions are audited with:

- `AuditActionTypes.View`
- `AuditActionTypes.Create`
- `AuditActionTypes.Update`
- `AuditActionTypes.Delete`

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.Reminder`
- attempted permission
- patient id when available
- reminder id when available
- request context metadata
- authorization denial reason

## Sensitivity Handling

Reminder endpoints call the authorization service through section-aware methods:

- reads use `CanViewPatientSectionAsync`
- writes use `CanEditPatientSectionAsync`

Single-reminder routes use the reminder's current `SensitivityLevel`.

Create uses the requested `SensitivityLevel`.

Update uses the requested `SensitivityLevel` when supplied; otherwise it uses the existing reminder sensitivity.

List endpoints use baseline `ViewReminders` because they can return mixed-sensitivity reminders and do not perform per-record redaction yet. A future phase may add filtering or redaction for mixed-sensitivity lists if needed.

## Privacy Sensitivity

Reminders are privacy-sensitive because they can reveal medications, care plans, follow-ups, diagnoses, lifestyle instructions, or future care activity.

Phase 84E2 only adds endpoint authorization and audit logging. Existing automation and timeline side effects are unchanged:

- auto-generated care-plan due reminders are unchanged
- `CarePlanDueReminderService` behavior is unchanged
- reminder creation side effects are unchanged
- timeline behavior is unchanged

## Development Fallback

JWT bearer authentication and internal admin login are now available, but real production identity/frontend integration is still incomplete.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This keeps the development admin panel usable. It is marked as fallback context and is not production-safe.

## Not Included Yet

- This document only describes the Reminders enforcement phase; later phases protected additional endpoint groups.
- No frontend changes.
- No production identity rollout or frontend JWT integration.
- No access grant creation workflow.

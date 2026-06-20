# Timeline Authorization

Phase 84F protects the Timeline endpoint group with authorization and audit logging.

This phase is limited to Patient Timeline events. Standalone Patient Profile endpoints remain separate future work.

## Protected Routes

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/timeline` | GET | `ViewTimeline` | `View` | `TimelineEvent` |
| `/api/health-core/patients/{patientId}/timeline` | POST | `CreateTimelineEvent` | `Create` | `TimelineEvent` |
| `/api/health-core/timeline-events/{eventId}` | PUT | `EditTimelineEvent` | `Update` | `TimelineEvent` |
| `/api/health-core/timeline-events/{eventId}` | DELETE | `DeleteTimelineEvent` | `Delete` | `TimelineEvent` |

There is no separate Timeline event detail endpoint yet.

## Timeline Is Not AuditLog

Timeline and AuditLog are separate systems.

Timeline is patient/care-team visible clinical or operational history. It may appear in the patient record UI and can include document events, paraclinical events, care plan events, reminder events, measurement events, and other patient-record history.

AuditLog is security, legal, and compliance accountability. It records who accessed or changed data, when, in which product context, and whether the access succeeded or was denied.

Phase 84F does not expose AuditLog through Timeline and does not replace Timeline with AuditLog.

## Audit Logging

Successful Timeline actions are audited with:

- `AuditActionTypes.View`
- `AuditActionTypes.Create`
- `AuditActionTypes.Update`
- `AuditActionTypes.Delete`

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.TimelineEvent`
- attempted permission
- patient id when available
- timeline event id when available
- request context metadata
- authorization denial reason

## Sensitivity and Visibility

Timeline events have both:

- `Visibility`
- `SensitivityLevel`

Single-event update/delete routes use the event's current `SensitivityLevel`, or the requested sensitivity when updating.

The list endpoint currently applies baseline `ViewTimeline` and preserves the existing `includeInternal` and `eventType` filter behavior. It does not perform section-level or per-event redaction in this phase.

Future improvements may add:

- per-event sensitivity filtering
- visibility-aware product policies
- redaction of internal events for patient-facing contexts
- separate permissions for internal timeline views if needed

## Audit Volume

The Timeline list endpoint may be called by the patient record UI.

For Phase 84F, successful Timeline reads are audited as `View`, following the current protected endpoint pattern. If audit volume becomes high, a future phase may aggregate or summarize frequent successful reads. Denied attempts must always be logged.

## Development Fallback

There is still no real JWT/auth provider.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This keeps the development admin panel usable. It is marked as fallback context and is not production-safe.

## Not Included Yet

- No authorization on standalone Patient Profile endpoints.
- No frontend changes.
- No real production authentication/JWT integration.
- No access grant creation workflow.
- No Timeline-to-AuditLog merge or exposure.

# Admin Panel UI/UX Final Review

Phase 91 reviewed the Health Core admin patient record UI after the security/session/access-grant work.

This phase is intentionally frontend-only. It does not change backend endpoints, permissions, schemas, migrations, AI behavior, public patient login, service-token issuing, or consumer app work.

## Reviewed Areas

- patient record shell and navigation
- overview first-view density
- care plan forms, filters, and list states
- medical history tabs and evidence grouping
- documents and paraclinical result states
- measurements and graph area
- reminders and alert filtering
- timeline wording and AuditLog distinction
- Security & Access tab and PatientAccessGrant list/create/revoke flow
- loading, empty, and error states
- Persian RTL layout and mobile/desktop behavior

## Implemented Polish

- Patient record sidebar navigation is scrollable on shorter desktop viewports.
- Paraclinical loading, error, and empty states now use the shared `Notice` component for consistency with the rest of the patient record.
- Timeline now includes a small note clarifying that it is clinical/operational history and not the security AuditLog.
- Security & Access Phase 90B documentation now reflects the reviewed create UI behavior:
  - non-internal products only
  - no `AllPatients`
  - no `InternalAdmin` authorization reason
  - scope follows the selected product-role profile

## Things Left Unchanged

- Care Plan, Reminders, Medical History, Documents, Measurements, and Security & Access forms already open/collapse and were left structurally unchanged.
- Documents and Paraclinical remain grouped under the Medical History evidence tab.
- No new top-level navigation sections were added.
- No PatientAccessGrant create behavior was broadened.

## Backlog

Candidate future UI work:

- Consider splitting Documents and Paraclinical into a top-level Evidence section if the medical-history tab becomes too dense.
- Reduce repeated patient identity information between the shell header and the Overview tab.
- Add a compact first-view priority strip for overdue care-plan items, high-priority reminders, abnormal results, and restricted/sensitive alerts.
- Add clearer active/completed grouping in Care Plan and Reminders if the lists grow.
- Add frontend smoke/E2E coverage for login, session, grant create/revoke, and fallback-off flows.
- Add final CSRF UX/error handling once cookie-authenticated mutation protection is implemented.

# Admin Panel Security UX Polish

Phase 90 improves the admin panel security-aware user experience after the backend admin auth, httpOnly session cookie, server-side helper, browser proxy, fallback-off validation, and PatientAccessGrant admin API work.

This phase does not change backend authorization permissions, health-record DTOs, database schema, Development fallback, public patient login, central SSO, or service-token issuing.

## Session Indicator and Logout

The admin panel now has a small session indicator on core admin pages:

- `/patients`
- `/patients/{id}`
- `/patients/new`

The indicator calls:

`GET /api/admin-auth/me`

When a valid session exists, it shows:

- admin display name when available
- product role
- logout button

Logout calls:

`POST /api/admin-auth/logout`

The logout flow clears the httpOnly cookie through the Next route handler, clears any legacy localStorage token, redirects to `/login`, and refreshes the router state.

## Login Behavior

The login page keeps the same route:

`/login`

Current behavior:

- checks `/api/admin-auth/me` on load
- redirects an already-authenticated admin to `/patients`
- posts credentials to `POST /api/admin-auth/login`
- shows generic Persian invalid-credential messaging
- shows a friendly Persian service-unavailable message when the auth backend cannot be reached
- does not receive or store the backend JWT in browser JavaScript

New logins rely on the httpOnly cookie session. The legacy localStorage helper remains only for cleanup of older transitional tokens.

## 401, 403, and Service Error States

The main patient admin pages now render controlled Persian error states instead of raw backend messages:

- `401`: login-required message with a link to `/login`
- `403`: access-denied message
- `502`: service-unavailable message
- `404` on patient detail: patient-not-found message remains distinct

No aggressive global redirect was added. This keeps local Development fallback usable while still making fallback-off failures understandable.

## PatientAccessGrant UI

The patient record shell now includes a limited `Security and Access` tab.

Implemented:

- list PatientAccessGrant records for the current patient
- create a new grant for a user or service account
- show product code, product role, scope, reason, grantee/service account, active/revoked state, validity window, grant metadata, and revoke metadata
- revoke an active grant with an optional reason

Not implemented in the UI:

- public patient consent flow
- family sharing flow
- emergency access workflow
- service account lifecycle management
- grant-scoped patient directory filtering

The frontend helper calls same-origin proxy routes:

- `GET /api/health-core/patients/{patientId}/access-grants`
- `POST /api/health-core/patients/{patientId}/access-grants`
- `POST /api/health-core/access-grants/{grantId}/revoke`

The browser does not call the backend directly and does not handle the JWT directly.

## Legacy LocalStorage Status

`frontend/src/lib/auth/admin-auth.ts` remains in place as a legacy cleanup path.

Current role:

- clear older stored tokens on `401`
- clear older stored tokens on logout

It is not the primary auth source. Ordinary browser Health Core calls use the cookie-backed Next proxy.

## Production Notes

Still required before production:

- formal logout entry point in any future global admin navigation
- CSRF protection for cookie-authenticated mutations
- admin login rate limiting and lockout
- password reset/provisioning workflow
- token revocation/session-store decision if needed
- fallback-off smoke automation for the login/session/proxy flow
- broader PatientAccessGrant UX policy for safe grant creation

## Phase 90B - PatientAccessGrant Create UI

Phase 90B added a minimal create workflow for PatientAccessGrant inside the patient record security/access tab.

Admins can now:
- View existing access grants for a patient.
- Create a new patient access grant for a user or service account.
- Select product context, product role, access scope, authorization reason, optional expiration, and notes.
- Revoke active grants from the same panel.

The UI does not bypass backend security rules. Grant creation remains controlled by backend permissions, ProductAccessProfiles, PatientAccessGrant validation, audit logging, and existing authorization enforcement.

The create UI intentionally excludes `InternalAdmin`, `AllPatients`, and `InternalAdmin` authorization reason options. Scope is limited to the selected product role's configured profile scope; the backend remains the source of truth for final validation.

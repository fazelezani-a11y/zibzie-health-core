# Server-Side Authenticated API Helper

Phase 87E2d adds a server-side API helper for Next.js server components.

The helper lets server-rendered admin pages call Health Core backend endpoints with the admin JWT stored in the httpOnly cookie created by Phase 87E2c.

## Helper Path

`frontend/src/lib/api/server-client.ts`

## Behavior

The helper:

- runs from server-side code
- reads the `zibzie_admin_access_token` cookie with Next `cookies()`
- builds backend URLs through `healthCoreBackendUrl(...)`
- attaches `Authorization: Bearer <token>` when the cookie exists
- uses `cache: "no-store"` by default for patient/admin data
- returns typed JSON
- throws `ServerApiError` with status for controlled error handling

The token is never exposed to client-side JavaScript by this helper and must not be logged.

## Missing Cookie Behavior

If the cookie is missing, the helper calls the backend without `Authorization`.

This is intentional during the transition because backend Development fallback may still allow local admin flows. When fallback is disabled, the backend returns `401`/`403`, and the server page renders its controlled error state.

## Pages Migrated

The following server-rendered pages now use the helper:

- `/patients`
- `/patients/[id]`

Specifically:

- `/patients` uses `getPatientsServer()`.
- `/patients/[id]` uses `getPatientSummaryServer(id)`.

## Error Handling

Current behavior:

- backend/network unavailable: `502`-style controlled Persian service error
- `401`: controlled login-required Persian message
- `403`: controlled access-denied Persian message
- `404`: patient detail page keeps its existing not-found distinction
- other backend errors: backend message is used when available

No aggressive redirect to `/login` was added in this phase, to avoid breaking Development fallback and existing visual flows.

## What Remains

Not done in this phase:

- client-side modules now use the session/proxy path for Health Core API calls
- localStorage helper remains only as legacy cleanup
- route guard/middleware is not added
- Development fallback is not removed
- logout UI is not added
- session refresh/hardening is not added

Next phase:

- Phase 87E3 should verify fallback-off behavior after cookie-backed server and browser calls are in place.

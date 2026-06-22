# Client API Session Proxy Migration

Phase 87E2e migrates browser-side Health Core API calls away from direct backend bearer-token calls and toward the Next.js BFF/session model.

## Proxy Route

Browser health-record API calls now use:

`/api/health-core/[...path]`

Implemented at:

`frontend/src/app/api/health-core/[...path]/route.ts`

Supported methods:

- `GET`
- `POST`
- `PUT`
- `PATCH`
- `DELETE`

The proxy is restricted to the Health Core API path. It is not an open proxy.

## Proxy Behavior

The proxy:

- reads the `zibzie_admin_access_token` httpOnly cookie
- forwards to backend `/api/health-core/{path}`
- preserves query string
- forwards JSON/body content for mutating requests
- forwards only controlled request headers, such as `Accept` and `Content-Type`
- attaches `Authorization: Bearer <token>` when the cookie exists
- calls backend without `Authorization` when the cookie is missing, preserving Development fallback during transition
- does not log or expose token values
- clears the session cookie when backend returns `401`

Backend unavailable is surfaced as `502` with a safe message.

## Browser API Client Change

`frontend/src/lib/api/client.ts` now calls same-origin paths directly.

Before:

- browser API client resolved `/api/health-core/...` against `NEXT_PUBLIC_API_BASE_URL`
- browser API client attached `Authorization: Bearer` from localStorage when present

After:

- browser API client calls `/api/health-core/...` on the Next app
- Next proxy attaches the backend bearer token from the httpOnly cookie
- browser API client no longer needs to read or attach access tokens
- `401` still clears the legacy localStorage token if one exists

## LocalStorage Status

The localStorage helper remains in the codebase only as a legacy transition cleanup path.

Current behavior:

- `/login` no longer stores the backend JWT in localStorage.
- `/api/admin-auth/login` no longer returns the backend JWT to browser code.
- ordinary browser-side Health Core API calls no longer depend on localStorage.
- `clearAdminAccessToken()` remains so old stored tokens can be removed on `401` or logout.

Future cleanup can remove the helper after confirming no old flows or local diagnostics rely on it.

## Error Handling

Current browser behavior:

- `401`: proxy clears httpOnly cookie; frontend API client clears legacy localStorage token and surfaces an API error
- `403`: frontend API client surfaces access denied through existing component/page handling
- backend unavailable: proxy returns `502`; frontend API client surfaces a service error

No aggressive global redirect is added in this phase.

## Security Benefits

This reduces production exposure because:

- the backend JWT lives in an httpOnly cookie
- browser JavaScript no longer needs to read the token for Health Core calls
- backend endpoint authorization and audit behavior remain unchanged
- the BFF proxy controls which backend paths can be called

## What Remains

Before fallback removal:

- add logout UI
- decide whether to remove the legacy localStorage helper entirely
- add smoke tests for cookie-backed browser mutations
- verify fallback-off behavior locally/staging with the JWT-required smoke and frontend session checklist
- decide CSRF posture for cookie-authenticated mutations

Next phase:

- Phase 87E3 verifies fallback-off behavior outside ordinary Development fallback once server and browser calls work through the cookie-backed session path.

See [Fallback-off verification](fallback-off-verification.md) for the transition checklist.

# Frontend Admin Auth Integration

Phase 87E2 connected the admin frontend to the internal admin auth backend added in Phase 87E1. Later sub-phases added httpOnly cookie route handlers, server-side authenticated fetching, and a browser API proxy.

## Login Route

The frontend login page is:

`/login`

The page posts credentials to the Next session route:

`POST /api/admin-auth/login`

On success, the route handler stores the backend access token in an httpOnly cookie and redirects to `/patients`.

On failure, it shows a generic Persian error message and does not reveal whether the username exists.

## Token Storage

The current production direction is the httpOnly cookie `zibzie_admin_access_token`.

The older localStorage helper remains in:

`frontend/src/lib/auth/admin-auth.ts`

It is now a legacy transition cleanup path, not the primary browser auth mechanism. It can clear older stored tokens on `401` or logout.

The wrapper exposes:

- `getAdminAccessToken()`
- `setAdminAccessToken(...)`
- `clearAdminAccessToken()`
- `getAdminAuthState()`

New logins no longer store the backend JWT in localStorage.

## API Client Behavior

The central frontend API client now calls same-origin `/api/health-core/...` paths. The Next proxy reads the httpOnly cookie and attaches `Authorization: Bearer <token>` to backend requests.

If a protected request returns `401`, the proxy clears the cookie and the API client clears any legacy localStorage token. `403` responses are surfaced through the existing `ApiError` path so pages/components can show their current error states.

## Current Server-Rendered Page Caveat

Some current routes, especially `/patients` and `/patients/[id]`, fetch initial data from server components. Those pages now use the server-side authenticated API helper, which reads the httpOnly cookie server-side.

During the transition, missing cookies can still work in Development through the existing fallback. Production readiness should continue toward:

- httpOnly cookie/session auth that server components can read safely
- a Next route/proxy layer that attaches trusted credentials server-side and browser-side
- fallback-off verification in staging/production-like config

This phase intentionally avoids a broad frontend routing or rendering rewrite.

The recommended server-side session path is documented in [Server-side admin auth and session strategy](server-side-admin-auth-session-strategy.md). The first route-handler layer is documented in [Next admin session route handlers](next-admin-session-route-handlers.md).

The browser API proxy migration is documented in [Client API session proxy migration](client-api-session-proxy-migration.md).

## `/me` Helper

The frontend has a small helper for:

`GET /api/admin-auth/me`

The login page uses it to recognize an already stored valid httpOnly cookie session and redirect to `/patients`.

## 401 and 403 Handling

Current behavior:

- `401` with an existing token clears local token storage.
- Login failures show a generic message.
- `403` remains an access-denied API error and is not globally redirected.

This avoids aggressive redirects that could break the Development fallback transition.

## Development Fallback Transition

Development fallback remains available:

- `HealthCoreAuth:AllowHeaderFallback`
- `HealthCoreAuth:AllowDefaultDevFallback`

The frontend can now use real admin JWTs where available, but local flows without a token are not forcibly redirected yet.

Recommended next steps:

1. Create a seeded/non-Production admin user.
2. Log in through `/login`.
3. Verify browser-side create/list calls go through `/api/health-core/...`.
4. Run fallback-off verification to prove the httpOnly cookie session works without Development fallback.
5. Disable fallback in staging/production after the verification checklist passes.

See [Fallback-off verification](fallback-off-verification.md) for the backend smoke mode and manual frontend checklist.

## Future Hardening

Future phases should add:

- frontend logout UI
- token expiry warning
- refresh/session strategy if needed
- stronger global `401` handling after fallback retirement
- production-safe CSRF/XSS posture for the chosen session model
- automated frontend smoke tests that exercise `/login`, cookie-backed sessions, and JWT-backed requests

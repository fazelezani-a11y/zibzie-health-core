# Frontend Admin Auth Integration

Phase 87E2 connects the admin frontend to the internal admin auth backend added in Phase 87E1. This is a transition step: it adds login and bearer-token support without removing the existing Development fallback.

## Login Route

The frontend login page is:

`/login`

The page posts credentials to:

`POST /api/health-core/auth/admin/login`

On success, it stores the returned access token and redirects to `/patients`.

On failure, it shows a generic Persian error message and does not reveal whether the username exists.

## Token Storage

The current frontend stores the admin access token in `localStorage` through:

`frontend/src/lib/auth/admin-auth.ts`

This is temporary and not the preferred production model. It was chosen because the current frontend does not yet have server-managed session or httpOnly cookie infrastructure.

The wrapper exposes:

- `getAdminAccessToken()`
- `setAdminAccessToken(...)`
- `clearAdminAccessToken()`
- `getAdminAuthState()`

The stored object includes:

- access token
- expiry
- product code
- product role
- safe admin display info

Expired or malformed stored tokens are cleared locally.

## API Client Behavior

The central frontend API client now attaches:

`Authorization: Bearer <token>`

when a browser-side token exists.

If no token exists, the API client continues without an Authorization header. This preserves current local Development behavior while `HealthCoreAuth` fallback is still configured.

If a protected request returns `401` and a token was present, the token is cleared. `403` responses are surfaced through the existing `ApiError` path so pages/components can show their current error states.

## Current Server-Rendered Page Caveat

Some current routes, especially `/patients` and `/patients/[id]`, fetch initial data from server components. The temporary `localStorage` token is only available in the browser, so those server-side fetches cannot use it.

During the transition, these routes can still work in Development through the existing fallback. A production-ready frontend integration should move to one of these models:

- httpOnly cookie/session auth that server components can read safely
- a Next route/proxy layer that attaches trusted credentials server-side
- converting selected data fetches to client-side calls where appropriate

This phase intentionally avoids a broad frontend routing or rendering rewrite.

## `/me` Helper

The frontend has a small helper for:

`GET /api/health-core/auth/admin/me`

The login page uses it to recognize an already stored valid token and redirect to `/patients`.

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
3. Verify browser-side create/list calls send the bearer token.
4. Add a server-compatible session/cookie strategy before disabling fallback for server-rendered admin pages.
5. Disable fallback in staging/production.

## Future Hardening

Future phases should add:

- httpOnly cookie or server-managed session support
- frontend logout UI
- token expiry warning
- refresh/session strategy if needed
- stronger global `401` handling after fallback retirement
- production-safe CSRF/XSS posture for the chosen session model
- security smoke tests that exercise `/login` and JWT-backed requests

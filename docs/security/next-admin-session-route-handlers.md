# Next Admin Session Route Handlers

Phase 87E2c adds Next.js route handlers that sit between the admin frontend and the Health Core backend admin-auth endpoints.

These handlers create the first server-readable admin session path by storing the backend JWT in an httpOnly cookie. They do not yet convert `/patients` or `/patients/[id]` server-side fetching to use the cookie.

## Route Handlers Added

### POST `/api/admin-auth/login`

Frontend-facing login endpoint.

Behavior:

- receives `username` and `password`
- calls backend `POST /api/health-core/auth/admin/login`
- on success, stores backend `accessToken` in an httpOnly cookie
- returns safe admin/session fields to the browser
- returns a generic `401` for invalid credentials
- returns `502` when the backend auth service is unavailable or returns an invalid response

During the transition, the route still returns `accessToken` to the browser so the existing localStorage-based client API bridge keeps working. This is temporary and should be removed after Phase 87E2e migrates browser calls to the session/proxy path.

### GET `/api/admin-auth/me`

Frontend-facing session check.

Behavior:

- reads the httpOnly auth cookie
- returns `401` if the cookie is missing
- calls backend `GET /api/health-core/auth/admin/me` with `Authorization: Bearer <token>`
- clears the cookie if the backend returns `401`
- returns `403` for access denied
- returns safe admin context on success

### POST `/api/admin-auth/logout`

Frontend-facing logout endpoint.

Behavior:

- clears the httpOnly auth cookie
- returns `{ ok: true }`

No backend logout call exists yet.

## Cookie

Cookie name:

`zibzie_admin_access_token`

Cookie options:

- `httpOnly = true`
- `sameSite = lax`
- `secure = true` outside Development
- `path = /`
- `expires` aligned with backend token expiry when available

Token values must not be logged or exposed through ordinary UI state.

## Backend Endpoints Called

- `POST /api/health-core/auth/admin/login`
- `GET /api/health-core/auth/admin/me`

The backend continues to issue and validate JWTs. Health-record endpoints and endpoint permissions are unchanged.

## Frontend Helper Changes

`frontend/src/lib/api/admin-auth.ts` now calls:

- `/api/admin-auth/login`
- `/api/admin-auth/me`
- `/api/admin-auth/logout`

The login page now checks `/api/admin-auth/me` on load so an existing cookie-backed session can redirect to `/patients`.

## Still Not Done

- `/patients` server-side fetches are not converted yet.
- `/patients/[id]` server-side fetches are not converted yet.
- browser-side health-record API calls still use the temporary localStorage token bridge.
- Development fallback is not removed.
- no refresh token/session renewal exists.
- no production CSRF strategy has been implemented yet.

## Next Phase

Phase 87E2d should add a server-side authenticated API helper that reads the cookie and attaches `Authorization: Bearer <token>` for server component calls.

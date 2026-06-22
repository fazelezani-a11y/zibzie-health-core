# Next Admin Session Route Handlers

Phase 87E2c adds Next.js route handlers that sit between the admin frontend and the Health Core backend admin-auth endpoints.

These handlers create the first server-readable admin session path by storing the backend JWT in an httpOnly cookie. Later phases connected `/patients`, `/patients/[id]`, and browser-side Health Core calls to this cookie-backed path.

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

After Phase 87E2e, the route no longer returns the backend `accessToken` to browser code. It sets the httpOnly cookie and returns only safe session/admin metadata.

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

As of Phase 87E4, session route responses are marked `Cache-Control: no-store`.

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

## Phase 90 UI Usage

Phase 90 adds visible admin session/logout UX that calls these route handlers:

- session indicator: `GET /api/admin-auth/me`
- logout button: `POST /api/admin-auth/logout`

The login page continues to use `POST /api/admin-auth/login`, checks existing sessions through `/me`, and does not receive the backend JWT in browser JavaScript.

See [Admin panel security UX polish](admin-panel-security-ux-polish.md).

## Still Not Done

- Development fallback is not removed.
- no refresh token/session renewal exists.
- no production CSRF strategy has been implemented yet.
- no token revocation/session store exists yet.

## Next Phase

Phase 87E2d added a server-side authenticated API helper that reads the cookie and attaches `Authorization: Bearer <token>` for server component calls. See [Server-side authenticated API helper](server-side-authenticated-api-helper.md).

Phase 87E2e added a browser-side `/api/health-core/[...path]` proxy so client components can also use the cookie-backed session path. See [Client API session proxy migration](client-api-session-proxy-migration.md).

Phase 87E4 documents and lightly hardens admin sessions. See [Admin session security hardening](admin-session-security-hardening.md).

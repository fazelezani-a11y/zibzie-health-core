# Server-Side Admin Auth and Session Strategy

Phase 87E2b documents the server-side auth/session strategy needed before Health Core can safely remove Development fallback from the admin frontend.

This phase is documentation-only. It does not implement cookies, route handlers, middleware, backend behavior changes, schema changes, or endpoint permission changes.

## 1. Executive Summary

Phase 87E1 added backend internal admin login and JWT issuing. Phase 87E2 added a `/login` page, temporary localStorage token storage, and browser-side `Authorization: Bearer` attachment.

That is enough for browser-side client components, but it is not enough for current server-rendered admin pages. `/patients` and `/patients/[id]` fetch data from server components, and server components cannot read browser localStorage. A production-ready admin frontend needs a server-readable session mechanism before Development fallback can be removed.

Recommended direction:

- keep backend JWT issuing as the source of trusted claims
- add a Next.js BFF/session layer that stores the backend access token in an httpOnly cookie
- let server components call Health Core with `Authorization: Bearer <token>` read from the cookie
- keep Development fallback until server-side authenticated fetches work

## 2. Current State

Frontend architecture findings:

| Route / module | Current rendering | Auth implication |
| --- | --- | --- |
| `/` | server component redirect to `/patients` | no direct API call |
| `/login` | client component | can use browser localStorage and call backend login |
| `/patients` | server component, `dynamic = "force-dynamic"` | cannot read localStorage token for `getPatients()` |
| `/patients/[id]` | server component, `dynamic = "force-dynamic"` | cannot read localStorage token for `getPatientSummary()` |
| `/patients/new` | client component | browser token can be attached by API client |
| patient record shell/modules | client components | browser token can be attached by API client |

Other relevant details:

- Next.js version is `16.2.9`.
- React version is `19.2.4`.
- The central frontend API helper is `frontend/src/lib/api/client.ts`.
- API base URL uses `NEXT_PUBLIC_API_BASE_URL`, falling back to `http://localhost:5230`.
- The login page stores a temporary browser token through `frontend/src/lib/auth/admin-auth.ts`.
- `next.config.ts` currently has no proxy/session/custom route behavior.
- Development fallback remains configured in the backend and is intentionally not removed yet.

## 3. Problem Statement

The current browser token integration only works after JavaScript runs in the browser. Server components render before browser localStorage is available.

If Development fallback is disabled today:

- `/login` can still authenticate and store a token in the browser.
- browser-side client component calls can send `Authorization: Bearer`.
- `/patients` and `/patients/[id]` cannot send the token during server-side data fetching.
- those pages would receive `401`/`403` unless rewritten or given a server-readable session.

## 4. Why localStorage Is Insufficient for Server Components

localStorage is a browser-only API. It is not available to:

- server components
- route handlers during server execution unless explicitly passed through request headers/cookies
- build-time or server-side rendering contexts

It also has production security drawbacks:

- JavaScript-readable tokens are exposed to XSS.
- There is no reliable server-side logout/session invalidation by default.
- It does not naturally support server-rendered authenticated data fetching.

localStorage can remain a temporary Development bridge, but it should not be the production admin session model.

## 5. Requirements

The target solution should:

- support server-rendered `/patients` and `/patients/[id]`
- avoid exposing access tokens to browser JavaScript in production
- preserve backend endpoint authorization through `IHealthCoreAuthorizationService`
- preserve backend JWT claims: `InternalAdmin`, product role, user id, expiry
- support browser-side mutations from client components
- handle `401` and `403` consistently
- provide logout that clears the session
- keep tokens out of logs
- account for CSRF if cookie-authenticated mutation routes are added
- allow Development fallback to remain until the server-side path is ready

## 6. Options Considered

| Option | Pros | Cons | Fit |
| --- | --- | --- | --- |
| Keep localStorage and convert protected pages to client-side fetching | Simple; minimal backend changes | More client-only UI; worse initial loading; not ideal for server-rendered admin pages; token remains JavaScript-readable | Not recommended as the main path |
| httpOnly cookie session/JWT | Server components can read cookies; avoids localStorage token exposure; better production posture | needs cookie-setting login flow; CSRF/SameSite decisions; logout/session clearing needed | Good target model |
| Next.js BFF/proxy route handlers | browser talks to Next route handlers; Next stores/reads httpOnly cookie; backend remains Bearer JWT; works for server components | more frontend/server infrastructure; careful error handling and deployment config needed | Recommended current path |
| Backend sets auth cookie directly | centralizes auth in backend | cross-origin/domain/cookie setup may be harder; less flexible with Next deployment | Possible later, not preferred now |

## 7. Recommended Approach

Use a Next.js BFF/session layer:

1. Keep backend `/api/health-core/auth/admin/login` returning a bearer token.
2. Add Next route handlers under the frontend, such as `/api/admin-auth/login`, `/api/admin-auth/me`, and `/api/admin-auth/logout`.
3. The Next login route handler calls the backend login endpoint.
4. The Next route handler stores the backend access token in an httpOnly cookie.
5. Server components read the cookie and call the backend with `Authorization: Bearer <token>`.
6. Browser-side client components either call Next route handlers/proxy endpoints or use a session-aware API helper.

This keeps Health Core backend authorization unchanged while giving the frontend a production-suitable session boundary.

## 8. Target Flow

### Login

1. User submits username/password from `/login`.
2. Frontend posts to a Next route handler, for example `POST /api/admin-auth/login`.
3. Next route handler posts to backend `POST /api/health-core/auth/admin/login`.
4. Backend returns JWT with `product_code = InternalAdmin` and `product_role`.
5. Next stores the token in an httpOnly secure SameSite cookie.
6. Browser redirects to `/patients`.

### Server-Side Data Fetching

1. Server component reads the auth cookie.
2. Server component uses a server-only API helper to call Health Core.
3. Helper attaches `Authorization: Bearer <token>`.
4. Backend request context reads JWT claims.
5. Backend authorization service continues permission checks.
6. `401` redirects to `/login`; `403` renders access denied.

### Client-Side Data Fetching

1. Client component calls either a Next proxy route or a session-aware frontend helper.
2. The proxy/helper attaches backend credentials without exposing them to page code.
3. `401` clears session and redirects to login.
4. `403` shows access denied.

### Logout

1. User calls `POST /api/admin-auth/logout`.
2. Next route handler clears the httpOnly cookie.
3. Browser redirects to `/login`.

## 9. Implementation Phases

### 87E2c: Next Admin Session Route Handlers

- Add `POST /api/admin-auth/login`.
- Add `GET /api/admin-auth/me`.
- Add `POST /api/admin-auth/logout`.
- Route handlers call backend admin auth endpoints.
- Route handlers set/clear httpOnly cookies.
- No protected page conversion yet.

### 87E2d: Server-Side Authenticated API Helper

- Add a server-only API helper that reads the auth cookie.
- Attach backend bearer token in server component fetches.
- Use it in `/patients` and `/patients/[id]`.
- Keep Development fallback as temporary backup.

### 87E2e: Client API Migration to Session/Proxy

- Move browser-side calls away from localStorage bearer tokens.
- Use Next route handlers/proxy or session-aware helper.
- Add frontend logout.
- Centralize `401` and `403` handling.

### 87E3: Disable Fallback Outside Development/Test

- Disable fallback only after server-side session works.
- Keep local-only diagnostics if needed.
- Add JWT/session-backed smoke tests.

## 10. Cookie and Session Security Requirements

Cookies should be:

- `httpOnly`
- `Secure` outside Development
- `SameSite=Lax` or `SameSite=Strict`, depending deployment and navigation needs
- short-lived, aligned with backend access token lifetime
- cleared on logout

Additional requirements:

- do not log cookie values or tokens
- avoid localStorage in production
- consider CSRF for cookie-authenticated mutations
- align cookie domain/path with frontend deployment
- ensure HTTPS and proxy headers are correct in staging/production
- decide whether token refresh is needed before access-token expiry

## 11. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| XSS exposes localStorage token | Move production auth to httpOnly cookies |
| CSRF with cookie-authenticated mutation routes | SameSite policy, CSRF token if needed, origin checks |
| Server components keep using unauthenticated fetches | Add server-only API helper and migrate patient pages |
| Token leaks in logs | never log cookie/token values; scrub request headers |
| Cross-origin cookie problems | prefer same-site Next BFF; document deployment domain rules |
| Premature fallback removal breaks admin panel | remove fallback only after server-side authenticated fetches pass smoke tests |
| Expired backend JWT breaks SSR | handle `401` by redirecting to `/login`; consider refresh later |

## 12. Transition Plan from Development Fallback

1. Keep backend Development fallback enabled for local use.
2. Implement Next session route handlers.
3. Migrate `/login` to store the token in an httpOnly cookie through Next.
4. Add server-side API helper and migrate `/patients` and `/patients/[id]`.
5. Migrate client component calls away from localStorage.
6. Add logout.
7. Add smoke tests for login, SSR patient list, patient detail, and client-side mutations.
8. Disable fallback in staging.
9. Disable fallback in production.

## 13. Backend Implications

Preferred Next BFF approach requires no immediate backend behavior changes:

- backend admin login can keep returning bearer tokens
- protected health-record endpoints can keep requiring bearer JWT/context
- endpoint permissions do not need to change
- backend does not need to issue cookies
- backend CORS/cookie settings may not need changes if browser talks to Next routes

Backend changes may be needed later only if the direct backend-cookie approach is chosen or if refresh/session revocation is implemented in Health Core.

## 14. Open Questions

- Will frontend and backend share a same-site deployment domain?
- Should the frontend use Next route handlers for all Health Core API calls or only auth/session?
- Should access tokens be refreshed, or should admins simply re-login after expiry?
- What session lifetime is acceptable for internal admins?
- What CSRF protection level is required for cookie-authenticated admin mutations?
- Should server components redirect to `/login` on `401`, or render a login-required state?
- Should `SuperAdmin` require MFA before production?
- How should logout and token revocation be audited if refresh/session state is added?

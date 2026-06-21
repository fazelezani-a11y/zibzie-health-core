export const ADMIN_SESSION_COOKIE_NAME = "zibzie_admin_access_token";

const DEFAULT_BACKEND_BASE_URL = "http://localhost:5230";

export function healthCoreBackendUrl(path: string) {
  const baseUrl = (
    process.env.HEALTH_CORE_API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL ??
    DEFAULT_BACKEND_BASE_URL
  ).replace(/\/$/, "");

  return `${baseUrl}${path.startsWith("/") ? path : `/${path}`}`;
}

export function adminSessionCookieOptions(expiresAt?: string | null) {
  const options: {
    httpOnly: true;
    sameSite: "lax";
    secure: boolean;
    path: "/";
    expires?: Date;
  } = {
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV !== "development",
    path: "/",
  };

  if (expiresAt) {
    const expires = new Date(expiresAt);

    if (!Number.isNaN(expires.getTime())) {
      options.expires = expires;
    }
  }

  return options;
}

export function clearAdminSessionCookieOptions() {
  return {
    ...adminSessionCookieOptions(),
    maxAge: 0,
  };
}

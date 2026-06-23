import { NextResponse, type NextRequest } from "next/server";
import {
  ADMIN_SESSION_COOKIE_NAME,
  clearAdminSessionCookieOptions,
  healthCoreBackendUrl,
} from "@/lib/auth/admin-session";
import { withSensitiveRouteHeaders } from "@/lib/auth/route-security";

function withNoStore(response: NextResponse) {
  return withSensitiveRouteHeaders(response);
}

export async function GET(request: NextRequest) {
  const accessToken = request.cookies.get(ADMIN_SESSION_COOKIE_NAME)?.value;

  if (!accessToken) {
    return withNoStore(
      NextResponse.json(
        { message: "Authentication is required." },
        { status: 401 },
      ),
    );
  }

  let backendResponse: Response;

  try {
    backendResponse = await fetch(
      healthCoreBackendUrl("/api/health-core/auth/admin/me"),
      {
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        cache: "no-store",
      },
    );
  } catch {
    return withNoStore(
      NextResponse.json(
        { message: "Authentication service is unavailable." },
        { status: 502 },
      ),
    );
  }

  if (backendResponse.status === 401) {
    const response = withNoStore(
      NextResponse.json(
        { message: "Authentication is required." },
        { status: 401 },
      ),
    );
    response.cookies.set(
      ADMIN_SESSION_COOKIE_NAME,
      "",
      clearAdminSessionCookieOptions(),
    );

    return response;
  }

  if (backendResponse.status === 403) {
    return withNoStore(
      NextResponse.json({ message: "Access denied." }, { status: 403 }),
    );
  }

  if (!backendResponse.ok) {
    return withNoStore(
      NextResponse.json(
        { message: "Authentication service is unavailable." },
        { status: 502 },
      ),
    );
  }

  const body = await backendResponse.json();

  return withNoStore(NextResponse.json(body));
}

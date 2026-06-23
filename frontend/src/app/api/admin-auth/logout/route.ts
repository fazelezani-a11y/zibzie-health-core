import { NextResponse, type NextRequest } from "next/server";
import {
  ADMIN_SESSION_COOKIE_NAME,
  clearAdminSessionCookieOptions,
} from "@/lib/auth/admin-session";
import {
  crossSiteMutationResponse,
  shouldRejectCrossSiteMutation,
  withSensitiveRouteHeaders,
} from "@/lib/auth/route-security";

export async function POST(request: NextRequest) {
  if (shouldRejectCrossSiteMutation(request)) {
    return crossSiteMutationResponse();
  }

  const response = NextResponse.json({ ok: true });

  response.cookies.set(
    ADMIN_SESSION_COOKIE_NAME,
    "",
    clearAdminSessionCookieOptions(),
  );

  return withSensitiveRouteHeaders(response);
}

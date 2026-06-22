import { NextResponse } from "next/server";
import {
  ADMIN_SESSION_COOKIE_NAME,
  clearAdminSessionCookieOptions,
} from "@/lib/auth/admin-session";

export async function POST() {
  const response = NextResponse.json({ ok: true });

  response.headers.set("Cache-Control", "no-store");
  response.cookies.set(
    ADMIN_SESSION_COOKIE_NAME,
    "",
    clearAdminSessionCookieOptions(),
  );

  return response;
}

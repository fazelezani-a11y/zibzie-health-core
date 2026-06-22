import { NextResponse, type NextRequest } from "next/server";
import {
  ADMIN_SESSION_COOKIE_NAME,
  adminSessionCookieOptions,
  healthCoreBackendUrl,
} from "@/lib/auth/admin-session";

type BackendAdminLoginResponse = {
  accessToken?: string;
  tokenType?: string;
  expiresAt?: string;
  productCode?: string;
  productRole?: string;
  admin?: {
    id?: string;
    username?: string;
    displayName?: string | null;
    productRole?: string;
  };
};

function withNoStore(response: NextResponse) {
  response.headers.set("Cache-Control", "no-store");
  return response;
}

const invalidLoginResponse = () =>
  withNoStore(
    NextResponse.json(
      { message: "Invalid username or password." },
      { status: 401 },
    ),
  );

export async function POST(request: NextRequest) {
  let credentials: { username?: string; password?: string };

  try {
    credentials = (await request.json()) as {
      username?: string;
      password?: string;
    };
  } catch {
    return invalidLoginResponse();
  }

  if (!credentials.username?.trim() || !credentials.password) {
    return invalidLoginResponse();
  }

  let backendResponse: Response;

  try {
    backendResponse = await fetch(
      healthCoreBackendUrl("/api/health-core/auth/admin/login"),
      {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          username: credentials.username.trim(),
          password: credentials.password,
        }),
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

  if (backendResponse.status === 401 || backendResponse.status === 403) {
    return invalidLoginResponse();
  }

  if (!backendResponse.ok) {
    return withNoStore(
      NextResponse.json(
        { message: "Authentication service is unavailable." },
        { status: 502 },
      ),
    );
  }

  let body: BackendAdminLoginResponse;

  try {
    body = (await backendResponse.json()) as BackendAdminLoginResponse;
  } catch {
    return withNoStore(
      NextResponse.json(
        { message: "Authentication service returned an invalid response." },
        { status: 502 },
      ),
    );
  }

  if (!body.accessToken) {
    return withNoStore(
      NextResponse.json(
        { message: "Authentication service returned an invalid response." },
        { status: 502 },
      ),
    );
  }

  const response = NextResponse.json({
    expiresAt: body.expiresAt ?? null,
    productCode: body.productCode ?? "InternalAdmin",
    productRole: body.productRole ?? "",
    admin: {
      id: body.admin?.id ?? "",
      username: body.admin?.username ?? "",
      displayName: body.admin?.displayName ?? null,
      productRole: body.admin?.productRole ?? body.productRole ?? "",
    },
  });

  response.cookies.set(
    ADMIN_SESSION_COOKIE_NAME,
    body.accessToken,
    adminSessionCookieOptions(body.expiresAt),
  );

  return withNoStore(response);
}

import { NextResponse, type NextRequest } from "next/server";
import {
  ADMIN_SESSION_COOKIE_NAME,
  clearAdminSessionCookieOptions,
  healthCoreBackendUrl,
} from "@/lib/auth/admin-session";

type ProxyContext = {
  params: Promise<{
    path: string[];
  }>;
};

const supportedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE"];

async function proxyHealthCoreRequest(
  request: NextRequest,
  context: ProxyContext,
) {
  if (!supportedMethods.includes(request.method)) {
    return NextResponse.json(
      { message: "Method is not supported." },
      { status: 405 },
    );
  }

  const { path } = await context.params;
  const encodedPath = path.map((segment) => encodeURIComponent(segment)).join("/");
  const backendPath = `/api/health-core/${encodedPath}${request.nextUrl.search}`;
  const accessToken = request.cookies.get(ADMIN_SESSION_COOKIE_NAME)?.value;
  const headers = new Headers();
  const accept = request.headers.get("accept");
  const contentType = request.headers.get("content-type");

  headers.set("Accept", accept || "application/json");

  if (contentType) {
    headers.set("Content-Type", contentType);
  }

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  let body: BodyInit | undefined;

  if (request.method !== "GET" && request.method !== "HEAD") {
    body = await request.arrayBuffer();
  }

  let backendResponse: Response;

  try {
    backendResponse = await fetch(healthCoreBackendUrl(backendPath), {
      method: request.method,
      headers,
      body,
      cache: "no-store",
    });
  } catch {
    return NextResponse.json(
      { message: "Health Core service is unavailable." },
      { status: 502 },
    );
  }

  const responseHeaders = new Headers();
  const responseContentType = backendResponse.headers.get("content-type");

  if (responseContentType) {
    responseHeaders.set("Content-Type", responseContentType);
  }

  const responseBody = await backendResponse.arrayBuffer();
  const response = new NextResponse(responseBody, {
    status: backendResponse.status,
    headers: responseHeaders,
  });

  if (backendResponse.status === 401) {
    response.cookies.set(
      ADMIN_SESSION_COOKIE_NAME,
      "",
      clearAdminSessionCookieOptions(),
    );
  }

  return response;
}

export async function GET(request: NextRequest, context: ProxyContext) {
  return proxyHealthCoreRequest(request, context);
}

export async function POST(request: NextRequest, context: ProxyContext) {
  return proxyHealthCoreRequest(request, context);
}

export async function PUT(request: NextRequest, context: ProxyContext) {
  return proxyHealthCoreRequest(request, context);
}

export async function PATCH(request: NextRequest, context: ProxyContext) {
  return proxyHealthCoreRequest(request, context);
}

export async function DELETE(request: NextRequest, context: ProxyContext) {
  return proxyHealthCoreRequest(request, context);
}

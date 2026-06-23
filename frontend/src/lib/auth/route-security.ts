import { NextResponse, type NextRequest } from "next/server";

const mutationMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

export function withSensitiveRouteHeaders(response: NextResponse) {
  response.headers.set("Cache-Control", "no-store");
  response.headers.set("Pragma", "no-cache");
  response.headers.set("Expires", "0");
  response.headers.set("X-Content-Type-Options", "nosniff");
  response.headers.set("Referrer-Policy", "strict-origin-when-cross-origin");
  response.headers.set("X-Frame-Options", "DENY");

  return response;
}

export function shouldRejectCrossSiteMutation(request: NextRequest) {
  if (!mutationMethods.has(request.method.toUpperCase())) {
    return false;
  }

  const origin = request.headers.get("origin");

  if (origin) {
    try {
      return new URL(origin).origin !== request.nextUrl.origin;
    } catch {
      return true;
    }
  }

  return request.headers.get("sec-fetch-site") === "cross-site";
}

export function crossSiteMutationResponse() {
  return withSensitiveRouteHeaders(
    NextResponse.json(
      { message: "Request origin is not allowed." },
      { status: 403 },
    ),
  );
}

import { clearAdminAccessToken } from "../auth/admin-auth";
import { ApiError } from "./client";

export type AdminLoginInput = {
  username: string;
  password: string;
};

export type AdminLoginResponse = {
  expiresAt: string | null;
  productCode: string;
  productRole: string;
  admin: {
    id: string;
    username: string;
    displayName: string | null;
    productRole: string;
  };
};

export type AdminMeResponse = {
  userId: string;
  productCode: string;
  productRole: string;
  displayName: string | null;
};

async function requestSessionJson<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const headers = new Headers(init?.headers);

  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json");
  }

  const response = await fetch(path, {
    ...init,
    credentials: "same-origin",
    headers,
  });

  if (!response.ok) {
    if (response.status === 401) {
      clearAdminAccessToken();
    }

    let message = `Request failed with status ${response.status}.`;

    try {
      const body = (await response.json()) as { message?: string };

      if (body.message) {
        message = body.message;
      }
    } catch {
      // Keep the default message when the route handler does not return JSON.
    }

    throw new ApiError(message, response.status);
  }

  return response.json();
}

export async function loginAdmin(
  input: AdminLoginInput,
): Promise<AdminLoginResponse> {
  return requestSessionJson<AdminLoginResponse>("/api/admin-auth/login", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      username: input.username.trim(),
      password: input.password,
    }),
  });
}

export async function getCurrentAdmin(): Promise<AdminMeResponse> {
  return requestSessionJson<AdminMeResponse>("/api/admin-auth/me", {
    cache: "no-store",
  });
}

export async function logoutAdmin(): Promise<void> {
  try {
    await requestSessionJson<{ ok: boolean }>("/api/admin-auth/logout", {
      method: "POST",
    });
  } finally {
    clearAdminAccessToken();
  }
}

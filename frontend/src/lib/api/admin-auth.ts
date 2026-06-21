import { requestJson } from "./client";

export type AdminLoginInput = {
  username: string;
  password: string;
};

export type AdminLoginResponse = {
  accessToken: string;
  tokenType: "Bearer";
  expiresAt: string;
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

export async function loginAdmin(
  input: AdminLoginInput,
): Promise<AdminLoginResponse> {
  return requestJson<AdminLoginResponse>("/api/health-core/auth/admin/login", {
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
  return requestJson<AdminMeResponse>("/api/health-core/auth/admin/me", {
    cache: "no-store",
  });
}

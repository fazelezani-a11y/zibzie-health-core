import { cookies } from "next/headers";
import {
  ADMIN_SESSION_COOKIE_NAME,
  healthCoreBackendUrl,
} from "../auth/admin-session";
import type { PatientListItem, PatientSummary } from "./patients";

export class ServerApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
  ) {
    super(message);
    this.name = "ServerApiError";
  }
}

async function adminAccessTokenFromCookie() {
  const cookieStore = await cookies();

  return cookieStore.get(ADMIN_SESSION_COOKIE_NAME)?.value ?? null;
}

export async function requestServerJson<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const accessToken = await adminAccessTokenFromCookie();
  const headers = new Headers(init?.headers);

  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json");
  }

  if (accessToken && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  let response: Response;

  try {
    response = await fetch(healthCoreBackendUrl(path), {
      cache: "no-store",
      ...init,
      headers,
    });
  } catch {
    throw new ServerApiError(
      "ارتباط با سرویس پرونده سلامت برقرار نشد.",
      502,
    );
  }

  if (!response.ok) {
    let message = `درخواست ناموفق بود. کد خطا: ${response.status}`;

    if (response.status === 401) {
      message = "برای مشاهده این بخش ابتدا وارد شوید.";
    } else if (response.status === 403) {
      message = "دسترسی شما به این بخش مجاز نیست.";
    }

    try {
      const body = (await response.json()) as { message?: string };

      if (body.message && response.status !== 401 && response.status !== 403) {
        message = body.message;
      }
    } catch {
      // Keep the controlled message when the backend does not return JSON.
    }

    throw new ServerApiError(message, response.status);
  }

  return response.json();
}

export async function getPatientsServer(): Promise<PatientListItem[]> {
  return requestServerJson<PatientListItem[]>("/api/health-core/patients");
}

export async function getPatientSummaryServer(
  id: string,
): Promise<PatientSummary> {
  return requestServerJson<PatientSummary>(
    `/api/health-core/patients/${id}/summary`,
  );
}

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ??
  "http://localhost:5230";

export type PatientListItem = {
  id: string;
  fullName: string;
  birthDate: string | null;
  nationalCode: string | null;
  mobileNumber: string;
  isActive: boolean;
};

function apiUrl(path: string) {
  return `${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`;
}

export async function getPatients(): Promise<PatientListItem[]> {
  const response = await fetch(apiUrl("/api/health-core/patients"), {
    headers: {
      Accept: "application/json",
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`درخواست فهرست بیماران ناموفق بود. کد خطا: ${response.status}`);
  }

  return response.json();
}

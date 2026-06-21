const ADMIN_AUTH_STORAGE_KEY = "zibzie.healthCore.adminAuth";

export type StoredAdminInfo = {
  id: string;
  username: string;
  displayName: string | null;
  productRole: string;
};

export type AdminAuthState = {
  accessToken: string;
  expiresAt: string | null;
  productCode: string;
  productRole: string;
  admin: StoredAdminInfo | null;
};

function canUseBrowserStorage() {
  return typeof window !== "undefined" && Boolean(window.localStorage);
}

function isExpired(expiresAt: string | null | undefined) {
  if (!expiresAt) {
    return false;
  }

  const expires = new Date(expiresAt);

  if (Number.isNaN(expires.getTime())) {
    return true;
  }

  return expires.getTime() <= Date.now();
}

export function getAdminAuthState(): AdminAuthState | null {
  if (!canUseBrowserStorage()) {
    return null;
  }

  const raw = window.localStorage.getItem(ADMIN_AUTH_STORAGE_KEY);

  if (!raw) {
    return null;
  }

  try {
    const state = JSON.parse(raw) as Partial<AdminAuthState>;

    if (!state.accessToken || isExpired(state.expiresAt)) {
      clearAdminAccessToken();
      return null;
    }

    return {
      accessToken: state.accessToken,
      expiresAt: state.expiresAt ?? null,
      productCode: state.productCode ?? "InternalAdmin",
      productRole: state.productRole ?? "",
      admin: state.admin ?? null,
    };
  } catch {
    clearAdminAccessToken();
    return null;
  }
}

export function getAdminAccessToken() {
  return getAdminAuthState()?.accessToken ?? null;
}

export function setAdminAccessToken(state: AdminAuthState) {
  if (!canUseBrowserStorage()) {
    return;
  }

  window.localStorage.setItem(ADMIN_AUTH_STORAGE_KEY, JSON.stringify(state));
}

export function clearAdminAccessToken() {
  if (!canUseBrowserStorage()) {
    return;
  }

  window.localStorage.removeItem(ADMIN_AUTH_STORAGE_KEY);
}

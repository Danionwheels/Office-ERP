export const providerAccessTokenOverrideHeader = "X-SafarSuite-Provider-Access-Token";

const storageKey = "safarsuite.controlDesk.providerAccessSession.v1";
const expirySkewMs = 30_000;

export type StoredProviderAccessSession = {
  accessToken: string;
  tokenType: string;
  actor: string;
  email?: string;
  scopes: string[];
  expiresAtUtc: string;
};

export function getProviderAccessSession(): StoredProviderAccessSession | null {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.localStorage.getItem(storageKey);

  if (raw === null) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as StoredProviderAccessSession;

    if (!isUsableSession(parsed)) {
      clearProviderAccessSession();
      return null;
    }

    return parsed;
  } catch {
    clearProviderAccessSession();
    return null;
  }
}

export function saveProviderAccessSession(session: StoredProviderAccessSession): void {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(storageKey, JSON.stringify(session));
}

export function clearProviderAccessSession(): void {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.removeItem(storageKey);
}

export function getProviderAccessToken(): string | null {
  return getProviderAccessSession()?.accessToken ?? null;
}

function isUsableSession(value: StoredProviderAccessSession): boolean {
  if (
    typeof value.accessToken !== "string"
    || value.accessToken.trim() === ""
    || typeof value.expiresAtUtc !== "string"
    || value.expiresAtUtc.trim() === ""
  ) {
    return false;
  }

  const expiresAt = Date.parse(value.expiresAtUtc);

  return Number.isFinite(expiresAt) && expiresAt - expirySkewMs > Date.now();
}

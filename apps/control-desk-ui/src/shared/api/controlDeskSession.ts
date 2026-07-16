const storageKey = "safarsuite.controlDesk.localOperatorSession.v1";
const expirySkewMs = 30_000;

export type StoredControlDeskSession = {
  accessToken: string;
  tokenType: string;
  actor: string;
  email?: string;
  scopes: string[];
  expiresAtUtc: string;
};

export function getControlDeskSession(): StoredControlDeskSession | null {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.localStorage.getItem(storageKey);

  if (raw === null) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as StoredControlDeskSession;

    if (!isUsableSession(parsed)) {
      clearControlDeskSession();
      return null;
    }

    return parsed;
  } catch {
    clearControlDeskSession();
    return null;
  }
}

export function saveControlDeskSession(session: StoredControlDeskSession): void {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(storageKey, JSON.stringify(session));
}

export function clearControlDeskSession(): void {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.removeItem(storageKey);
}

function isUsableSession(value: StoredControlDeskSession): boolean {
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

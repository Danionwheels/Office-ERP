import { ApiError, type ApiErrorItem } from "./apiError";
import {
  getControlDeskSession,
  invalidateControlDeskSession
} from "./controlDeskSession";
import {
  getProviderAccessToken,
  providerAccessTokenOverrideHeader
} from "./providerAccessSession";

type ApiErrorResponse = {
  statusCode?: number;
  title?: string;
  errors?: ApiErrorItem[];
};

export async function apiRequest<TResponse>(
  path: string,
  init: RequestInit = {}
): Promise<TResponse> {
  const headers = createHeaders(path, init);

  const response = await fetch(path, {
    ...init,
    headers
  });

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

export async function apiDownload(
  path: string,
  fallbackFileName: string
): Promise<void> {
  const response = await fetch(path, { headers: createHeaders(path) });

  if (!response.ok) {
    throw await toApiError(response);
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = getDownloadFileName(response.headers.get("Content-Disposition"), fallbackFileName);
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(objectUrl);
}

function createHeaders(path: string, init: RequestInit = {}): Headers {
  const headers = new Headers(init.headers);

  if (init.body !== undefined && !(init.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  const controlDeskSession = getControlDeskSession();

  if (controlDeskSession !== null) {
    headers.set(
      "Authorization",
      `${controlDeskSession.tokenType} ${controlDeskSession.accessToken}`
    );
  }

  if (
    path.startsWith("/api/v1/control-cloud")
    || path.startsWith("/api/v1/payments/portal-payment-claims")
  ) {
    const providerAccessToken = getProviderAccessToken();

    if (providerAccessToken !== null) {
      headers.set(providerAccessTokenOverrideHeader, providerAccessToken);
    }
  }

  return headers;
}

function getDownloadFileName(contentDisposition: string | null, fallback: string): string {
  const utf8Match = contentDisposition?.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1]);
    } catch {
      return fallback;
    }
  }

  const basicMatch = contentDisposition?.match(/filename="?([^";]+)"?/i);
  return basicMatch?.[1]?.trim() || fallback;
}

async function toApiError(response: Response): Promise<ApiError> {
  if (response.status === 401) {
    invalidateControlDeskSession();
  }

  try {
    const body = (await response.json()) as ApiErrorResponse;
    return new ApiError(
      body.statusCode ?? response.status,
      body.title ?? response.statusText,
      body.errors ?? []
    );
  } catch {
    return new ApiError(response.status, response.statusText);
  }
}

import { ApiError, type ApiErrorItem } from "./apiError";
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
  const headers = new Headers(init.headers);

  headers.set("Content-Type", "application/json");

  if (path.startsWith("/api/v1/control-cloud")) {
    const providerAccessToken = getProviderAccessToken();

    if (providerAccessToken !== null) {
      headers.set(providerAccessTokenOverrideHeader, providerAccessToken);
    }
  }

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

async function toApiError(response: Response): Promise<ApiError> {
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

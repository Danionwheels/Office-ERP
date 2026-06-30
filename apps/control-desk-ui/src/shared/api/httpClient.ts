import { ApiError, type ApiErrorItem } from "./apiError";

type ApiErrorResponse = {
  statusCode?: number;
  title?: string;
  errors?: ApiErrorItem[];
};

export async function apiRequest<TResponse>(
  path: string,
  init: RequestInit = {}
): Promise<TResponse> {
  const response = await fetch(path, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init.headers
    }
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

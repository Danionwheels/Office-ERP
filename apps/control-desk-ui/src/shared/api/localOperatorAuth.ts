import { apiRequest } from "./httpClient";
import type { StoredControlDeskSession } from "./controlDeskSession";

export type LocalOperatorSessionCreateInput = {
  email: string;
  password: string;
  expiresInMinutes?: number;
};

export async function createLocalOperatorSession(
  input: LocalOperatorSessionCreateInput
): Promise<StoredControlDeskSession> {
  return apiRequest<StoredControlDeskSession>("/api/v1/auth/operator-sessions", {
    method: "POST",
    body: JSON.stringify({
      email: input.email.trim(),
      password: input.password,
      expiresInMinutes: input.expiresInMinutes ?? null
    })
  });
}

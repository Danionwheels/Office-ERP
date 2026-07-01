import { apiRequest } from "../../../shared/api/httpClient";
import type { ClientStatement } from "../types/statementTypes";

export async function getClientStatement(clientId: string): Promise<ClientStatement> {
  return apiRequest<ClientStatement>(`/api/v1/clients/${clientId}/statement`);
}

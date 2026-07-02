import { apiRequest } from "../../../shared/api/httpClient";
import type { ControlCloudInstallationStatus } from "../types/controlCloudTypes";

export async function getCloudInstallationStatus(
  clientId: string,
  installationId: string
): Promise<ControlCloudInstallationStatus> {
  return apiRequest<ControlCloudInstallationStatus>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/status`
  );
}

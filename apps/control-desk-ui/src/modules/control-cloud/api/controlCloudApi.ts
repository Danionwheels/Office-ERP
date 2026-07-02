import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ControlCloudAuditEvent,
  ControlCloudInstallationStatus,
  CreateCloudInstallationProvisioningInput,
  LocalServerBootstrapPackage,
  LocalServerDiagnosticReport,
  LocalServerSetupToken,
  QueuedCloudInstallationSupportCommand,
  QueueCloudInstallationSupportCommandInput
} from "../types/controlCloudTypes";

type ControlCloudAuditEventsResponse = {
  events: ControlCloudAuditEvent[];
};

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

export async function createCloudInstallationSetupToken(
  clientId: string,
  installationId: string,
  input: CreateCloudInstallationProvisioningInput
): Promise<LocalServerSetupToken> {
  return apiRequest<LocalServerSetupToken>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/setup-token`,
    {
      method: "POST",
      body: JSON.stringify(toSetupTokenRequest(input))
    }
  );
}

export async function createCloudInstallationBootstrapPackage(
  clientId: string,
  installationId: string,
  input: CreateCloudInstallationProvisioningInput
): Promise<LocalServerBootstrapPackage> {
  return apiRequest<LocalServerBootstrapPackage>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/bootstrap-package`,
    {
      method: "POST",
      body: JSON.stringify({
        ...toSetupTokenRequest(input),
        localServerVersion: input.localServerVersion,
        safarSuiteAppVersion: optionalText(input.safarSuiteAppVersion)
      })
    }
  );
}

export async function listCloudInstallationAuditEvents(
  clientId: string,
  installationId: string,
  take = 50
): Promise<ControlCloudAuditEvent[]> {
  const response = await apiRequest<ControlCloudAuditEventsResponse>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/audit-events?take=${take}`
  );

  return response.events;
}

export async function getLatestCloudInstallationDiagnostics(
  clientId: string,
  installationId: string
): Promise<LocalServerDiagnosticReport> {
  return apiRequest<LocalServerDiagnosticReport>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/diagnostics/latest`
  );
}

export async function queueCloudInstallationSupportCommand(
  clientId: string,
  installationId: string,
  input: QueueCloudInstallationSupportCommandInput
): Promise<QueuedCloudInstallationSupportCommand> {
  return apiRequest<QueuedCloudInstallationSupportCommand>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/support-command`,
    {
      method: "POST",
      body: JSON.stringify({
        commandType: input.commandType,
        reason: input.reason,
        requestedBy: input.requestedBy,
        expiresInHours: input.expiresInHours
      })
    }
  );
}

function toSetupTokenRequest(input: CreateCloudInstallationProvisioningInput) {
  return {
    expiresInHours: input.expiresInHours,
    createdBy: input.createdBy,
    deploymentMode: input.bootstrapMode,
    clientDeploymentMode: input.clientDeploymentMode,
    siteId: optionalText(input.siteId),
    siteRole: optionalText(input.siteRole),
    parentSiteId: optionalText(input.parentSiteId),
    branchCode: optionalText(input.branchCode),
    syncTopologyId: optionalText(input.syncTopologyId)
  };
}

function optionalText(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === "" ? undefined : trimmed;
}

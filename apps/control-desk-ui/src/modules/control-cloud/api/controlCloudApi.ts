import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ControlCloudAuditEvent,
  ControlCloudInstallationStatus,
  CloudOutboxMessage,
  CreateCloudInstallationProvisioningInput,
  RevokeCloudAppActivationIssueInput,
  IssueCloudAppActivationTokenInput,
  IssuedSafarSuiteAppActivationToken,
  LocalServerBootstrapPackage,
  LocalServerBootstrapPackageRegister,
  LocalServerBootstrapPackageSummary,
  LocalServerDiagnosticReport,
  LocalServerSetupToken,
  PublishCloudOutboxMessagesResult,
  ProviderAccessOperator,
  ProviderAccessOperatorCreateInput,
  ProviderAccessOperatorPasswordInput,
  ProviderAccessOperatorRecoveryCodesInput,
  ProviderAccessOperatorRecoveryCodesResult,
  ProviderAccessOperatorScopesInput,
  ProviderAccessOperatorStatusInput,
  ProviderAccessOperatorTotpEnrollmentResult,
  ProviderAccessOperatorTotpInput,
  ProviderAccessPasswordChangeInput,
  ProviderAccessSession,
  ProviderAccessSessionCreateInput,
  QueuedCloudInstallationSupportCommand,
  QueueCloudInstallationSupportCommandInput,
  SafarSuiteAppActivationIssue
} from "../types/controlCloudTypes";

type CloudOutboxMessagesResponse = {
  messages: CloudOutboxMessage[];
};

type ControlCloudAuditEventsResponse = {
  events: ControlCloudAuditEvent[];
};

type SafarSuiteAppActivationIssuesResponse = {
  issues: SafarSuiteAppActivationIssue[];
};

type ProviderAccessOperatorsResponse = {
  operators: ProviderAccessOperator[];
};

export async function listCloudOutboxMessages(
  input: {
    status?: string;
    messageType?: string;
  } = {}
): Promise<CloudOutboxMessage[]> {
  const search = new URLSearchParams();

  if (input.status?.trim()) {
    search.set("status", input.status.trim());
  }

  if (input.messageType?.trim()) {
    search.set("messageType", input.messageType.trim());
  }

  const query = search.toString();
  const response = await apiRequest<CloudOutboxMessagesResponse>(
    `/api/v1/control-cloud/outbox-messages${query === "" ? "" : `?${query}`}`
  );

  return response.messages;
}

export async function publishCloudOutboxMessages(
  batchSize = 20
): Promise<PublishCloudOutboxMessagesResult> {
  const search = new URLSearchParams({
    batchSize: String(batchSize)
  });

  return apiRequest<PublishCloudOutboxMessagesResult>(
    `/api/v1/control-cloud/outbox-messages/publish?${search.toString()}`,
    {
      method: "POST"
    }
  );
}

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

export async function listCloudInstallationBootstrapPackages(
  clientId: string,
  installationId: string,
  take = 20
): Promise<LocalServerBootstrapPackageSummary[]> {
  const response = await apiRequest<LocalServerBootstrapPackageRegister>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/bootstrap-packages?take=${take}`
  );

  return response.packages;
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

export async function issueCloudAppActivationToken(
  clientId: string,
  installationId: string,
  input: IssueCloudAppActivationTokenInput
): Promise<IssuedSafarSuiteAppActivationToken> {
  return apiRequest<IssuedSafarSuiteAppActivationToken>(
    `/api/v1/control-cloud/clients/${clientId}/installations/${encodeURIComponent(
      installationId
    )}/app-activation-token`,
    {
      method: "POST",
      body: JSON.stringify({
        activationRequestId: input.activationRequestId,
        replacesActivationIssueId: input.replacesActivationIssueId,
        serverInstallationId: input.serverInstallationId.trim(),
        fingerprintHash: input.fingerprintHash.trim(),
        serverPublicKey: input.serverPublicKey.trim(),
        requestedBy: input.requestedBy.trim()
      })
    }
  );
}

export async function listCloudAppActivationIssues(
  clientId: string,
  input: {
    installationId?: string;
    appServerInstallationId?: string;
    query?: string;
    take?: number;
  } = {}
): Promise<SafarSuiteAppActivationIssue[]> {
  const search = new URLSearchParams();

  if (input.installationId?.trim()) {
    search.set("installationId", input.installationId.trim());
  }

  if (input.appServerInstallationId?.trim()) {
    search.set("appServerInstallationId", input.appServerInstallationId.trim());
  }

  if (input.query?.trim()) {
    search.set("query", input.query.trim());
  }

  search.set("take", String(input.take ?? 50));

  const response = await apiRequest<SafarSuiteAppActivationIssuesResponse>(
    `/api/v1/control-cloud/clients/${clientId}/app-activation-issues?${search.toString()}`
  );

  return response.issues;
}

export async function revokeCloudAppActivationIssue(
  clientId: string,
  activationIssueId: string,
  input: RevokeCloudAppActivationIssueInput
): Promise<SafarSuiteAppActivationIssue> {
  return apiRequest<SafarSuiteAppActivationIssue>(
    `/api/v1/control-cloud/clients/${clientId}/app-activation-issues/${activationIssueId}/revoke`,
    {
      method: "POST",
      body: JSON.stringify({
        revokedBy: input.revokedBy.trim(),
        reason: input.reason.trim()
      })
    }
  );
}

export async function listProviderAccessOperators(): Promise<ProviderAccessOperator[]> {
  const response = await apiRequest<ProviderAccessOperatorsResponse>(
    "/api/v1/control-cloud/provider-access/operators"
  );

  return response.operators;
}

export async function createProviderAccessOperatorSession(
  input: ProviderAccessSessionCreateInput
): Promise<ProviderAccessSession> {
  return apiRequest<ProviderAccessSession>(
    "/api/v1/control-cloud/provider-access/operator-sessions",
    {
      method: "POST",
      body: JSON.stringify({
        email: input.email.trim(),
        password: input.password,
        scopes: input.scopes,
        expiresInMinutes: input.expiresInMinutes,
        recoveryCode: input.recoveryCode?.trim() || null,
        totpCode: input.totpCode?.trim() || null
      })
    }
  );
}

export async function changeProviderAccessOperatorPassword(
  input: ProviderAccessPasswordChangeInput
): Promise<ProviderAccessOperator> {
  return apiRequest<ProviderAccessOperator>(
    "/api/v1/control-cloud/provider-access/operator-password",
    {
      method: "POST",
      body: JSON.stringify({
        email: input.email.trim(),
        currentPassword: input.currentPassword,
        newPassword: input.newPassword
      })
    }
  );
}

export async function createProviderAccessOperator(
  input: ProviderAccessOperatorCreateInput
): Promise<ProviderAccessOperator> {
  return apiRequest<ProviderAccessOperator>(
    "/api/v1/control-cloud/provider-access/operators",
    {
      method: "POST",
      body: JSON.stringify({
        email: input.email.trim(),
        fullName: input.fullName.trim(),
        password: input.password,
        scopes: input.scopes,
        createdBy: input.createdBy.trim()
      })
    }
  );
}

export async function resetProviderAccessOperatorPassword(
  userId: string,
  input: ProviderAccessOperatorPasswordInput
): Promise<ProviderAccessOperator> {
  return apiRequest<ProviderAccessOperator>(
    `/api/v1/control-cloud/provider-access/operators/${encodeURIComponent(userId)}/password`,
    {
      method: "POST",
      body: JSON.stringify({
        password: input.password,
        updatedBy: input.updatedBy.trim()
      })
    }
  );
}

export async function resetProviderAccessOperatorRecoveryCodes(
  userId: string,
  input: ProviderAccessOperatorRecoveryCodesInput
): Promise<ProviderAccessOperatorRecoveryCodesResult> {
  return apiRequest<ProviderAccessOperatorRecoveryCodesResult>(
    `/api/v1/control-cloud/provider-access/operators/${encodeURIComponent(userId)}/recovery-codes`,
    {
      method: "POST",
      body: JSON.stringify({
        count: input.count,
        updatedBy: input.updatedBy.trim()
      })
    }
  );
}

export async function resetProviderAccessOperatorTotp(
  userId: string,
  input: ProviderAccessOperatorTotpInput
): Promise<ProviderAccessOperatorTotpEnrollmentResult> {
  return apiRequest<ProviderAccessOperatorTotpEnrollmentResult>(
    `/api/v1/control-cloud/provider-access/operators/${encodeURIComponent(userId)}/totp`,
    {
      method: "POST",
      body: JSON.stringify({
        updatedBy: input.updatedBy.trim()
      })
    }
  );
}

export async function updateProviderAccessOperatorScopes(
  userId: string,
  input: ProviderAccessOperatorScopesInput
): Promise<ProviderAccessOperator> {
  return apiRequest<ProviderAccessOperator>(
    `/api/v1/control-cloud/provider-access/operators/${encodeURIComponent(userId)}/scopes`,
    {
      method: "POST",
      body: JSON.stringify({
        scopes: input.scopes,
        updatedBy: input.updatedBy.trim()
      })
    }
  );
}

export async function updateProviderAccessOperatorStatus(
  userId: string,
  input: ProviderAccessOperatorStatusInput
): Promise<ProviderAccessOperator> {
  return apiRequest<ProviderAccessOperator>(
    `/api/v1/control-cloud/provider-access/operators/${encodeURIComponent(userId)}/status`,
    {
      method: "POST",
      body: JSON.stringify({
        status: input.status,
        updatedBy: input.updatedBy.trim()
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

import type {
  CloudAppActivationTokenFormInput,
  CloudOutboxMessage,
  ControlCloudConnectionState,
  IssuedSafarSuiteAppActivationToken
} from "../types/controlCloudTypes";
import type {
  AppActivationRequestJson,
  AppIdentityMapping,
  CloudControlRow,
  CloudControlRowsInput,
  CloudTone
} from "../types/cloudWorkspaceTypes";
import type {
  LocalServerDeploymentProfile,
  SafarSuiteAppActivationIssue
} from "../types/controlCloudTypes";

export function statusClass(value: string): string {
  const normalized = value.trim().toLowerCase();

  if (
    normalized === "active"
    || normalized === "healthy"
    || normalized === "registered"
    || normalized === "ok"
    || normalized === "consumed"
    || normalized === "sent"
  ) {
    return "active";
  }

  if (normalized === "pending") {
    return "pending";
  }

  if (
    normalized === "suspended"
    || normalized === "expired"
    || normalized === "failed"
    || normalized === "failure"
  ) {
    return "suspended";
  }

  return "draft";
}

export function getAppIdentityMapping({
  appActivationValue,
  issuedAppActivation,
  providerInstallationId
}: {
  appActivationValue: CloudAppActivationTokenFormInput;
  issuedAppActivation: IssuedSafarSuiteAppActivationToken | null;
  providerInstallationId: string;
}): AppIdentityMapping {
  const providerId = providerInstallationId.trim();
  const appServerId = appActivationValue.serverInstallationId.trim()
    || issuedAppActivation?.appServerInstallationId
    || "";
  const appFingerprint = appActivationValue.fingerprintHash.trim();
  const replacesActivationIssueId = appActivationValue.replacesActivationIssueId.trim()
    || issuedAppActivation?.replacesActivationIssueId
    || "";

  if (providerId === "") {
    return {
      providerInstallationId: "Not set",
      appServerInstallationId: formatNullableText(appServerId),
      appFingerprint: formatNullableText(appFingerprint),
      status: "Provider missing",
      tone: "warning"
    };
  }

  if (appServerId === "") {
    return {
      providerInstallationId: providerId,
      appServerInstallationId: "Not imported",
      appFingerprint: formatNullableText(appFingerprint),
      status: "Awaiting app identity",
      tone: "neutral"
    };
  }

  if (issuedAppActivation !== null) {
    const providerMatches = equalsIgnoreCase(issuedAppActivation.installationId, providerId);
    const appMatches = equalsIgnoreCase(issuedAppActivation.appServerInstallationId, appServerId);

    return {
      providerInstallationId: providerId,
      appServerInstallationId: appServerId,
      appFingerprint: formatNullableText(appFingerprint),
      status: providerMatches && appMatches
        ? replacesActivationIssueId === "" ? "Issued mapping" : "Issued replacement"
        : "Issued mapping changed",
      tone: providerMatches && appMatches ? "ready" : "warning"
    };
  }

  return {
    providerInstallationId: providerId,
    appServerInstallationId: appServerId,
    appFingerprint: formatNullableText(appFingerprint),
    status: replacesActivationIssueId === "" ? "Ready to issue" : "Ready replacement",
    tone: "ready"
  };
}

export function toAppActivationRequestJson(value: unknown): AppActivationRequestJson {
  const root = asRecord(value, "Activation request JSON must be an object.");
  const source = root.request === undefined || root.request === null
    ? root
    : asRecord(root.request, "Activation request payload must be an object.");

  return {
    activationRequestId: readOptionalString(source, "activationRequestId"),
    serverInstallationId: readRequiredString(source, "serverInstallationId"),
    fingerprintHash: readRequiredString(source, "fingerprintHash"),
    serverPublicKey: readRequiredString(source, "serverPublicKey"),
    requestedBy: readOptionalString(source, "requestedBy")
  };
}

export function getCloudControlRows({
  auditEvents,
  appActivationIssues,
  connectionState,
  commandStatus,
  deploymentProfile,
  deploymentValue,
  diagnosticsReport,
  issuedAppActivation,
  installationId,
  latestEntitlement,
  latestHeartbeat,
  status
}: CloudControlRowsInput): CloudControlRow[] {
  const installationStatus = status === null
    ? installationId.trim() === "" ? "No installation" : "Not loaded"
    : status.installationStatus;
  const pendingCommandCount = commandStatus?.pendingCommandCount ?? 0;
  const diagnosticsErrorCount = diagnosticsReport?.bundle.recentErrors?.length ?? 0;
  const deploymentMode = deploymentProfile?.clientDeploymentMode
    ?? deploymentValue.clientDeploymentMode
    ?? "Not set";
  const siteId = deploymentProfile?.siteId
    ?? deploymentValue.siteId
    ?? "";
  const latestAppActivationAudit = auditEvents.find((auditEvent) =>
    auditEvent.eventType === "AppActivationTokenIssued"
  );
  const latestAppActivationIssue = appActivationIssues[0];

  return [
    {
      key: "cloudLink",
      label: "Cloud link",
      status: formatCloudConnectionStatus(connectionState),
      detail: formatCloudConnectionDetail(connectionState),
      tone: getCloudConnectionTone(connectionState.status)
    },
    {
      key: "installation",
      label: "Installation",
      status: installationStatus,
      detail: status === null
        ? "Refresh status after selecting an installation"
        : `Registered ${formatNullableDateTime(status.registeredAtUtc)}`,
      tone: status === null ? "warning" : getCloudTone(status.installationStatus)
    },
    {
      key: "heartbeat",
      label: "Heartbeat",
      status: latestHeartbeat === null ? "No heartbeat" : latestHeartbeat.heartbeatStatus,
      detail: latestHeartbeat === null
        ? "Local server heartbeat has not been loaded"
        : `${latestHeartbeat.licenseStatus} at ${formatNullableDateTime(latestHeartbeat.receivedAtUtc)}`,
      tone: latestHeartbeat === null ? "warning" : getCloudTone(latestHeartbeat.heartbeatStatus)
    },
    {
      key: "entitlement",
      label: "Entitlement",
      status: latestEntitlement === null ? "Not issued" : `v${latestEntitlement.entitlementVersion}`,
      detail: latestEntitlement === null
        ? "Refresh entitlement after billing is complete"
        : `Paid until ${latestEntitlement.paidUntil}`,
      tone: latestEntitlement === null ? "warning" : "ready"
    },
    {
      key: "appActivation",
      label: "App activation",
      status: issuedAppActivation !== null
        || latestAppActivationIssue !== undefined
        || latestAppActivationAudit !== undefined
        ? latestAppActivationIssue?.status ?? "Issued"
        : "Not issued",
      detail: issuedAppActivation !== null
        ? `${shortIdentifier(issuedAppActivation.installationId)} -> ${shortIdentifier(issuedAppActivation.appServerInstallationId)}, expires ${formatNullableDateTime(issuedAppActivation.expiresAtUtc)}`
        : latestAppActivationIssue !== undefined
          ? `${shortIdentifier(latestAppActivationIssue.installationId)} -> ${shortIdentifier(latestAppActivationIssue.appServerInstallationId)}, expires ${formatNullableDateTime(latestAppActivationIssue.expiresAtUtc)}`
          : latestAppActivationAudit === undefined
            ? "Issue after app identity is available"
            : `Latest issue ${formatNullableDateTime(latestAppActivationAudit.occurredAtUtc)}`,
      tone: issuedAppActivation !== null
        || latestAppActivationIssue !== undefined
        || latestAppActivationAudit !== undefined
        ? "ready"
        : "neutral"
    },
    {
      key: "commands",
      label: "Commands",
      status: `${pendingCommandCount} pending`,
      detail: commandStatus?.latestCommandStatus === null || commandStatus?.latestCommandStatus === undefined
        ? "No support command queued"
        : `${commandStatus.latestCommandStatus} ${commandStatus.latestCommandType ?? "command"}`,
      tone: pendingCommandCount > 0 ? "warning" : "neutral"
    },
    {
      key: "diagnostics",
      label: "Diagnostics",
      status: diagnosticsReport?.status ?? "Not loaded",
      detail: diagnosticsReport === null
        ? "Request or refresh diagnostics from the local server"
        : `${diagnosticsReport.bundle.checks.length} checks, ${diagnosticsErrorCount} recent errors`,
      tone: diagnosticsReport === null
        ? "neutral"
        : diagnosticsErrorCount > 0 ? "warning" : getCloudTone(diagnosticsReport.status)
    },
    {
      key: "deployment",
      label: "Deployment",
      status: deploymentMode,
      detail: siteId.trim() === ""
        ? "Site profile is not set"
        : `${formatNullableText(siteId)} / ${deploymentProfile?.siteRole ?? deploymentValue.siteRole}`,
      tone: deploymentProfile === null && deploymentValue.installationId.trim() === "" ? "warning" : "ready"
    },
    {
      key: "history",
      label: "History",
      status: `${auditEvents.length} events`,
      detail: auditEvents.length === 0
        ? "Refresh cloud installation history"
        : `Latest event ${formatNullableDateTime(auditEvents[0]?.occurredAtUtc)}`,
      tone: auditEvents.length === 0 ? "neutral" : "ready"
    }
  ];
}

export function getCloudTone(value: string | null | undefined): CloudTone {
  if (value === null || value === undefined || value.trim() === "") {
    return "neutral";
  }

  const normalized = value.trim().toLowerCase();

  if (
    normalized === "active"
    || normalized === "healthy"
    || normalized === "registered"
    || normalized === "ok"
    || normalized === "success"
    || normalized === "succeeded"
    || normalized === "approved"
  ) {
    return "ready";
  }

  if (
    normalized === "suspended"
    || normalized === "expired"
    || normalized === "failed"
    || normalized === "failure"
    || normalized === "error"
    || normalized === "unhealthy"
  ) {
    return "warning";
  }

  return "neutral";
}

export function isCloudConnectionBlockingWrites(state: ControlCloudConnectionState): boolean {
  return state.status === "unavailable" || state.status === "notConfigured";
}

export function formatCloudConnectionStatus(state: ControlCloudConnectionState): string {
  switch (state.status) {
    case "checking":
      return "Checking";
    case "connected":
      return "Connected";
    case "unavailable":
      return "Unavailable";
    case "notConfigured":
      return "Not configured";
    default:
      return "Not checked";
  }
}

export function formatCloudConnectionDetail(state: ControlCloudConnectionState): string {
  const checkedAt = state.checkedAtUtc === null
    ? ""
    : ` Checked ${formatNullableDateTime(state.checkedAtUtc)}.`;

  return `${state.detail}${checkedAt}`;
}

export function getCloudConnectionTone(
  status: ControlCloudConnectionState["status"]
): CloudTone {
  if (status === "connected") {
    return "ready";
  }

  if (status === "unavailable" || status === "notConfigured") {
    return "warning";
  }

  return "neutral";
}

export function cloudConnectionStatusClass(state: ControlCloudConnectionState): string {
  switch (state.status) {
    case "connected":
      return "active";
    case "unavailable":
    case "notConfigured":
      return "suspended";
    default:
      return "draft";
  }
}

export function getCloudOutboxCounts(messages: CloudOutboxMessage[]) {
  const now = Date.now();

  return messages.reduce(
    (counts, message) => {
      const status = message.status.trim().toLowerCase();
      const nextAttemptAt = message.nextAttemptAtUtc === null
        ? null
        : Date.parse(message.nextAttemptAtUtc);

      return {
        pending: counts.pending + (status === "pending" ? 1 : 0),
        failed: counts.failed + (status === "failed" ? 1 : 0),
        sent: counts.sent + (status === "sent" ? 1 : 0),
        ready: counts.ready + (
          status === "pending"
          || (status === "failed" && nextAttemptAt !== null && nextAttemptAt <= now)
            ? 1
            : 0
        ),
        attempts: counts.attempts + message.attemptCount
      };
    },
    {
      pending: 0,
      failed: 0,
      sent: 0,
      ready: 0,
      attempts: 0
    }
  );
}

export function formatCloudOutboxType(message: CloudOutboxMessage): string {
  return formatAuditEventType(message.messageType);
}

export function formatCloudOutboxTiming(message: CloudOutboxMessage): string {
  const status = message.status.trim().toLowerCase();

  if (status === "sent") {
    return `Sent ${formatNullableDateTime(message.sentAtUtc)}.`;
  }

  if (status === "failed") {
    return message.nextAttemptAtUtc === null
      ? `Failed ${formatNullableDateTime(message.failedAtUtc)}. No next attempt is scheduled.`
      : `Failed ${formatNullableDateTime(message.failedAtUtc)}. Next attempt ${formatNullableDateTime(message.nextAttemptAtUtc)}.`;
  }

  if (message.lastAttemptedAtUtc !== null) {
    return `Last attempt ${formatNullableDateTime(message.lastAttemptedAtUtc)}.`;
  }

  return `Queued ${formatNullableDateTime(message.occurredAtUtc)}.`;
}

export function shortIdentifier(value: string): string {
  return value.length <= 18 ? value : `${value.slice(0, 8)}...${value.slice(-6)}`;
}

export function isRevokedAppActivationIssue(issue: SafarSuiteAppActivationIssue): boolean {
  return issue.status.trim().toLowerCase() === "revoked";
}

export function formatNullableText(value: string | null | undefined): string {
  return value === null || value === undefined || value.trim() === ""
    ? "Not set"
    : value;
}

export function formatNullableDateTime(value: string | null | undefined): string {
  if (value === null || value === undefined || value.trim() === "") {
    return "Not available";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

export function formatAvailability(value: boolean | null | undefined): string {
  if (value === true) {
    return "Available";
  }

  if (value === false) {
    return "Unavailable";
  }

  return "Not reported";
}

export function formatCheckCode(value: string): string {
  return value
    .split("-")
    .filter((part) => part.trim() !== "")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export function formatAuditEventType(value: string): string {
  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/([A-Z])([A-Z][a-z])/g, "$1 $2");
}

export function formatBranchLabel(profile: LocalServerDeploymentProfile | null): string {
  if (profile === null) {
    return "No site profile";
  }

  if (profile.branchCode !== null && profile.branchCode.trim() !== "") {
    return `Branch ${profile.branchCode}`;
  }

  if (profile.parentSiteId !== null && profile.parentSiteId.trim() !== "") {
    return `Parent ${profile.parentSiteId}`;
  }

  return profile.siteRole;
}

export function formatSyncLabel(profile: LocalServerDeploymentProfile | null): string {
  if (profile === null) {
    return "Sync not loaded";
  }

  return profile.syncTopologyId === null || profile.syncTopologyId.trim() === ""
    ? "No sync topology"
    : profile.syncTopologyId;
}

function equalsIgnoreCase(left: string, right: string): boolean {
  return left.trim().toLowerCase() === right.trim().toLowerCase();
}

function asRecord(value: unknown, message: string): Record<string, unknown> {
  if (typeof value === "object" && value !== null && !Array.isArray(value)) {
    return value as Record<string, unknown>;
  }

  throw new Error(message);
}

function readRequiredString(source: Record<string, unknown>, key: string): string {
  const value = readOptionalString(source, key);

  if (value === undefined) {
    throw new Error(`${key} is required in the activation request JSON.`);
  }

  return value;
}

function readOptionalString(source: Record<string, unknown>, key: string): string | undefined {
  const value = source[key];

  return typeof value === "string" && value.trim() !== "" ? value.trim() : undefined;
}

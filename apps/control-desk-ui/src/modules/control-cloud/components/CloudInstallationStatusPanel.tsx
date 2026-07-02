import {
  Activity,
  Cloud,
  Download,
  History,
  KeyRound,
  MapPin,
  Network,
  PackageCheck,
  RefreshCw,
  Send,
  ServerCog
} from "lucide-react";
import type {
  ClientDeployment,
  ClientDetails,
  ConfigureClientDeploymentInput
} from "../../clients/types/clientTypes";
import type {
  CloudInstallationSupportCommandFormInput,
  ControlCloudAuditEvent,
  ControlCloudInstallationStatus,
  LocalServerBootstrapPackage,
  LocalServerDiagnosticReport,
  LocalServerDeploymentProfile,
  LocalServerSetupToken,
  QueuedCloudInstallationSupportCommand
} from "../types/controlCloudTypes";

type CloudInstallationStatusPanelProps = {
  client: ClientDetails | null;
  installationId: string;
  deployments: ClientDeployment[];
  selectedDeploymentId: string;
  deploymentValue: ConfigureClientDeploymentInput;
  setupTokenHours: string;
  status: ControlCloudInstallationStatus | null;
  setupToken: LocalServerSetupToken | null;
  bootstrapPackage: LocalServerBootstrapPackage | null;
  supportCommandValue: CloudInstallationSupportCommandFormInput;
  queuedSupportCommand: QueuedCloudInstallationSupportCommand | null;
  auditEvents: ControlCloudAuditEvent[];
  diagnosticsReport: LocalServerDiagnosticReport | null;
  isBusy: boolean;
  onInstallationIdChange: (value: string) => void;
  onDeploymentValueChange: (value: ConfigureClientDeploymentInput) => void;
  onSetupTokenHoursChange: (value: string) => void;
  onDeploymentSelect: (clientDeploymentId: string) => void;
  onSaveDeployment: () => Promise<void>;
  onCreateSetupToken: () => Promise<void>;
  onCreateBootstrapPackage: () => Promise<void>;
  onSupportCommandValueChange: (value: CloudInstallationSupportCommandFormInput) => void;
  onQueueSupportCommand: () => Promise<void>;
  onRefreshAuditEvents: () => Promise<void>;
  onRefreshDiagnostics: () => Promise<void>;
  onRefresh: () => Promise<void>;
};

const defaultBundleContentType = "application/vnd.safarsuite.local-server-bootstrap+json";

function downloadBootstrapBundle(bootstrapPackage: LocalServerBootstrapPackage) {
  const bundleJson = JSON.stringify(bootstrapPackage.signedBundle, null, 2);
  const blob = new Blob([bundleJson], {
    type: bootstrapPackage.bundleContentType || defaultBundleContentType
  });
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement("a");
  const fallbackFileName = `${bootstrapPackage.installationId}-bootstrap.json`;

  link.href = objectUrl;
  link.download = bootstrapPackage.bundleFileName.trim() || fallbackFileName;
  link.style.display = "none";
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 0);
}

function downloadDiagnosticsReport(diagnosticsReport: LocalServerDiagnosticReport) {
  const diagnosticsJson = JSON.stringify(diagnosticsReport, null, 2);
  const blob = new Blob([diagnosticsJson], {
    type: "application/json"
  });
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = objectUrl;
  link.download = `safarsuite-diagnostics-${diagnosticsReport.installationId}.json`;
  link.style.display = "none";
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 0);
}

export function CloudInstallationStatusPanel({
  client,
  installationId,
  deployments,
  selectedDeploymentId,
  deploymentValue,
  setupTokenHours,
  status,
  setupToken,
  bootstrapPackage,
  supportCommandValue,
  queuedSupportCommand,
  auditEvents,
  diagnosticsReport,
  isBusy,
  onInstallationIdChange,
  onDeploymentValueChange,
  onSetupTokenHoursChange,
  onDeploymentSelect,
  onSaveDeployment,
  onCreateSetupToken,
  onCreateBootstrapPackage,
  onSupportCommandValueChange,
  onQueueSupportCommand,
  onRefreshAuditEvents,
  onRefreshDiagnostics,
  onRefresh
}: CloudInstallationStatusPanelProps) {
  const latestHeartbeat = status?.latestHeartbeat ?? null;
  const latestEntitlement = status?.latestEntitlement ?? null;
  const commandStatus = status?.commandStatus ?? null;
  const deploymentProfile = status?.deploymentProfile ?? latestHeartbeat?.deploymentProfile ?? null;
  const statusText =
    latestHeartbeat?.licenseStatus ?? status?.installationStatus ?? "Not loaded";

  return (
    <section className="client-panel cloud-status-panel">
      <div className="client-panel-heading">
        <div>
          <span>Control Cloud</span>
          <strong>{statusText}</strong>
        </div>
        {status !== null && (
          <span className={`status-pill ${statusClass(statusText)}`}>
            {statusText}
          </span>
        )}
      </div>

      <div className="entitlement-action-row">
        <label className="cloud-installation-field">
          <span>Saved deployment</span>
          <select
            value={selectedDeploymentId}
            disabled={isBusy || deployments.length === 0}
            onChange={(event) => onDeploymentSelect(event.target.value)}
          >
            <option value="">New deployment</option>
            {deployments.map((deployment) => (
              <option
                value={deployment.clientDeploymentId}
                key={deployment.clientDeploymentId}
              >
                {deployment.displayName}
                {deployment.isPrimary ? " (primary)" : ""}
              </option>
            ))}
          </select>
        </label>
        <label className="cloud-installation-field">
          <span>Installation</span>
          <input
            type="text"
            value={installationId}
            disabled={isBusy}
            maxLength={160}
            onChange={(event) => onInstallationIdChange(event.target.value)}
          />
        </label>
        <button
          className="icon-button"
          type="button"
          disabled={isBusy || client === null || installationId.trim() === ""}
          onClick={onRefresh}
          title="Refresh cloud installation status"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
        <span className="billing-small-fact">
          {client === null ? "Select a client" : client.code}
        </span>
      </div>

      <div className="cloud-deployment-grid">
        <label>
          <span>Name</span>
          <input
            type="text"
            value={deploymentValue.displayName}
            disabled={isBusy}
            maxLength={128}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              displayName: event.target.value
            })}
          />
        </label>
        <label>
          <span>Bootstrap</span>
          <select
            value={deploymentValue.bootstrapMode}
            disabled={isBusy}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              bootstrapMode: event.target.value
            })}
          >
            <option value="OnlineBootstrap">OnlineBootstrap</option>
            <option value="OfflineAssistedBootstrap">OfflineAssistedBootstrap</option>
          </select>
        </label>
        <label>
          <span>Deployment</span>
          <select
            value={deploymentValue.clientDeploymentMode}
            disabled={isBusy}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              clientDeploymentMode: event.target.value
            })}
          >
            <option value="OfflineLocal">OfflineLocal</option>
            <option value="BranchToHqSync">BranchToHqSync</option>
            <option value="CloudSyncMultiBranch">CloudSyncMultiBranch</option>
            <option value="HostedSaas">HostedSaas</option>
          </select>
        </label>
        <label>
          <span>Site</span>
          <input
            type="text"
            value={deploymentValue.siteId}
            disabled={isBusy}
            maxLength={96}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              siteId: event.target.value
            })}
          />
        </label>
        <label>
          <span>Role</span>
          <select
            value={deploymentValue.siteRole}
            disabled={isBusy}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              siteRole: event.target.value
            })}
          >
            <option value="Standalone">Standalone</option>
            <option value="Hq">Hq</option>
            <option value="Branch">Branch</option>
            <option value="Hosted">Hosted</option>
          </select>
        </label>
        <label>
          <span>Parent</span>
          <input
            type="text"
            value={deploymentValue.parentSiteId}
            disabled={isBusy}
            maxLength={96}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              parentSiteId: event.target.value
            })}
          />
        </label>
        <label>
          <span>Branch</span>
          <input
            type="text"
            value={deploymentValue.branchCode}
            disabled={isBusy}
            maxLength={64}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              branchCode: event.target.value
            })}
          />
        </label>
        <label>
          <span>Sync</span>
          <input
            type="text"
            value={deploymentValue.syncTopologyId}
            disabled={isBusy}
            maxLength={96}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              syncTopologyId: event.target.value
            })}
          />
        </label>
        <label>
          <span>Local server</span>
          <input
            type="text"
            value={deploymentValue.localServerVersion}
            disabled={isBusy}
            maxLength={64}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              localServerVersion: event.target.value
            })}
          />
        </label>
        <label>
          <span>SafarSuite</span>
          <input
            type="text"
            value={deploymentValue.safarSuiteAppVersion}
            disabled={isBusy}
            maxLength={64}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              safarSuiteAppVersion: event.target.value
            })}
          />
        </label>
        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={deploymentValue.isPrimary}
            disabled={isBusy}
            onChange={(event) => onDeploymentValueChange({
              ...deploymentValue,
              isPrimary: event.target.checked
            })}
          />
          <span>Primary</span>
        </label>
        <button
          className="primary-button"
          type="button"
          disabled={isBusy || client === null || deploymentValue.installationId.trim() === ""}
          onClick={onSaveDeployment}
        >
          Save deployment
        </button>
      </div>

      <div className="cloud-provisioning-panel">
        <div className="cloud-provisioning-row">
          <label>
            <span>Token hours</span>
            <input
              type="number"
              value={setupTokenHours}
              min={1}
              max={168}
              disabled={isBusy}
              onChange={(event) => onSetupTokenHoursChange(event.target.value)}
            />
          </label>
          <button
            className="icon-button"
            type="button"
            disabled={isBusy || client === null || deploymentValue.installationId.trim() === ""}
            onClick={onCreateSetupToken}
            title="Create setup token"
          >
            <KeyRound size={16} />
            Setup token
          </button>
          <button
            className="icon-button primary"
            type="button"
            disabled={
              isBusy
              || client === null
              || deploymentValue.installationId.trim() === ""
              || deploymentValue.localServerVersion.trim() === ""
            }
            onClick={onCreateBootstrapPackage}
            title="Create bootstrap package"
          >
            <PackageCheck size={16} />
            Bootstrap
          </button>
        </div>

        {setupToken !== null && (
          <div className="cloud-provisioning-result">
            <div className="cloud-provisioning-result-heading">
              <span>Setup token</span>
              <strong>{setupToken.tokenStatus}</strong>
            </div>
            <input readOnly value={setupToken.setupToken} />
            <dl className="cloud-provisioning-facts">
              <div>
                <dt>Expires</dt>
                <dd>{formatNullableDateTime(setupToken.expiresAtUtc)}</dd>
              </div>
              <div>
                <dt>Deployment</dt>
                <dd>{setupToken.deploymentProfile.clientDeploymentMode}</dd>
              </div>
              <div>
                <dt>Site</dt>
                <dd>{setupToken.deploymentProfile.siteId}</dd>
              </div>
            </dl>
          </div>
        )}

        {bootstrapPackage !== null && (
          <div className="cloud-provisioning-result">
            <div className="cloud-provisioning-result-heading">
              <div>
                <span>Bootstrap package</span>
                <strong>{bootstrapPackage.bundleFileName}</strong>
              </div>
              <button
                className="icon-button"
                type="button"
                onClick={() => downloadBootstrapBundle(bootstrapPackage)}
                title="Download signed bootstrap bundle"
              >
                <Download size={16} />
                Download
              </button>
            </div>
            <textarea rows={3} readOnly value={bootstrapPackage.installCommand} />
            <dl className="cloud-provisioning-facts">
              <div>
                <dt>Setup expires</dt>
                <dd>{formatNullableDateTime(bootstrapPackage.setupTokenExpiresAtUtc)}</dd>
              </div>
              <div>
                <dt>Bundle SHA</dt>
                <dd>{bootstrapPackage.bundleSha256}</dd>
              </div>
              <div>
                <dt>Runtime</dt>
                <dd>{bootstrapPackage.runtimePlan?.runtimeMode ?? bootstrapPackage.localServerVersion}</dd>
              </div>
              <div>
                <dt>Signature</dt>
                <dd>{bootstrapPackage.signedBundle.signature.keyId}</dd>
              </div>
            </dl>
          </div>
        )}

        <div className="cloud-command-panel">
          <div className="cloud-audit-heading">
            <div>
              <span>Support command</span>
              <strong>{queuedSupportCommand?.status ?? "Ready"}</strong>
            </div>
            <button
              className="icon-button"
              type="button"
              disabled={
                isBusy
                || client === null
                || deploymentValue.installationId.trim() === ""
                || supportCommandValue.reason.trim() === ""
              }
              onClick={onQueueSupportCommand}
              title="Queue support command"
            >
              <Send size={16} />
              Queue
            </button>
          </div>

          <div className="cloud-command-grid">
            <label>
              <span>Command</span>
              <select
                value={supportCommandValue.commandType}
                disabled={isBusy}
                onChange={(event) => onSupportCommandValueChange({
                  ...supportCommandValue,
                  commandType: event.target.value
                })}
              >
                <option value="request_diagnostics">Request diagnostics</option>
                <option value="refresh_entitlement">Refresh entitlement</option>
              </select>
            </label>
            <label>
              <span>Expires</span>
              <input
                type="number"
                value={supportCommandValue.expiresInHours}
                min={1}
                max={168}
                disabled={isBusy}
                onChange={(event) => onSupportCommandValueChange({
                  ...supportCommandValue,
                  expiresInHours: event.target.value
                })}
              />
            </label>
            <label>
              <span>Requested by</span>
              <input
                type="text"
                value={supportCommandValue.requestedBy}
                maxLength={120}
                disabled={isBusy}
                onChange={(event) => onSupportCommandValueChange({
                  ...supportCommandValue,
                  requestedBy: event.target.value
                })}
              />
            </label>
            <label className="cloud-command-reason">
              <span>Reason</span>
              <input
                type="text"
                value={supportCommandValue.reason}
                maxLength={500}
                disabled={isBusy}
                onChange={(event) => onSupportCommandValueChange({
                  ...supportCommandValue,
                  reason: event.target.value
                })}
              />
            </label>
          </div>

          {queuedSupportCommand !== null && (
            <dl className="cloud-provisioning-facts">
              <div>
                <dt>Version</dt>
                <dd>{queuedSupportCommand.commandVersion}</dd>
              </div>
              <div>
                <dt>Queued</dt>
                <dd>{formatNullableDateTime(queuedSupportCommand.queuedAtUtc)}</dd>
              </div>
              <div>
                <dt>Expires</dt>
                <dd>{formatNullableDateTime(queuedSupportCommand.expiresAtUtc)}</dd>
              </div>
              <div>
                <dt>Signature</dt>
                <dd>{queuedSupportCommand.signatureKeyId}</dd>
              </div>
            </dl>
          )}
        </div>

        <div className="cloud-audit-panel">
          <div className="cloud-audit-heading">
            <div>
              <span>History</span>
              <strong>{auditEvents.length}</strong>
            </div>
            <button
              className="icon-button"
              type="button"
              disabled={isBusy || client === null || deploymentValue.installationId.trim() === ""}
              onClick={onRefreshAuditEvents}
              title="Refresh cloud installation history"
            >
              <History size={16} />
              History
            </button>
          </div>

          {auditEvents.length === 0 ? (
            <div className="client-empty-state">No cloud installation history loaded</div>
          ) : (
            <div className="cloud-audit-list">
              {auditEvents.map((auditEvent) => (
                <article className="cloud-audit-item" key={auditEvent.auditEventId}>
                  <header>
                    <div>
                      <strong>{formatAuditEventType(auditEvent.eventType)}</strong>
                      <span>{auditEvent.actor}</span>
                    </div>
                    <time dateTime={auditEvent.occurredAtUtc}>
                      {formatNullableDateTime(auditEvent.occurredAtUtc)}
                    </time>
                  </header>
                  <p>{auditEvent.detail}</p>
                </article>
              ))}
            </div>
          )}
        </div>

        <div className="cloud-diagnostics-panel">
          <div className="cloud-audit-heading">
            <div>
              <span>Diagnostics</span>
              <strong>{diagnosticsReport?.status ?? "Not loaded"}</strong>
            </div>
            <div className="cloud-diagnostics-actions">
              {diagnosticsReport !== null && (
                <button
                  className="icon-button"
                  type="button"
                  onClick={() => downloadDiagnosticsReport(diagnosticsReport)}
                  title="Download diagnostics JSON"
                >
                  <Download size={16} />
                  Download
                </button>
              )}
              <button
                className="icon-button"
                type="button"
                disabled={isBusy || client === null || deploymentValue.installationId.trim() === ""}
                onClick={onRefreshDiagnostics}
                title="Refresh latest diagnostics"
              >
                <RefreshCw size={16} />
                Diagnostics
              </button>
            </div>
          </div>

          {diagnosticsReport === null ? (
            <div className="client-empty-state">No diagnostics report loaded</div>
          ) : (
            <>
              <dl className="cloud-diagnostics-facts">
                <div>
                  <dt>License</dt>
                  <dd>{diagnosticsReport.licenseStatus}</dd>
                </div>
                <div>
                  <dt>Received</dt>
                  <dd>{formatNullableDateTime(diagnosticsReport.receivedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Generated</dt>
                  <dd>{formatNullableDateTime(diagnosticsReport.generatedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Local server</dt>
                  <dd>{diagnosticsReport.localServerVersion}</dd>
                </div>
                <div>
                  <dt>Runtime</dt>
                  <dd>{diagnosticsReport.bundle.runtime?.runtimeMode ?? "Not reported"}</dd>
                </div>
                <div>
                  <dt>Docker</dt>
                  <dd>{formatAvailability(diagnosticsReport.bundle.runtime?.dockerAvailable)}</dd>
                </div>
                <div>
                  <dt>Services</dt>
                  <dd>{diagnosticsReport.bundle.services?.length ?? 0}</dd>
                </div>
                <div>
                  <dt>Errors</dt>
                  <dd>{diagnosticsReport.bundle.recentErrors?.length ?? 0}</dd>
                </div>
              </dl>

              <div className="cloud-diagnostics-checks">
                {diagnosticsReport.bundle.checks.map((check) => (
                  <article className="cloud-diagnostics-check" key={check.code}>
                    <header>
                      <strong>{formatCheckCode(check.code)}</strong>
                      <span className={`status-pill ${statusClass(check.status)}`}>
                        {check.status}
                      </span>
                    </header>
                    <p>{check.detail}</p>
                  </article>
                ))}
              </div>

              {diagnosticsReport.bundle.services !== null
                && diagnosticsReport.bundle.services.length > 0 && (
                <div className="cloud-diagnostics-list">
                  {diagnosticsReport.bundle.services.slice(0, 6).map((service) => (
                    <article className="cloud-diagnostics-line" key={service.serviceName}>
                      <strong>{service.serviceName}</strong>
                      <span>{service.currentState ?? service.expectedState}</span>
                    </article>
                  ))}
                </div>
              )}

              {diagnosticsReport.bundle.recentErrors !== null
                && diagnosticsReport.bundle.recentErrors.length > 0 && (
                <div className="cloud-diagnostics-list">
                  {diagnosticsReport.bundle.recentErrors.slice(0, 3).map((error) => (
                    <article
                      className="cloud-diagnostics-line warning"
                      key={`${error.source}-${error.message}-${error.occurredAtUtc ?? ""}`}
                    >
                      <strong>{error.severity}</strong>
                      <span>{error.message}</span>
                    </article>
                  ))}
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {status === null ? (
        <div className="client-empty-state entitlement-empty">
          Cloud status not loaded
        </div>
      ) : (
        <>
          <dl className="entitlement-facts cloud-status-facts">
            <div>
              <dt>Installation</dt>
              <dd>{status.installationStatus}</dd>
            </div>
            <div>
              <dt>License</dt>
              <dd>{latestHeartbeat?.licenseStatus ?? "No heartbeat"}</dd>
            </div>
            <div>
              <dt>Heartbeat</dt>
              <dd>{formatNullableDateTime(latestHeartbeat?.receivedAtUtc)}</dd>
            </div>
            <div>
              <dt>Deployment</dt>
              <dd>{deploymentProfile?.clientDeploymentMode ?? "Not loaded"}</dd>
            </div>
            <div>
              <dt>Site</dt>
              <dd>{formatNullableText(deploymentProfile?.siteId)}</dd>
            </div>
            <div>
              <dt>Role</dt>
              <dd>{formatNullableText(deploymentProfile?.siteRole)}</dd>
            </div>
            <div>
              <dt>Paid until</dt>
              <dd>
                {latestHeartbeat?.paidUntil
                  ?? latestEntitlement?.paidUntil
                  ?? "Not issued"}
              </dd>
            </div>
            <div>
              <dt>Entitlement</dt>
              <dd>{status.latestEntitlementVersion}</dd>
            </div>
            <div>
              <dt>Commands</dt>
              <dd>{commandStatus?.pendingCommandCount ?? 0} pending</dd>
            </div>
          </dl>

          <div className="cloud-status-notes">
            <span>
              <Cloud size={15} />
              Last bundle {formatNullableDateTime(status.lastBundleIssuedAtUtc)}
            </span>
            <span>
              <ServerCog size={15} />
              {deploymentProfile?.bootstrapMode ?? "Bootstrap not loaded"}
            </span>
            <span>
              <MapPin size={15} />
              {formatBranchLabel(deploymentProfile)}
            </span>
            <span>
              <Network size={15} />
              {formatSyncLabel(deploymentProfile)}
            </span>
            <span>
              <Activity size={15} />
              {commandStatus?.latestCommandStatus ?? "No command"} command
            </span>
          </div>
        </>
      )}
    </section>
  );
}

function statusClass(value: string): string {
  const normalized = value.trim().toLowerCase();

  if (normalized === "active" || normalized === "healthy" || normalized === "registered" || normalized === "ok") {
    return "active";
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

function formatNullableText(value: string | null | undefined): string {
  return value === null || value === undefined || value.trim() === ""
    ? "Not set"
    : value;
}

function formatNullableDateTime(value: string | null | undefined): string {
  if (value === null || value === undefined || value.trim() === "") {
    return "Not available";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function formatAvailability(value: boolean | null | undefined): string {
  if (value === true) {
    return "Available";
  }

  if (value === false) {
    return "Unavailable";
  }

  return "Not reported";
}

function formatCheckCode(value: string): string {
  return value
    .split("-")
    .filter((part) => part.trim() !== "")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function formatAuditEventType(value: string): string {
  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/([A-Z])([A-Z][a-z])/g, "$1 $2");
}

function formatBranchLabel(profile: LocalServerDeploymentProfile | null): string {
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

function formatSyncLabel(profile: LocalServerDeploymentProfile | null): string {
  if (profile === null) {
    return "Sync not loaded";
  }

  return profile.syncTopologyId === null || profile.syncTopologyId.trim() === ""
    ? "No sync topology"
    : profile.syncTopologyId;
}

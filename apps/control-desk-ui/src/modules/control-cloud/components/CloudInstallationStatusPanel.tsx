import { useRef, useState } from "react";
import {
  Activity,
  Cloud,
  Copy,
  Download,
  FileText,
  History,
  KeyRound,
  MapPin,
  Network,
  PackageCheck,
  RefreshCw,
  Send,
  ServerCog,
  ShieldCheck,
  ShieldOff,
  Upload
} from "lucide-react";
import type { CloudInstallationStatusPanelProps } from "../types/cloudWorkspaceTypes";
import {
  copyTextToClipboard,
  downloadBootstrapArtifact,
  downloadAppActivationImport,
  downloadBootstrapBundle,
  downloadCustomerSetupGuide,
  downloadDiagnosticsReport
} from "../utils/cloudDownloads";
import {
  cloudConnectionStatusClass,
  formatAuditEventType,
  formatAvailability,
  formatBranchLabel,
  formatCheckCode,
  formatCloudConnectionStatus,
  formatCloudOutboxTiming,
  formatCloudOutboxType,
  formatNullableDateTime,
  formatNullableText,
  formatSyncLabel,
  getAppIdentityMapping,
  getCloudControlRows,
  getCloudOutboxCounts,
  isCloudConnectionBlockingWrites,
  isRevokedAppActivationIssue,
  shortIdentifier,
  statusClass,
  toAppActivationRequestJson
} from "../utils/cloudWorkspaceModel";
import { CloudControlBoard } from "./shared/CloudControlBoard";
import type {
  ControlCloudInstallationStatus,
  IssuedSafarSuiteAppActivationToken,
  LocalServerBootstrapPackage,
  LocalServerBootstrapPackageSummary,
  LocalServerDiagnosticReport,
  SafarSuiteAppActivationIssue
} from "../types/controlCloudTypes";

export function CloudInstallationStatusPanel({
  client,
  installationId,
  deployments,
  selectedDeploymentId,
  deploymentValue,
  setupTokenHours,
  connectionState,
  status,
  setupToken,
  bootstrapPackage,
  bootstrapPackages,
  supportCommandValue,
  queuedSupportCommand,
  appActivationValue,
  issuedAppActivation,
  appActivationIssues,
  appActivationIssueSearch,
  appActivationRevocationValue,
  outboxMessages,
  latestOutboxPublish,
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
  onRefreshBootstrapPackages,
  onSupportCommandValueChange,
  onQueueSupportCommand,
  onAppActivationValueChange,
  onIssueAppActivationToken,
  onAppActivationIssueSearchChange,
  onRefreshAppActivationIssues,
  onAppActivationRevocationValueChange,
  onRevokeAppActivationIssue,
  onPrepareReplacementAppActivationIssue,
  onRefreshOutboxMessages,
  onPublishOutboxMessages,
  onRefreshAuditEvents,
  onRefreshDiagnostics,
  onRefresh
}: CloudInstallationStatusPanelProps) {
  const appActivationRequestInputRef = useRef<HTMLInputElement>(null);
  const [appActivationImportError, setAppActivationImportError] = useState("");
  const [installCommandCopyState, setInstallCommandCopyState] = useState<"idle" | "copied" | "failed">("idle");
  const latestHeartbeat = status?.latestHeartbeat ?? null;
  const latestEntitlement = status?.latestEntitlement ?? null;
  const commandStatus = status?.commandStatus ?? null;
  const deploymentProfile = status?.deploymentProfile ?? latestHeartbeat?.deploymentProfile ?? null;
  const cloudConnectionText = formatCloudConnectionStatus(connectionState);
  const cloudWritesBlocked = isCloudConnectionBlockingWrites(connectionState);
  const statusText =
    connectionState.status === "connected" || connectionState.status === "notChecked"
      ? latestHeartbeat?.licenseStatus ?? status?.installationStatus ?? "Not loaded"
      : cloudConnectionText;
  const cloudControlRows = getCloudControlRows({
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
  });
  const setupSteps = getCustomerSetupSteps({
    selectedDeploymentId,
    deploymentValue,
    status,
    bootstrapPackage,
    bootstrapPackages,
    latestHeartbeat,
    latestEntitlement,
    diagnosticsReport,
    issuedAppActivation,
    appActivationIssues
  });
  const completedSetupSteps = setupSteps.filter((step) => step.done).length;
  const warningSetupSteps = setupSteps.filter((step) => step.tone === "warning").length;
  const setupReadinessTone = completedSetupSteps === setupSteps.length
    ? warningSetupSteps > 0 ? "warning" : "ready"
    : "neutral";
  const setupReadinessStatus = completedSetupSteps === setupSteps.length
    ? warningSetupSteps > 0 ? "Review" : "Complete"
    : `${completedSetupSteps}/${setupSteps.length} ready`;
  const outboxCounts = getCloudOutboxCounts(outboxMessages);
  const canIssueAppActivationToken =
    !isBusy
    && !cloudWritesBlocked
    && client !== null
    && deploymentValue.installationId.trim() !== ""
    && appActivationValue.serverInstallationId.trim() !== ""
    && appActivationValue.fingerprintHash.trim() !== ""
    && appActivationValue.serverPublicKey.trim() !== "";
  const canRevokeAppActivationIssue =
    !isBusy
    && !cloudWritesBlocked
    && client !== null
    && appActivationRevocationValue.revokedBy.trim() !== ""
    && appActivationRevocationValue.reason.trim() !== "";
  const appIdentityMapping = getAppIdentityMapping({
    appActivationValue,
    issuedAppActivation,
    providerInstallationId: deploymentValue.installationId.trim() || installationId.trim()
  });

  async function handleImportAppActivationRequest(file: File) {
    setAppActivationImportError("");

    try {
      const request = toAppActivationRequestJson(JSON.parse(await file.text()));
      onAppActivationValueChange({
        ...appActivationValue,
        activationRequestId: request.activationRequestId ?? "",
        serverInstallationId: request.serverInstallationId,
        fingerprintHash: request.fingerprintHash,
        serverPublicKey: request.serverPublicKey,
        requestedBy: request.requestedBy?.trim() || appActivationValue.requestedBy
      });
    } catch (error) {
      setAppActivationImportError(error instanceof Error
        ? error.message
        : "Activation request JSON could not be read.");
    }
  }

  async function handleCopyInstallCommand() {
    if (bootstrapPackage === null) {
      return;
    }

    try {
      await copyTextToClipboard(bootstrapPackage.installCommand);
      setInstallCommandCopyState("copied");
      window.setTimeout(() => setInstallCommandCopyState("idle"), 1800);
    } catch {
      setInstallCommandCopyState("failed");
      window.setTimeout(() => setInstallCommandCopyState("idle"), 2400);
    }
  }

  return (
    <section className="client-panel cloud-status-panel">
      <div className="client-panel-heading">
        <div>
          <span>Control Cloud</span>
          <strong>{statusText}</strong>
        </div>
        <span
          className={`status-pill ${cloudConnectionStatusClass(connectionState)}`}
          title={connectionState.detail}
        >
          {cloudConnectionText}
        </span>
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

      <CloudControlBoard rows={cloudControlRows} />

      <div className={`cloud-setup-readiness ${setupReadinessTone}`}>
        <div className="cloud-audit-heading">
          <div>
            <span>Customer setup</span>
            <strong>{setupReadinessStatus}</strong>
          </div>
          <span className={`status-pill ${setupReadinessTone === "ready" ? "active" : setupReadinessTone === "warning" ? "pending" : "draft"}`}>
            {warningSetupSteps > 0
              ? `${warningSetupSteps} review`
              : `${completedSetupSteps}/${setupSteps.length}`}
          </span>
        </div>
        <div className="cloud-setup-step-grid">
          {setupSteps.map((step) => (
            <article className={`cloud-setup-step ${step.tone}`} key={step.key}>
              <span className={`cloud-setup-step-icon ${step.done ? "ready" : step.tone}`}>
                {step.done ? <ShieldCheck size={15} /> : <ShieldOff size={15} />}
              </span>
              <div>
                <strong>{step.label}</strong>
                <span>{step.detail}</span>
              </div>
              <span className={`status-pill ${step.done ? "active" : step.tone === "warning" ? "pending" : "draft"}`}>
                {step.status}
              </span>
            </article>
          ))}
        </div>
      </div>

      <div className="cloud-audit-panel">
        <div className="cloud-audit-heading">
          <div>
            <span>Local outbox</span>
            <strong>
              {outboxCounts.pending} pending / {outboxCounts.failed} failed
            </strong>
          </div>
          <div className="cloud-diagnostics-actions">
            <button
              className="icon-button"
              type="button"
              disabled={isBusy}
              onClick={onRefreshOutboxMessages}
              title="Refresh local cloud outbox"
            >
              <RefreshCw size={16} />
              Outbox
            </button>
            <button
              className="icon-button primary"
              type="button"
              disabled={isBusy || outboxCounts.ready === 0 || cloudWritesBlocked}
              onClick={onPublishOutboxMessages}
              title="Publish ready local outbox messages to Control Cloud"
            >
              <Send size={16} />
              Publish
            </button>
          </div>
        </div>

        <dl className="cloud-provisioning-facts">
          <div>
            <dt>Ready</dt>
            <dd>{outboxCounts.ready}</dd>
          </div>
          <div>
            <dt>Sent</dt>
            <dd>{outboxCounts.sent}</dd>
          </div>
          <div>
            <dt>Attempts</dt>
            <dd>{outboxCounts.attempts}</dd>
          </div>
          <div>
            <dt>Last publish</dt>
            <dd>
              {latestOutboxPublish === null
                ? "Not run"
                : `${latestOutboxPublish.publishedCount} sent / ${latestOutboxPublish.failedCount} failed`}
            </dd>
          </div>
        </dl>

        {outboxMessages.length === 0 ? (
          <div className="client-empty-state">No local cloud outbox messages loaded</div>
        ) : (
          <div className="cloud-audit-list">
            {outboxMessages.slice(0, 8).map((outboxMessage) => (
              <article
                className="cloud-audit-item"
                key={outboxMessage.cloudOutboxMessageId}
              >
                <header>
                  <div>
                    <strong>{formatCloudOutboxType(outboxMessage)}</strong>
                    <span>
                      {outboxMessage.subjectType} / {shortIdentifier(outboxMessage.subjectId)}
                    </span>
                  </div>
                  <span className={`status-pill ${statusClass(outboxMessage.status)}`}>
                    {outboxMessage.status}
                  </span>
                </header>
                <p>
                  {formatCloudOutboxTiming(outboxMessage)}
                  {outboxMessage.failureReason === null ? "" : ` ${outboxMessage.failureReason}`}
                </p>
              </article>
            ))}
          </div>
        )}
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
            disabled={
              cloudWritesBlocked
              || isBusy
              || client === null
              || deploymentValue.installationId.trim() === ""
            }
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
              cloudWritesBlocked
              || isBusy
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
              <div className="cloud-setup-packet-actions">
                <button
                  className="icon-button"
                  type="button"
                  onClick={handleCopyInstallCommand}
                  title="Copy install command"
                >
                  <Copy size={16} />
                  {installCommandCopyState === "copied"
                    ? "Copied"
                    : installCommandCopyState === "failed" ? "Failed" : "Copy"}
                </button>
                <button
                  className="icon-button"
                  type="button"
                  onClick={() => downloadCustomerSetupGuide(
                    bootstrapPackage,
                    client?.code ?? null)}
                  title="Download customer setup guide"
                >
                  <FileText size={16} />
                  Guide
                </button>
                <button
                  className="icon-button"
                  type="button"
                  onClick={() => downloadBootstrapBundle(bootstrapPackage)}
                  title="Download signed bootstrap bundle"
                >
                  <Download size={16} />
                  Bundle
                </button>
              </div>
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
              <div>
                <dt>Artifacts</dt>
                <dd>{bootstrapPackage.artifacts.length}</dd>
              </div>
            </dl>
            {bootstrapPackage.artifacts.length > 0 && (
              <div className="cloud-setup-artifact-list">
                {bootstrapPackage.artifacts.map((artifact) => (
                  <div
                    className="cloud-setup-artifact"
                    key={`${artifact.artifactType}-${artifact.fileName}`}
                  >
                    <div>
                      <strong>{artifact.fileName}</strong>
                      <span>{artifact.artifactType} / {artifact.targetPath}</span>
                    </div>
                    <span>{formatPackageHash(artifact.sha256)}</span>
                    <button
                      className="icon-button"
                      type="button"
                      onClick={() => downloadBootstrapArtifact(artifact)}
                      title={`Download ${artifact.fileName}`}
                    >
                      <Download size={16} />
                      File
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        <div className="cloud-bootstrap-register">
          <div className="cloud-audit-heading">
            <div>
              <span>Deployment packages</span>
              <strong>{bootstrapPackages.length} loaded</strong>
            </div>
            <button
              className="icon-button"
              type="button"
              disabled={isBusy || client === null || deploymentValue.installationId.trim() === ""}
              onClick={onRefreshBootstrapPackages}
              title="Refresh deployment package register"
            >
              <RefreshCw size={16} />
              Packages
            </button>
          </div>

          {bootstrapPackages.length === 0 ? (
            <div className="client-empty-state">No deployment packages loaded</div>
          ) : (
            <div className="cloud-bootstrap-package-list">
              {bootstrapPackages.slice(0, 6).map((packageSummary) => (
                <article
                  className="cloud-bootstrap-package"
                  key={packageSummary.bootstrapPackageId}
                >
                  <header>
                    <div>
                      <strong>
                        {packageSummary.bundleFileName.trim()
                          || shortIdentifier(packageSummary.bootstrapPackageId)}
                      </strong>
                      <span>
                        {shortIdentifier(packageSummary.bootstrapPackageId)}
                        {" / setup "}
                        {shortIdentifier(packageSummary.setupTokenId)}
                      </span>
                    </div>
                    <span className={`status-pill ${statusClass(packageSummary.packageStatus)}`}>
                      {packageSummary.packageStatus}
                    </span>
                  </header>
                  <dl>
                    <div>
                      <dt>Generated</dt>
                      <dd>{formatNullableDateTime(packageSummary.generatedAtUtc)}</dd>
                    </div>
                    <div>
                      <dt>Expires</dt>
                      <dd>{formatNullableDateTime(packageSummary.setupTokenExpiresAtUtc)}</dd>
                    </div>
                    <div>
                      <dt>Consumed</dt>
                      <dd>{formatPackageConsumption(packageSummary)}</dd>
                    </div>
                    <div>
                      <dt>Runtime</dt>
                      <dd>{formatPackageRuntime(packageSummary)}</dd>
                    </div>
                    <div>
                      <dt>Site</dt>
                      <dd>{formatNullableText(packageSummary.deploymentProfile.siteId)}</dd>
                    </div>
                    <div>
                      <dt>Bundle SHA</dt>
                      <dd>{formatPackageHash(packageSummary.bundleSha256)}</dd>
                    </div>
                  </dl>
                </article>
              ))}
            </div>
          )}
        </div>

        <div className="cloud-command-panel">
          <div className="cloud-audit-heading">
            <div>
              <span>App activation</span>
              <strong>{issuedAppActivation === null ? "Ready" : "Issued"}</strong>
            </div>
            <div className="cloud-diagnostics-actions">
              <button
                className="icon-button"
                type="button"
                disabled={isBusy}
                onClick={() => appActivationRequestInputRef.current?.click()}
                title="Import SafarSuite app activation request JSON"
              >
                <Upload size={16} />
                Import request
              </button>
              <input
                ref={appActivationRequestInputRef}
                className="cloud-hidden-file"
                type="file"
                accept="application/json,.json"
                onChange={(event) => {
                  const file = event.currentTarget.files?.[0] ?? null;
                  event.currentTarget.value = "";

                  if (file !== null) {
                    void handleImportAppActivationRequest(file);
                  }
                }}
              />
              <button
                className="icon-button primary"
                type="button"
                disabled={!canIssueAppActivationToken}
                onClick={onIssueAppActivationToken}
                title="Issue SafarSuite app activation token"
              >
                <ShieldCheck size={16} />
                Issue
              </button>
            </div>
          </div>

          <div className={`cloud-app-identity-map ${appIdentityMapping.tone}`}>
            <div>
              <span>Provider installation</span>
              <strong>{appIdentityMapping.providerInstallationId}</strong>
            </div>
            <div>
              <span>App server</span>
              <strong>{appIdentityMapping.appServerInstallationId}</strong>
            </div>
            <div>
              <span>App fingerprint</span>
              <strong>{appIdentityMapping.appFingerprint}</strong>
            </div>
            <div>
              <span>Mapping</span>
              <strong>{appIdentityMapping.status}</strong>
            </div>
          </div>

          {appActivationImportError !== "" && (
            <div className="cloud-app-activation-error">{appActivationImportError}</div>
          )}

          <div className="cloud-app-activation-grid">
            <label>
              <span>Activation request</span>
              <input
                type="text"
                value={appActivationValue.activationRequestId}
                maxLength={36}
                disabled={isBusy}
                onChange={(event) => onAppActivationValueChange({
                  ...appActivationValue,
                  activationRequestId: event.target.value
                })}
              />
            </label>
            <label>
              <span>Replacing issue</span>
              <input
                type="text"
                value={appActivationValue.replacesActivationIssueId}
                maxLength={36}
                disabled={isBusy}
                onChange={(event) => onAppActivationValueChange({
                  ...appActivationValue,
                  replacesActivationIssueId: event.target.value
                })}
              />
            </label>
            <label>
              <span>App server</span>
              <input
                type="text"
                value={appActivationValue.serverInstallationId}
                maxLength={36}
                disabled={isBusy}
                onChange={(event) => onAppActivationValueChange({
                  ...appActivationValue,
                  serverInstallationId: event.target.value
                })}
              />
            </label>
            <label>
              <span>Fingerprint</span>
              <input
                type="text"
                value={appActivationValue.fingerprintHash}
                maxLength={512}
                disabled={isBusy}
                onChange={(event) => onAppActivationValueChange({
                  ...appActivationValue,
                  fingerprintHash: event.target.value
                })}
              />
            </label>
            <label>
              <span>Requested by</span>
              <input
                type="text"
                value={appActivationValue.requestedBy}
                maxLength={120}
                disabled={isBusy}
                onChange={(event) => onAppActivationValueChange({
                  ...appActivationValue,
                  requestedBy: event.target.value
                })}
              />
            </label>
            <label className="cloud-app-activation-key">
              <span>Public key</span>
              <textarea
                rows={4}
                value={appActivationValue.serverPublicKey}
                maxLength={4096}
                disabled={isBusy}
                onChange={(event) => onAppActivationValueChange({
                  ...appActivationValue,
                  serverPublicKey: event.target.value
                })}
              />
            </label>
          </div>

          {issuedAppActivation !== null && (
            <div className="cloud-provisioning-result">
              <div className="cloud-provisioning-result-heading">
                <div>
                  <span>Activation import</span>
                  <strong>{issuedAppActivation.activationIssueId}</strong>
                </div>
                <button
                  className="icon-button"
                  type="button"
                  onClick={() => downloadAppActivationImport(issuedAppActivation)}
                  title="Download signed app activation import"
                >
                  <Download size={16} />
                  Download
                </button>
              </div>
              <input readOnly value={issuedAppActivation.import.activationToken} />
              <dl className="cloud-provisioning-facts">
                <div>
                  <dt>Provider install</dt>
                  <dd>{issuedAppActivation.installationId}</dd>
                </div>
                <div>
                  <dt>App server</dt>
                  <dd>{issuedAppActivation.appServerInstallationId}</dd>
                </div>
                <div>
                  <dt>Activation request</dt>
                  <dd>{issuedAppActivation.activationRequestId}</dd>
                </div>
                {issuedAppActivation.replacesActivationIssueId !== null && (
                  <div>
                    <dt>Replaces</dt>
                    <dd>{issuedAppActivation.replacesActivationIssueId}</dd>
                  </div>
                )}
                <div>
                  <dt>Entitlement</dt>
                  <dd>{issuedAppActivation.entitlementVersion}</dd>
                </div>
                <div>
                  <dt>Expires</dt>
                  <dd>{formatNullableDateTime(issuedAppActivation.expiresAtUtc)}</dd>
                </div>
                <div>
                  <dt>Signature</dt>
                  <dd>{issuedAppActivation.signingKeyId}</dd>
                </div>
                <div>
                  <dt>Customer</dt>
                  <dd>{issuedAppActivation.import.customerCode}</dd>
                </div>
                <div>
                  <dt>Branch</dt>
                  <dd>{issuedAppActivation.import.branchName}</dd>
                </div>
              </dl>
            </div>
          )}

          <div className="cloud-app-activation-register">
            <div className="cloud-audit-heading">
              <div>
                <span>Activation register</span>
                <strong>{appActivationIssues.length}</strong>
              </div>
              <div className="cloud-diagnostics-actions">
                <label className="cloud-app-activation-search">
                  <span>Search</span>
                  <input
                    type="search"
                    value={appActivationIssueSearch}
                    maxLength={200}
                    disabled={isBusy}
                    onChange={(event) => onAppActivationIssueSearchChange(event.target.value)}
                  />
                </label>
                <label className="cloud-app-activation-search">
                  <span>Revoked by</span>
                  <input
                    type="text"
                    value={appActivationRevocationValue.revokedBy}
                    maxLength={120}
                    disabled={isBusy}
                    onChange={(event) => onAppActivationRevocationValueChange({
                      ...appActivationRevocationValue,
                      revokedBy: event.target.value
                    })}
                  />
                </label>
                <label className="cloud-app-activation-search wide">
                  <span>Reason</span>
                  <input
                    type="text"
                    value={appActivationRevocationValue.reason}
                    maxLength={500}
                    disabled={isBusy}
                    onChange={(event) => onAppActivationRevocationValueChange({
                      ...appActivationRevocationValue,
                      reason: event.target.value
                    })}
                  />
                </label>
                <button
                  className="icon-button"
                  type="button"
                  disabled={isBusy || client === null}
                  onClick={onRefreshAppActivationIssues}
                  title="Refresh app activation register"
                >
                  <RefreshCw size={16} />
                  Register
                </button>
              </div>
            </div>

            {appActivationIssues.length === 0 ? (
              <div className="client-empty-state">No app activation mappings loaded</div>
            ) : (
              <div className="cloud-app-activation-issue-list">
                {appActivationIssues.map((issue) => (
                  <article
                    className={`cloud-app-activation-issue ${isRevokedAppActivationIssue(issue) ? "revoked" : ""}`}
                    key={issue.activationIssueId}
                  >
                    <header>
                      <div>
                        <strong>{shortIdentifier(issue.installationId)} - {shortIdentifier(issue.appServerInstallationId)}</strong>
                        <span>{issue.status} / v{issue.entitlementVersion}</span>
                      </div>
                      <div className="cloud-app-activation-issue-actions">
                        <time dateTime={issue.issuedAtUtc}>
                          {formatNullableDateTime(issue.issuedAtUtc)}
                        </time>
                        {!isRevokedAppActivationIssue(issue) && (
                          <button
                            className="icon-button"
                            type="button"
                            disabled={!canRevokeAppActivationIssue}
                            onClick={() => onRevokeAppActivationIssue(issue.activationIssueId)}
                            title="Revoke app activation mapping"
                          >
                            <ShieldOff size={16} />
                            Revoke
                          </button>
                        )}
                        {isRevokedAppActivationIssue(issue) && (
                          <button
                            className="icon-button"
                            type="button"
                            disabled={isBusy || client === null}
                            onClick={() => onPrepareReplacementAppActivationIssue(issue)}
                            title="Prepare replacement activation mapping"
                          >
                            <RefreshCw size={16} />
                            Replace
                          </button>
                        )}
                      </div>
                    </header>
                    <dl>
                      <div>
                        <dt>Issue</dt>
                        <dd>{issue.activationIssueId}</dd>
                      </div>
                      <div>
                        <dt>Request</dt>
                        <dd>{issue.activationRequestId}</dd>
                      </div>
                      {issue.replacesActivationIssueId !== null && (
                        <div>
                          <dt>Replaces</dt>
                          <dd>{issue.replacesActivationIssueId}</dd>
                        </div>
                      )}
                      <div>
                        <dt>Fingerprint</dt>
                        <dd>{shortIdentifier(issue.fingerprintHash)}</dd>
                      </div>
                      <div>
                        <dt>Expires</dt>
                        <dd>{formatNullableDateTime(issue.expiresAtUtc)}</dd>
                      </div>
                      <div>
                        <dt>Requested by</dt>
                        <dd>{issue.requestedBy}</dd>
                      </div>
                      <div>
                        <dt>Key</dt>
                        <dd>{issue.signingKeyId}</dd>
                      </div>
                      {issue.revokedAtUtc !== null && (
                        <div>
                          <dt>Revoked</dt>
                          <dd>{formatNullableDateTime(issue.revokedAtUtc)}</dd>
                        </div>
                      )}
                      {issue.revokedBy !== null && (
                        <div>
                          <dt>Revoked by</dt>
                          <dd>{issue.revokedBy}</dd>
                        </div>
                      )}
                      {issue.revocationReason !== null && (
                        <div>
                          <dt>Reason</dt>
                          <dd>{issue.revocationReason}</dd>
                        </div>
                      )}
                    </dl>
                  </article>
                ))}
              </div>
            )}
          </div>
        </div>

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
              cloudWritesBlocked
                || isBusy
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

type CustomerSetupStep = {
  key: string;
  label: string;
  status: string;
  detail: string;
  done: boolean;
  tone: "neutral" | "ready" | "warning";
};

type CustomerSetupStepsInput = {
  selectedDeploymentId: string;
  deploymentValue: CloudInstallationStatusPanelProps["deploymentValue"];
  status: ControlCloudInstallationStatus | null;
  bootstrapPackage: LocalServerBootstrapPackage | null;
  bootstrapPackages: LocalServerBootstrapPackageSummary[];
  latestHeartbeat: ControlCloudInstallationStatus["latestHeartbeat"];
  latestEntitlement: ControlCloudInstallationStatus["latestEntitlement"];
  diagnosticsReport: LocalServerDiagnosticReport | null;
  issuedAppActivation: IssuedSafarSuiteAppActivationToken | null;
  appActivationIssues: SafarSuiteAppActivationIssue[];
};

function getCustomerSetupSteps({
  selectedDeploymentId,
  deploymentValue,
  status,
  bootstrapPackage,
  bootstrapPackages,
  latestHeartbeat,
  latestEntitlement,
  diagnosticsReport,
  issuedAppActivation,
  appActivationIssues
}: CustomerSetupStepsInput): CustomerSetupStep[] {
  const installationId = deploymentValue.installationId.trim();
  const deploymentSaved = selectedDeploymentId.trim() !== "";
  const latestRegisteredPackage = bootstrapPackages[0] ?? null;
  const packageStatus = bootstrapPackage === null
    ? latestRegisteredPackage?.packageStatus ?? "Waiting"
    : "Ready";
  const packageReady = bootstrapPackage !== null || isUsablePackage(latestRegisteredPackage);
  const registrationReady = status !== null;
  const heartbeatReady = latestHeartbeat !== null;
  const heartbeatEntitlementVersion = latestHeartbeat?.entitlementVersion ?? null;
  const entitlementPulled = heartbeatEntitlementVersion !== null;
  const diagnosticsErrorCount = diagnosticsReport?.bundle.recentErrors?.length ?? 0;
  const activeAppActivationIssue = appActivationIssues.find((issue) =>
    !isRevokedAppActivationIssue(issue)
  );
  const appActivationReady = issuedAppActivation !== null || activeAppActivationIssue !== undefined;

  return [
    {
      key: "deployment",
      label: "Deployment profile",
      status: deploymentSaved ? "Saved" : "Waiting",
      detail: deploymentSaved
        ? deploymentValue.displayName.trim() || installationId
        : installationId === "" ? "Installation not set" : "Save deployment profile",
      done: deploymentSaved,
      tone: deploymentSaved ? "ready" : "neutral"
    },
    {
      key: "package",
      label: "Setup packet",
      status: packageReady ? packageStatus : "Waiting",
      detail: formatSetupPackageDetail(bootstrapPackage, latestRegisteredPackage),
      done: packageReady,
      tone: packageReady
        ? isExpiredPackage(latestRegisteredPackage) && bootstrapPackage === null ? "warning" : "ready"
        : "neutral"
    },
    {
      key: "registration",
      label: "Local server",
      status: status?.installationStatus ?? "Waiting",
      detail: status === null
        ? "No registration loaded"
        : `Registered ${formatNullableDateTime(status.registeredAtUtc)}`,
      done: registrationReady,
      tone: registrationReady ? "ready" : "neutral"
    },
    {
      key: "heartbeat",
      label: "Heartbeat",
      status: latestHeartbeat?.heartbeatStatus ?? "Waiting",
      detail: latestHeartbeat === null
        ? "No heartbeat loaded"
        : `${latestHeartbeat.licenseStatus} at ${formatNullableDateTime(latestHeartbeat.receivedAtUtc)}`,
      done: heartbeatReady,
      tone: heartbeatReady ? "ready" : "neutral"
    },
    {
      key: "entitlement",
      label: "Entitlement pulled",
      status: entitlementPulled
        ? `v${heartbeatEntitlementVersion}`
        : latestEntitlement === null ? "Waiting" : "Issued",
      detail: entitlementPulled
        ? `Heartbeat confirmed entitlement v${heartbeatEntitlementVersion}`
        : latestEntitlement === null
          ? "No entitlement evidence loaded"
          : `Cloud issued v${latestEntitlement.entitlementVersion}`,
      done: entitlementPulled,
      tone: entitlementPulled ? "ready" : latestEntitlement === null ? "neutral" : "warning"
    },
    {
      key: "diagnostics",
      label: "Diagnostics",
      status: diagnosticsReport?.status ?? "Waiting",
      detail: diagnosticsReport === null
        ? "No diagnostics loaded"
        : `${diagnosticsReport.bundle.checks.length} checks, ${diagnosticsErrorCount} recent errors`,
      done: diagnosticsReport !== null,
      tone: diagnosticsReport === null
        ? "neutral"
        : diagnosticsErrorCount > 0 || isWarningStatus(diagnosticsReport.status) ? "warning" : "ready"
    },
    {
      key: "app-activation",
      label: "SafarSuite app",
      status: issuedAppActivation !== null ? "Issued" : activeAppActivationIssue?.status ?? "Waiting",
      detail: formatAppActivationSetupDetail(issuedAppActivation, activeAppActivationIssue),
      done: appActivationReady,
      tone: appActivationReady ? "ready" : "neutral"
    }
  ];
}

function isUsablePackage(packageSummary: LocalServerBootstrapPackageSummary | null): boolean {
  return packageSummary !== null && !isExpiredPackage(packageSummary);
}

function isExpiredPackage(packageSummary: LocalServerBootstrapPackageSummary | null): boolean {
  return packageSummary?.packageStatus.trim().toLowerCase() === "expired";
}

function isWarningStatus(value: string): boolean {
  const normalized = value.trim().toLowerCase();

  return normalized !== "healthy"
    && normalized !== "active"
    && normalized !== "registered"
    && normalized !== "ok"
    && normalized !== "received";
}

function formatSetupPackageDetail(
  bootstrapPackage: LocalServerBootstrapPackage | null,
  packageSummary: LocalServerBootstrapPackageSummary | null
): string {
  if (bootstrapPackage !== null) {
    return `${bootstrapPackage.bundleFileName} generated ${formatNullableDateTime(bootstrapPackage.generatedAtUtc)}`;
  }

  if (packageSummary === null) {
    return "No package generated";
  }

  return `${packageSummary.bundleFileName || shortIdentifier(packageSummary.bootstrapPackageId)} generated ${formatNullableDateTime(packageSummary.generatedAtUtc)}`;
}

function formatAppActivationSetupDetail(
  issuedAppActivation: IssuedSafarSuiteAppActivationToken | null,
  activeIssue: SafarSuiteAppActivationIssue | undefined
): string {
  if (issuedAppActivation !== null) {
    return `${shortIdentifier(issuedAppActivation.installationId)} -> ${shortIdentifier(issuedAppActivation.appServerInstallationId)}`;
  }

  if (activeIssue !== undefined) {
    return `${shortIdentifier(activeIssue.installationId)} -> ${shortIdentifier(activeIssue.appServerInstallationId)}`;
  }

  return "No active app activation loaded";
}

function formatPackageConsumption(packageSummary: LocalServerBootstrapPackageSummary): string {
  if (packageSummary.consumedAtUtc === null) {
    return "Not consumed";
  }

  const consumedVersion = formatNullableText(packageSummary.consumedLocalServerVersion);

  return consumedVersion === "Not set"
    ? formatNullableDateTime(packageSummary.consumedAtUtc)
    : `${formatNullableDateTime(packageSummary.consumedAtUtc)} / ${consumedVersion}`;
}

function formatPackageRuntime(packageSummary: LocalServerBootstrapPackageSummary): string {
  const localServerVersion = formatNullableText(packageSummary.localServerVersion);
  const safarSuiteVersion = formatNullableText(packageSummary.safarSuiteAppVersion);

  return safarSuiteVersion === "Not set"
    ? localServerVersion
    : `${localServerVersion} / ${safarSuiteVersion}`;
}

function formatPackageHash(value: string): string {
  return value.trim() === "" ? "Not set" : shortIdentifier(value);
}

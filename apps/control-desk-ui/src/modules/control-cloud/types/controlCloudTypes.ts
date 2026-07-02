export type LocalServerDeploymentProfile = {
  bootstrapMode: string;
  clientDeploymentMode: string;
  siteId: string;
  siteRole: string;
  parentSiteId: string | null;
  branchCode: string | null;
  syncTopologyId: string | null;
};

export type CreateCloudInstallationProvisioningInput = {
  expiresInHours: number;
  createdBy: string;
  bootstrapMode: string;
  clientDeploymentMode: string;
  siteId: string;
  siteRole: string;
  parentSiteId: string;
  branchCode: string;
  syncTopologyId: string;
  localServerVersion: string;
  safarSuiteAppVersion: string;
};

export type CloudInstallationSupportCommandFormInput = {
  commandType: string;
  reason: string;
  requestedBy: string;
  expiresInHours: string;
};

export type QueueCloudInstallationSupportCommandInput = {
  commandType: string;
  reason: string;
  requestedBy: string;
  expiresInHours: number;
};

export type QueuedCloudInstallationSupportCommand = {
  commandId: string;
  clientId: string;
  installationId: string;
  commandVersion: number;
  commandType: string;
  status: string;
  idempotencyKey: string;
  queuedAtUtc: string;
  notBeforeUtc: string | null;
  expiresAtUtc: string;
  acknowledgedAtUtc: string | null;
  acknowledgementStatus: string | null;
  acknowledgementDetail: string | null;
  signatureKeyId: string;
  payloadSha256: string;
};

export type LocalServerSetupToken = {
  setupTokenId: string;
  clientId: string;
  installationId: string;
  setupToken: string;
  tokenStatus: string;
  createdBy: string;
  deploymentMode: string;
  deploymentProfile: LocalServerDeploymentProfile;
  createdAtUtc: string;
  expiresAtUtc: string;
};

export type ControlCloudAuditEvent = {
  auditEventId: string;
  clientId: string | null;
  invitationId: string | null;
  userId: string | null;
  subjectEmail: string;
  eventType: string;
  actor: string;
  detail: string;
  occurredAtUtc: string;
};

export type LocalServerBootstrapPackageEndpoints = {
  registrationUrl: string;
  entitlementBundleUrl: string;
  heartbeatUrl: string;
  pendingCommandsUrl: string;
  diagnosticsUrl: string | null;
};

export type LocalServerBootstrapPackageArtifact = {
  artifactType: string;
  fileName: string;
  downloadUrl: string;
  targetPath: string;
  contentType: string;
  sha256: string;
  content: string;
};

export type LocalServerBootstrapRuntimePlan = {
  runtimeMode: string;
  composeProjectName: string;
  configDirectory: string;
  stateDirectory: string;
  localServerVersion: string;
  safarSuiteAppVersion: string;
};

export type LocalServerBootstrapPackageSignature = {
  algorithm: string;
  keyId: string;
  payloadSha256: string;
  value: string;
};

export type LocalServerBootstrapPackagePayload = {
  formatVersion: string;
  bootstrapPackageId: string;
  setupTokenId: string;
  clientId: string;
  installationId: string;
  deploymentMode: string;
  deploymentProfile: LocalServerDeploymentProfile;
  cloudBaseUrl: string;
  localServerVersion: string;
  setupToken: string;
  setupTokenExpiresAtUtc: string;
  generatedAtUtc: string;
  endpoints: LocalServerBootstrapPackageEndpoints;
  installScriptUrl: string;
  installCommand: string;
  artifacts: LocalServerBootstrapPackageArtifact[];
  runtimePlan: LocalServerBootstrapRuntimePlan | null;
};

export type LocalServerSignedBootstrapBundle = {
  payloadJson: string;
  payload: LocalServerBootstrapPackagePayload;
  signature: LocalServerBootstrapPackageSignature;
};

export type LocalServerBootstrapPackage = LocalServerBootstrapPackagePayload & {
  bundleFileName: string;
  bundleContentType: string;
  bundleSha256: string;
  signedBundle: LocalServerSignedBootstrapBundle;
};

export type LocalServerDiagnosticCheck = {
  code: string;
  status: string;
  detail: string;
};

export type LocalServerDiagnosticModule = {
  moduleCode: string;
  status: string;
  isEnabled: boolean;
};

export type LocalServerDiagnosticEntitlement = {
  hasCachedEntitlement: boolean;
  bundleVersion: string | null;
  bundleIssueId: string | null;
  entitlementVersion: number | null;
  status: string | null;
  bundleIssuedAtUtc: string | null;
  importedAtUtc: string | null;
  validFrom: string | null;
  paidUntil: string | null;
  warningStartsAt: string | null;
  graceUntil: string | null;
  offlineValidUntil: string | null;
  allowedDevices: number | null;
  allowedBranches: number | null;
  signatureKeyId: string | null;
  payloadSha256: string | null;
  modules: LocalServerDiagnosticModule[];
};

export type LocalServerDiagnosticTrustState = {
  lastAcceptedEntitlementVersion: number;
  lastAcceptedBundleIssueId: string | null;
  lastAcceptedBundleIssuedAtUtc: string | null;
  lastAcceptedAtUtc: string | null;
  lastSuccessfulCloudTimeUtc: string | null;
  lastLocalCheckAtUtc: string | null;
  clockMovedBackwards: boolean;
  clockMovedBackwardsDetectedAtUtc: string | null;
  lastClockWarning: string | null;
  lastReplayWarning: string | null;
  lastReplayWarningAtUtc: string | null;
  updatedAtUtc: string;
};

export type LocalServerDiagnosticRuntime = {
  version: string;
  buildChannel: string;
  buildCommit: string;
  runtimeMode: string;
  machineName: string;
  operatingSystem: string;
  hostArchitecture: string;
  processorCount: number;
  dockerAvailable: boolean | null;
  dockerVersion: string | null;
  dockerComposeAvailable: boolean | null;
  dockerComposeVersion: string | null;
};

export type LocalServerDiagnosticBootstrap = {
  configDirectory: string;
  bootstrapStatus: string;
  bootstrapConfigSha256: string | null;
  composeFileSha256: string | null;
  environmentFileSha256: string | null;
  lastRegistrationAttemptUtc: string | null;
  lastRegistrationSucceededAtUtc: string | null;
  lastHeartbeatSentAtUtc: string | null;
  lastEntitlementPullAtUtc: string | null;
};

export type LocalServerDiagnosticService = {
  serviceName: string;
  expectedState: string;
  currentState: string | null;
  containerName: string | null;
  lastStartedAtUtc: string | null;
  detail: string | null;
};

export type LocalServerDiagnosticRecentError = {
  source: string;
  severity: string;
  message: string;
  occurredAtUtc: string | null;
};

export type LocalServerDiagnosticImportAudit = {
  auditRecordId: string;
  installationId: string;
  clientId: string | null;
  importSource: string;
  resultStatus: string;
  entitlementVersion: number | null;
  bundleIssueId: string | null;
  failureCode: string | null;
  detail: string | null;
  payloadSha256: string | null;
  signatureKeyId: string | null;
  occurredAtUtc: string;
};

export type LocalServerDiagnosticBundle = {
  formatVersion: string;
  diagnosticBundleId: string;
  clientId: string;
  installationId: string;
  generatedAtUtc: string;
  generatedBy: string;
  reason: string;
  localServerVersion: string;
  machineName: string;
  operatingSystem: string;
  licenseStatus: string;
  cachedEntitlement: LocalServerDiagnosticEntitlement;
  trustState: LocalServerDiagnosticTrustState | null;
  checks: LocalServerDiagnosticCheck[];
  runtime: LocalServerDiagnosticRuntime | null;
  bootstrap: LocalServerDiagnosticBootstrap | null;
  services: LocalServerDiagnosticService[] | null;
  recentErrors: LocalServerDiagnosticRecentError[] | null;
  importAudit: LocalServerDiagnosticImportAudit[] | null;
  deploymentProfile: LocalServerDeploymentProfile | null;
};

export type LocalServerDiagnosticReport = {
  diagnosticReportId: string;
  clientId: string;
  installationId: string;
  status: string;
  receivedAtUtc: string;
  generatedAtUtc: string;
  uploadedBy: string;
  reason: string;
  localServerVersion: string;
  licenseStatus: string;
  bundle: LocalServerDiagnosticBundle;
};

export type LocalServerHeartbeat = {
  heartbeatId: string;
  installationId: string;
  clientId: string;
  heartbeatStatus: string;
  receivedAtUtc: string;
  reportedAtUtc: string;
  licenseStatus: string;
  entitlementVersion: number | null;
  paidUntil: string | null;
  warningStartsAt: string | null;
  graceUntil: string | null;
  offlineValidUntil: string | null;
  localServerVersion: string | null;
  detail: string | null;
  deploymentProfile?: LocalServerDeploymentProfile | null;
};

export type ControlCloudInstallationEntitlementStatus = {
  bundleIssueId: string;
  entitlementVersion: number;
  entitlementSnapshotId: string;
  issuedAtUtc: string;
  paidUntil: string;
  warningStartsAt: string;
  graceUntil: string;
  offlineValidUntil: string;
  keyId: string;
  payloadSha256: string;
};

export type ControlCloudInstallationCommandStatus = {
  pendingCommandCount: number;
  latestCommandVersion: number;
  latestCommandId: string | null;
  latestCommandType: string | null;
  latestCommandStatus: string | null;
  latestCommandQueuedAtUtc: string | null;
  latestCommandAcknowledgedAtUtc: string | null;
  latestAcknowledgementStatus: string | null;
  latestAcknowledgementDetail: string | null;
};

export type ControlCloudInstallationStatus = {
  clientId: string;
  installationId: string;
  installationStatus: string;
  deploymentProfile: LocalServerDeploymentProfile;
  registeredAtUtc: string;
  lastBundleIssuedAtUtc: string | null;
  latestEntitlementVersion: number;
  latestHeartbeat: LocalServerHeartbeat | null;
  latestEntitlement: ControlCloudInstallationEntitlementStatus | null;
  commandStatus: ControlCloudInstallationCommandStatus;
};

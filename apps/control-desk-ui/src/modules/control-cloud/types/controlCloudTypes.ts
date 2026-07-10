export type LocalServerDeploymentProfile = {
  bootstrapMode: string;
  clientDeploymentMode: string;
  siteId: string;
  siteRole: string;
  parentSiteId: string | null;
  branchCode: string | null;
  syncTopologyId: string | null;
};

export type ControlCloudConnectionState = {
  status: "notChecked" | "checking" | "connected" | "unavailable" | "notConfigured";
  detail: string;
  checkedAtUtc: string | null;
};

export type CloudOutboxMessage = {
  cloudOutboxMessageId: string;
  messageType: string;
  subjectType: string;
  subjectId: string;
  payloadJson: string;
  status: string;
  attemptCount: number;
  occurredAtUtc: string;
  lastAttemptedAtUtc: string | null;
  nextAttemptAtUtc: string | null;
  sentAtUtc: string | null;
  failedAtUtc: string | null;
  failureReason: string | null;
};

export type PublishedCloudOutboxMessage = {
  cloudOutboxMessageId: string;
  messageType: string;
  subjectType: string;
  subjectId: string;
  status: string;
  attemptCount: number;
  lastAttemptedAtUtc: string | null;
  nextAttemptAtUtc: string | null;
  sentAtUtc: string | null;
  failedAtUtc: string | null;
  failureReason: string | null;
  cloudReference: string | null;
  envelopeSignature: string | null;
};

export type PublishCloudOutboxMessagesResult = {
  requestedBatchSize: number;
  publishedCount: number;
  failedCount: number;
  messages: PublishedCloudOutboxMessage[];
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

export type CloudAppActivationTokenFormInput = {
  activationRequestId: string;
  replacesActivationIssueId: string;
  serverInstallationId: string;
  fingerprintHash: string;
  serverPublicKey: string;
  requestedBy: string;
};

export type IssueCloudAppActivationTokenInput = {
  activationRequestId: string | null;
  replacesActivationIssueId: string | null;
  serverInstallationId: string;
  fingerprintHash: string;
  serverPublicKey: string;
  requestedBy: string;
};

export type CloudAppActivationRevocationFormInput = {
  revokedBy: string;
  reason: string;
};

export type CloudFirstManagerSetupTokenFormInput = {
  pendingDeviceRequestId: string;
  managerDisplayName: string;
  managerEmail: string;
  createdBy: string;
  expiresInHours: string;
  purpose: "FirstManagerBootstrap" | "ManagerRecovery";
  recoveryReason: string;
};

export type IssueCloudFirstManagerSetupTokenInput = {
  pendingDeviceRequestId: string;
  managerDisplayName: string;
  managerEmail: string;
  createdBy: string;
  expiresInHours: number;
  purpose: "FirstManagerBootstrap" | "ManagerRecovery";
  recoveryReason: string;
};

export type RevokeCloudAppActivationIssueInput = {
  revokedBy: string;
  reason: string;
};

export type ProviderAccessOperator = {
  userId: string;
  email: string;
  fullName: string;
  status: string;
  scopes: string[];
  createdAtUtc: string;
  createdBy: string;
  updatedAtUtc: string | null;
  updatedBy: string | null;
  lastLoginAtUtc: string | null;
  mfaEnabled?: boolean;
  totpEnabled?: boolean;
  totpEnabledAtUtc?: string | null;
  totpUpdatedAtUtc?: string | null;
  totpUpdatedBy?: string | null;
  lastTotpUsedAtUtc?: string | null;
  recoveryCodeCount?: number;
  recoveryCodesUpdatedAtUtc?: string | null;
  recoveryCodesUpdatedBy?: string | null;
  lastRecoveryCodeUsedAtUtc?: string | null;
  failedLoginAttemptCount?: number;
  lastFailedLoginAtUtc?: string | null;
  lockoutEndsAtUtc?: string | null;
};

export type ProviderAccessSession = {
  accessToken: string;
  tokenType: string;
  actor: string;
  scopes: string[];
  expiresAtUtc: string;
};

export type ProviderAccessSessionCreateInput = {
  email: string;
  password: string;
  scopes: string[];
  expiresInMinutes: number;
  recoveryCode?: string;
  totpCode?: string;
};

export type ProviderAccessPasswordChangeInput = {
  email: string;
  currentPassword: string;
  newPassword: string;
};

export type ProviderAccessOperatorCreateInput = {
  email: string;
  fullName: string;
  password: string;
  scopes: string[];
  createdBy: string;
};

export type ProviderAccessOperatorPasswordInput = {
  password: string;
  updatedBy: string;
};

export type ProviderAccessOperatorRecoveryCodesInput = {
  count: number;
  updatedBy: string;
};

export type ProviderAccessOperatorRecoveryCodesResult = {
  operator: ProviderAccessOperator;
  recoveryCodes: string[];
};

export type ProviderAccessOperatorTotpInput = {
  updatedBy: string;
};

export type ProviderAccessOperatorTotpEnrollmentResult = {
  operator: ProviderAccessOperator;
  secret: string;
  otpAuthUri: string;
};

export type ProviderAccessOperatorScopesInput = {
  scopes: string[];
  updatedBy: string;
};

export type ProviderAccessOperatorStatusInput = {
  status: string;
  updatedBy: string;
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

export type SafarSuiteAppActivationTokenImport = {
  activationToken: string;
  signature: string;
  signingKeyId: string;
  tenantId: string;
  branchId: string;
  customerCode: string;
  customerName: string;
  branchName: string;
  paidUntil: string;
  graceEndsOn: string;
  offlineValidUntil: string;
  moduleEntitlements: Record<string, boolean>;
};

export type IssuedSafarSuiteAppActivationToken = {
  activationIssueId: string;
  clientId: string;
  installationId: string;
  appServerInstallationId: string;
  activationRequestId: string;
  replacesActivationIssueId: string | null;
  entitlementVersion: number;
  signingKeyId: string;
  issuedAtUtc: string;
  expiresAtUtc: string;
  import: SafarSuiteAppActivationTokenImport;
};

export type SafarSuiteAppActivationIssue = {
  activationIssueId: string;
  clientId: string;
  installationId: string;
  appServerInstallationId: string;
  activationRequestId: string;
  replacesActivationIssueId: string | null;
  fingerprintHash: string;
  serverPublicKeySha256: string;
  entitlementVersion: number;
  signingKeyId: string;
  status: string;
  requestedBy: string;
  issuedAtUtc: string;
  expiresAtUtc: string;
  revokedAtUtc: string | null;
  revokedBy: string | null;
  revocationReason: string | null;
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

export type LocalServerBootstrapSecretReadiness = {
  status: "Ready" | "Review" | "Blocked" | string;
  activeKeyId: string;
  hasActiveSecret: boolean;
  warnings: string[];
  requiredEnvironmentVariables: string[];
  detail: string;
};

export type LocalServerFirstManagerSetupTokenPayload = {
  formatVersion: string;
  tokenId: string;
  clientId: string;
  installationId: string;
  pendingDeviceRequestId: string;
  allowedActions: string[];
  managerDisplayName: string;
  managerEmail: string | null;
  createdBy: string;
  issuedAtUtc: string;
  expiresAtUtc: string;
  purpose: "FirstManagerBootstrap" | "ManagerRecovery";
  recoveryReason: string | null;
};

export type LocalServerSignedFirstManagerSetupToken = {
  payloadJson: string;
  payload: LocalServerFirstManagerSetupTokenPayload;
  signature: LocalServerBootstrapPackageSignature;
};

export type IssuedLocalServerFirstManagerSetupToken = {
  tokenId: string;
  clientId: string;
  installationId: string;
  pendingDeviceRequestId: string;
  managerDisplayName: string;
  managerEmail: string | null;
  createdBy: string;
  signingKeyId: string;
  payloadSha256: string;
  issuedAtUtc: string;
  expiresAtUtc: string;
  signedToken: LocalServerSignedFirstManagerSetupToken;
  purpose: "FirstManagerBootstrap" | "ManagerRecovery";
  recoveryReason: string | null;
  allowedActions: string[] | null;
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
  secretReadiness: LocalServerBootstrapSecretReadiness | null;
};

export type LocalServerBootstrapPackageSummary = {
  bootstrapPackageId: string;
  setupTokenId: string;
  clientId: string;
  installationId: string;
  packageStatus: string;
  tokenStatus: string;
  createdBy: string;
  deploymentMode: string;
  deploymentProfile: LocalServerDeploymentProfile;
  createdAtUtc: string;
  generatedAtUtc: string;
  setupTokenExpiresAtUtc: string;
  consumedAtUtc: string | null;
  consumedLocalServerVersion: string | null;
  localServerVersion: string;
  safarSuiteAppVersion: string;
  bundleFileName: string;
  bundleSha256: string;
};

export type LocalServerBootstrapPackageRegister = {
  packages: LocalServerBootstrapPackageSummary[];
};

export type CloudBootstrapPackageHandoffFormInput = {
  channel: string;
  recipient: string;
  markedBy: string;
  note: string;
};

export type MarkCloudBootstrapPackageHandoffInput = {
  channel: string;
  recipient: string;
  markedBy: string;
  note?: string;
};

export type LocalServerBootstrapPackageHandoff = {
  bootstrapPackageId: string;
  setupTokenId: string;
  clientId: string;
  installationId: string;
  handoffStatus: string;
  channel: string;
  recipient: string;
  markedBy: string;
  note: string | null;
  markedAtUtc: string;
};

export type LocalServerPairingDescriptor = {
  formatVersion: "safarsuite-local-pairing-descriptor-v1";
  clientId: string;
  providerInstallationId: string;
  bootstrapPackageId: string;
  setupTokenId: string;
  displayName: string;
  appServerInstallationId?: string;
  serverInstallationId?: string;
  siteId?: string;
  siteRole?: string;
  customerCode?: string;
  customerName?: string;
  branchName?: string;
  fingerprintHash?: string;
  tlsCaSha256?: string;
  tlsCertificateSha256?: string;
  serverPairingKeySha256?: string;
  urlCandidates: string[];
  generatedAtUtc: string;
  expiresAtUtc?: string;
  source: "ControlDeskBootstrapPackage" | "ControlCloudPairingDescriptor";
  bootstrapBundleSha256: string;
  bootstrapSignatureKeyId: string;
  notes: string[];
  signatureAlgorithm?: string;
  signatureKeyId?: string;
  payloadSha256?: string;
  signature?: string;
};

export type IssueLocalServerPairingDescriptorInput = {
  bootstrapPackageId?: string;
  setupTokenId?: string;
  clientCode?: string;
  customerName?: string;
  appServerInstallationId?: string;
  fingerprintHash?: string;
  urlCandidates?: string[];
  tlsCaSha256?: string;
  tlsCertificateSha256?: string;
  serverPairingKeySha256?: string;
  requestedBy?: string;
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
  pairingStatus?: LocalServerPairingStatus | null;
};

export type LocalServerPairingStatus = {
  pairingMode: string;
  totalDeviceCount: number;
  pendingDeviceCount: number;
  approvedDeviceCount: number;
  suspendedDeviceCount: number;
  revokedDeviceCount: number;
  firstManagerDeviceApproved: boolean;
  firstManagerDeviceApprovedAtUtc: string | null;
  lastDeviceUpdatedAtUtc: string | null;
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

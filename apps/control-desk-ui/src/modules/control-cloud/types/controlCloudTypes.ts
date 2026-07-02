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
  registeredAtUtc: string;
  lastBundleIssuedAtUtc: string | null;
  latestEntitlementVersion: number;
  latestHeartbeat: LocalServerHeartbeat | null;
  latestEntitlement: ControlCloudInstallationEntitlementStatus | null;
  commandStatus: ControlCloudInstallationCommandStatus;
};

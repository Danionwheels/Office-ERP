import type {
  ClientDeployment,
  ClientDetails,
  ConfigureClientDeploymentInput
} from "../../clients/types/clientTypes";
import type {
  CloudAppActivationRevocationFormInput,
  CloudAppActivationTokenFormInput,
  CloudFirstManagerSetupTokenFormInput,
  CloudInstallationSupportCommandFormInput,
  CloudOutboxMessage,
  ControlCloudAuditEvent,
  ControlCloudConnectionState,
  ControlCloudInstallationStatus,
  IssuedSafarSuiteAppActivationToken,
  IssuedLocalServerFirstManagerSetupToken,
  LocalServerBootstrapPackage,
  LocalServerBootstrapPackageSummary,
  LocalServerDiagnosticReport,
  LocalServerDeploymentProfile,
  LocalServerSetupToken,
  PublishCloudOutboxMessagesResult,
  QueuedCloudInstallationSupportCommand,
  SafarSuiteAppActivationIssue
} from "./controlCloudTypes";

export type CloudInstallationStatusPanelProps = {
  client: ClientDetails | null;
  installationId: string;
  deployments: ClientDeployment[];
  selectedDeploymentId: string;
  deploymentValue: ConfigureClientDeploymentInput;
  setupTokenHours: string;
  connectionState: ControlCloudConnectionState;
  status: ControlCloudInstallationStatus | null;
  setupToken: LocalServerSetupToken | null;
  bootstrapPackage: LocalServerBootstrapPackage | null;
  bootstrapPackages: LocalServerBootstrapPackageSummary[];
  supportCommandValue: CloudInstallationSupportCommandFormInput;
  queuedSupportCommand: QueuedCloudInstallationSupportCommand | null;
  appActivationValue: CloudAppActivationTokenFormInput;
  issuedAppActivation: IssuedSafarSuiteAppActivationToken | null;
  firstManagerSetupTokenValue: CloudFirstManagerSetupTokenFormInput;
  issuedFirstManagerSetupToken: IssuedLocalServerFirstManagerSetupToken | null;
  appActivationIssues: SafarSuiteAppActivationIssue[];
  appActivationIssueSearch: string;
  appActivationRevocationValue: CloudAppActivationRevocationFormInput;
  outboxMessages: CloudOutboxMessage[];
  latestOutboxPublish: PublishCloudOutboxMessagesResult | null;
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
  onRefreshBootstrapPackages: () => Promise<void>;
  onSupportCommandValueChange: (value: CloudInstallationSupportCommandFormInput) => void;
  onQueueSupportCommand: () => Promise<void>;
  onAppActivationValueChange: (value: CloudAppActivationTokenFormInput) => void;
  onIssueAppActivationToken: () => Promise<void>;
  onFirstManagerSetupTokenValueChange: (value: CloudFirstManagerSetupTokenFormInput) => void;
  onIssueFirstManagerSetupToken: () => Promise<void>;
  onAppActivationIssueSearchChange: (value: string) => void;
  onRefreshAppActivationIssues: () => Promise<void>;
  onAppActivationRevocationValueChange: (value: CloudAppActivationRevocationFormInput) => void;
  onRevokeAppActivationIssue: (activationIssueId: string) => Promise<void>;
  onPrepareReplacementAppActivationIssue: (issue: SafarSuiteAppActivationIssue) => void;
  onRefreshOutboxMessages: () => Promise<void>;
  onPublishOutboxMessages: () => Promise<void>;
  onRefreshAuditEvents: () => Promise<void>;
  onRefreshDiagnostics: () => Promise<void>;
  onRefresh: () => Promise<void>;
};

export type CloudTone = "neutral" | "ready" | "warning";

export type CloudControlKey =
  | "cloudLink"
  | "installation"
  | "heartbeat"
  | "pairing"
  | "entitlement"
  | "appActivation"
  | "commands"
  | "diagnostics"
  | "deployment"
  | "history";

export type CloudControlRow = {
  key: CloudControlKey;
  label: string;
  status: string;
  detail: string;
  tone: CloudTone;
};

export type AppActivationRequestJson = {
  activationRequestId?: string | null;
  serverInstallationId: string;
  fingerprintHash: string;
  serverPublicKey: string;
  requestedBy?: string | null;
};

export type AppIdentityMapping = {
  providerInstallationId: string;
  appServerInstallationId: string;
  appFingerprint: string;
  status: string;
  tone: CloudTone;
};

export type CloudControlRowsInput = {
  auditEvents: ControlCloudAuditEvent[];
  appActivationIssues: SafarSuiteAppActivationIssue[];
  connectionState: ControlCloudConnectionState;
  commandStatus: ControlCloudInstallationStatus["commandStatus"] | null;
  deploymentProfile: LocalServerDeploymentProfile | null;
  deploymentValue: ConfigureClientDeploymentInput;
  diagnosticsReport: LocalServerDiagnosticReport | null;
  issuedAppActivation: IssuedSafarSuiteAppActivationToken | null;
  installationId: string;
  latestEntitlement: ControlCloudInstallationStatus["latestEntitlement"] | null;
  latestHeartbeat: ControlCloudInstallationStatus["latestHeartbeat"] | null;
  status: ControlCloudInstallationStatus | null;
};

import type {
  IssuedSafarSuiteAppActivationToken,
  IssuedLocalServerFirstManagerSetupToken,
  LocalServerBootstrapPackage,
  LocalServerBootstrapPackageArtifact,
  LocalServerDiagnosticReport,
  LocalServerPairingDescriptor
} from "../types/controlCloudTypes";

const defaultBundleContentType = "application/vnd.safarsuite.local-server-bootstrap+json";

export function downloadBootstrapBundle(bootstrapPackage: LocalServerBootstrapPackage) {
  const bundleJson = JSON.stringify(bootstrapPackage.signedBundle, null, 2);
  const fallbackFileName = `${bootstrapPackage.installationId}-bootstrap.json`;

  downloadFile({
    content: bundleJson,
    fileName: bootstrapPackage.bundleFileName.trim() || fallbackFileName,
    contentType: bootstrapPackage.bundleContentType || defaultBundleContentType
  });
}

export function downloadBootstrapArtifact(
  artifact: LocalServerBootstrapPackageArtifact
) {
  downloadFile({
    content: artifact.content,
    fileName: artifact.fileName,
    contentType: artifact.contentType || "text/plain"
  });
}

export function downloadCustomerSetupGuide(
  bootstrapPackage: LocalServerBootstrapPackage,
  clientCode: string | null,
  appIdentity: PairingDescriptorAppIdentity | null = null
) {
  const pairingDescriptor = buildPairingDescriptor(
    bootstrapPackage,
    clientCode,
    appIdentity);
  const guide = [
    "# SafarSuite Customer Setup",
    "",
    `Client: ${clientCode?.trim() || bootstrapPackage.clientId}`,
    `Installation: ${bootstrapPackage.installationId}`,
    `Bootstrap package: ${bootstrapPackage.bootstrapPackageId}`,
    `Bundle: ${bootstrapPackage.bundleFileName}`,
    `Bundle SHA-256: ${bootstrapPackage.bundleSha256}`,
    `Setup token expires: ${bootstrapPackage.setupTokenExpiresAtUtc}`,
    `Runtime: ${bootstrapPackage.runtimePlan?.runtimeMode ?? "DockerCompose"}`,
    `LocalServer version: ${bootstrapPackage.localServerVersion}`,
    `SafarSuite app version: ${bootstrapPackage.runtimePlan?.safarSuiteAppVersion ?? "Not set"}`,
    "",
    "## Install",
    "",
    "```bash",
    bootstrapPackage.installCommand,
    "```",
    "",
    "## Windows / PowerShell",
    "",
    "The install command is a Bash command. On Windows, run it from Git Bash or WSL with Docker Desktop running; do not paste it directly into PowerShell or Command Prompt.",
    "",
    "From PowerShell, open Git Bash with:",
    "",
    "```powershell",
    "& \"C:\\Program Files\\Git\\bin\\bash.exe\"",
    "```",
    "",
    "Or open WSL with:",
    "",
    "```powershell",
    "wsl",
    "```",
    "",
    "Then paste the Bash install command from the Install section.",
    "",
    "Keep this guide and the signed bundle in a secure handoff channel until the setup token expires. Generate a fresh bootstrap package if either file is sent to the wrong place.",
    "",
    "## SafarSuite App Pairing Descriptor",
    "",
    "Download the pairing descriptor from Control Desk and import it on the SafarSuite Windows pre-login screen. The descriptor is a non-secret hint file: it carries URL candidates and setup identity metadata, then the Windows app still validates the live LocalServer hello response and requires fingerprint confirmation before it trusts the server.",
    "",
    `Descriptor format: ${pairingDescriptor.formatVersion}`,
    `Descriptor expires: ${pairingDescriptor.expiresAtUtc ?? "Not set"}`,
    `Descriptor URLs: ${pairingDescriptor.urlCandidates.join(", ")}`,
    ...(pairingDescriptor.serverInstallationId
      ? [`App server id: ${pairingDescriptor.serverInstallationId}`]
      : ["App server id: Not bound yet; import the app activation request/response first if you need a pinned descriptor."]),
    ...(pairingDescriptor.fingerprintHash
      ? [`App server fingerprint: ${pairingDescriptor.fingerprintHash}`]
      : []),
    "",
    "## Verification",
    "",
    "- Refresh the Control Cloud installation status.",
    "- Confirm bootstrap registration is Active or Registered.",
    "- Confirm entitlement and heartbeat are present.",
    "- Queue request_diagnostics and refresh diagnostics after the local agent processes it.",
    "- Import the SafarSuite app activation request and issue the app activation import.",
    "",
    "## Runtime Artifacts",
    "",
    ...bootstrapPackage.artifacts.flatMap((artifact) => [
      `- ${artifact.fileName}`,
      `  - Type: ${artifact.artifactType}`,
      `  - Target: ${artifact.targetPath}`,
      `  - SHA-256: ${artifact.sha256}`
    ]),
    ""
  ].join("\n");

  downloadFile({
    content: guide,
    fileName: `safarsuite-setup-${bootstrapPackage.installationId}.md`,
    contentType: "text/markdown"
  });
}

export function downloadPairingDescriptor(
  bootstrapPackage: LocalServerBootstrapPackage,
  clientCode: string | null,
  appIdentity: PairingDescriptorAppIdentity | null = null
) {
  const descriptor = buildPairingDescriptor(
    bootstrapPackage,
    clientCode,
    appIdentity);

  downloadFile({
    content: JSON.stringify(descriptor, null, 2),
    fileName: `safarsuite-pairing-${bootstrapPackage.installationId}.json`,
    contentType: "application/json"
  });
}

export function downloadPairingDescriptorFile(
  descriptor: LocalServerPairingDescriptor,
  installationId: string
) {
  downloadFile({
    content: JSON.stringify(descriptor, null, 2),
    fileName: `safarsuite-pairing-${installationId}.json`,
    contentType: "application/json"
  });
}

export function downloadDiagnosticsReport(diagnosticsReport: LocalServerDiagnosticReport) {
  downloadFile({
    content: JSON.stringify(diagnosticsReport, null, 2),
    fileName: `safarsuite-diagnostics-${diagnosticsReport.installationId}.json`,
    contentType: "application/json"
  });
}

export function downloadAppActivationImport(
  issuedAppActivation: IssuedSafarSuiteAppActivationToken
) {
  downloadFile({
    content: JSON.stringify(issuedAppActivation.import, null, 2),
    fileName: `safarsuite-app-activation-${issuedAppActivation.installationId}-${issuedAppActivation.appServerInstallationId}.json`,
    contentType: "application/json"
  });
}

export function downloadFirstManagerSetupToken(
  issuedToken: IssuedLocalServerFirstManagerSetupToken
) {
  const purposeSlug = issuedToken.purpose === "ManagerRecovery"
    ? "manager-recovery"
    : "first-manager";

  downloadFile({
    content: JSON.stringify(issuedToken.signedToken, null, 2),
    fileName: `safarsuite-${purposeSlug}-${issuedToken.installationId}-${issuedToken.pendingDeviceRequestId}.json`,
    contentType: "application/json"
  });
}

export type PairingDescriptorAppIdentity = {
  appServerInstallationId: string;
  fingerprintHash?: string | null;
};

export function buildPairingDescriptor(
  bootstrapPackage: LocalServerBootstrapPackage,
  clientCode: string | null,
  appIdentity: PairingDescriptorAppIdentity | null
): LocalServerPairingDescriptor {
  const deploymentProfile = bootstrapPackage.deploymentProfile;
  const appServerInstallationId = optionalText(appIdentity?.appServerInstallationId);
  const fingerprintHash = optionalText(appIdentity?.fingerprintHash);
  const appPort = readEnvironmentValue(bootstrapPackage, "SAFARSUITE_APP_HTTP_PORT") ?? "5280";
  const configuredPairingUrl = readEnvironmentValue(bootstrapPackage, "SAFARSUITE_LOCAL_PAIRING_HTTPS_URL");
  const urlCandidates = dedupeText([
    configuredPairingUrl,
    `http://localhost:${appPort}`,
    `http://127.0.0.1:${appPort}`,
    deploymentProfile.siteId ? `http://safarsuite-${deploymentProfile.siteId}.lan:${appPort}` : undefined,
    deploymentProfile.branchCode ? `http://safarsuite-${deploymentProfile.branchCode}.lan:${appPort}` : undefined
  ]);

  return {
    formatVersion: "safarsuite-local-pairing-descriptor-v1",
    clientId: bootstrapPackage.clientId,
    providerInstallationId: bootstrapPackage.installationId,
    bootstrapPackageId: bootstrapPackage.bootstrapPackageId,
    setupTokenId: bootstrapPackage.setupTokenId,
    displayName: formatDescriptorDisplayName(bootstrapPackage, clientCode),
    ...(appServerInstallationId ? { appServerInstallationId, serverInstallationId: appServerInstallationId } : {}),
    siteId: optionalText(deploymentProfile.siteId),
    siteRole: optionalText(deploymentProfile.siteRole),
    customerCode: optionalText(clientCode),
    branchName: optionalText(deploymentProfile.branchCode) ?? optionalText(deploymentProfile.siteId),
    fingerprintHash,
    tlsCaSha256: readEnvironmentValue(bootstrapPackage, "SAFARSUITE_LOCAL_API_TLS_CA_SHA256"),
    tlsCertificateSha256: readEnvironmentValue(bootstrapPackage, "SAFARSUITE_LOCAL_API_TLS_CERTIFICATE_SHA256"),
    serverPairingKeySha256: readEnvironmentValue(bootstrapPackage, "SAFARSUITE_LOCAL_PAIRING_KEY_SHA256"),
    urlCandidates,
    generatedAtUtc: new Date().toISOString(),
    expiresAtUtc: bootstrapPackage.setupTokenExpiresAtUtc,
    source: "ControlDeskBootstrapPackage",
    bootstrapBundleSha256: bootstrapPackage.bundleSha256,
    bootstrapSignatureKeyId: bootstrapPackage.signedBundle.signature.keyId,
    notes: [
      "This descriptor is safe to share with the customer setup packet.",
      "It does not contain setup-token plaintext, provider credentials, database credentials, or activation tokens.",
      "The SafarSuite Windows app must still validate the live LocalServer identity and require fingerprint confirmation before trust is written."
    ]
  };
}

function formatDescriptorDisplayName(
  bootstrapPackage: LocalServerBootstrapPackage,
  clientCode: string | null
): string {
  const clientLabel = optionalText(clientCode) ?? bootstrapPackage.clientId;
  const siteLabel = optionalText(bootstrapPackage.deploymentProfile.branchCode)
    ?? optionalText(bootstrapPackage.deploymentProfile.siteId)
    ?? bootstrapPackage.installationId;

  return `${clientLabel} - ${siteLabel}`;
}

function readEnvironmentValue(
  bootstrapPackage: LocalServerBootstrapPackage,
  key: string
): string | undefined {
  const environmentArtifact = bootstrapPackage.artifacts.find((artifact) =>
    artifact.fileName === "local-server.env.template"
      || artifact.artifactType === "EnvironmentTemplate");

  if (!environmentArtifact) {
    return undefined;
  }

  const pattern = new RegExp(`^${escapeRegExp(key)}=(.*)$`, "mu");
  const match = environmentArtifact.content.match(pattern);

  return optionalText(match?.[1]);
}

function optionalText(value: string | null | undefined): string | undefined {
  const trimmed = value?.trim();

  return trimmed ? trimmed : undefined;
}

function dedupeText(values: Array<string | undefined>): string[] {
  const result: string[] = [];

  for (const value of values) {
    const trimmed = optionalText(value);
    if (trimmed && !result.some((item) => item.toLowerCase() === trimmed.toLowerCase())) {
      result.push(trimmed);
    }
  }

  return result;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

export async function copyTextToClipboard(value: string): Promise<void> {
  if (navigator.clipboard?.writeText !== undefined) {
    await navigator.clipboard.writeText(value);
    return;
  }

  const textarea = document.createElement("textarea");

  textarea.value = value;
  textarea.setAttribute("readonly", "true");
  textarea.style.position = "fixed";
  textarea.style.left = "-9999px";
  document.body.appendChild(textarea);
  textarea.select();

  try {
    document.execCommand("copy");
  } finally {
    textarea.remove();
  }
}

function downloadFile({
  content,
  contentType,
  fileName
}: {
  content: string;
  contentType: string;
  fileName: string;
}) {
  const blob = new Blob([content], {
    type: contentType
  });
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = objectUrl;
  link.download = fileName;
  link.style.display = "none";
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 0);
}

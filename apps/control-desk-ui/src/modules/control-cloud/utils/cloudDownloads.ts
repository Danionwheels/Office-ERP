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
  const secretReadiness = bootstrapPackage.secretReadiness;
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
    `Compose project: ${bootstrapPackage.runtimePlan?.composeProjectName ?? "Not set"}`,
    `State directory: ${bootstrapPackage.runtimePlan?.stateDirectory ?? "Not set"}`,
    `Signing readiness: ${secretReadiness?.status ?? "Not reported"}`,
    `Signing key id: ${secretReadiness?.activeKeyId ?? bootstrapPackage.signedBundle.signature.keyId}`,
    "",
    "## Install",
    "",
    "```bash",
    bootstrapPackage.installCommand,
    "```",
    "",
    "## Clean Target Preflight",
    "",
    "- Use a fresh machine, VM, or dedicated Docker Desktop target for the first customer install whenever possible.",
    `- This package expects compose project ${bootstrapPackage.runtimePlan?.composeProjectName ?? "from the generated runtime plan"} and state directory ${bootstrapPackage.runtimePlan?.stateDirectory ?? "from the generated runtime plan"}.`,
    "- If retrying after a failed install on the same host, stop and remove only the previous SafarSuite setup stack and its Docker volumes for this installation before rerunning the install command.",
    "- Do not reuse database volumes from another customer, another bootstrap package, or another PostgreSQL major version.",
    "- Do not remove volumes from an existing live customer installation unless the provider and customer have confirmed the local data can be destroyed.",
    "",
    "## Provider Secret Custody",
    "",
    ...(secretReadiness === null
      ? ["- Control Cloud did not return a signing-readiness report for this package. Confirm the active signing key and install-time secret through the provider runbook before import."]
      : [
        `- Control Cloud signing readiness: ${secretReadiness.status}. ${secretReadiness.detail}`,
        `- Active signing key id: ${secretReadiness.activeKeyId}. The key id is not secret and is already included in the generated install command.`,
        `- Install-time provider variables to control: ${secretReadiness.requiredEnvironmentVariables.join(", ") || "Not reported"}.`,
        ...secretReadiness.warnings.map((warning) => `- Review: ${warning}`)
      ]),
    "- Provider signing and trust secrets are not part of the customer setup packet. Keep them in the provider-approved secret store or inject them only during a controlled install session.",
    "- Do not put provider signing secrets, provider credentials, database passwords, app activation tokens, signing private keys, or long-lived HMAC secrets into this guide, tickets, chat, email, or customer-visible notes.",
    "- Before import or registration, confirm the generated runtime environment uses the provider-approved entitlement/bootstrap signing secret. If the template still contains placeholder or change-before-production trust values, stop and supply the approved value before continuing.",
    "- Treat setup tokens and signed bundles as time-limited handoff artifacts. Treat signing secrets as provider-owned operational secrets that must not be handed to the customer.",
    "",
    "## Windows / PowerShell",
    "",
    "The install command is a Bash command. On Windows, run it from Git Bash or WSL with Docker Desktop running; do not paste it directly into PowerShell or Command Prompt.",
    "Use Git Bash or WSL for Local API HTTPS verification until provider support has confirmed host-side PowerShell/.NET trust behavior for the generated local CA on that workstation.",
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
    "## Local API TLS Troubleshooting",
    "",
    "- The generated runtime uses HTTPS for the local API by default with a generated local CA. Keep the installer and verification helper on the generated CA-pinned path before trying native Windows host checks.",
    "- If the Git Bash or WSL helper succeeds but PowerShell, .NET, or Windows Schannel curl reports SEC_E_NO_CREDENTIALS, certificate revocation, or credential-selection errors against the local API, treat it as a Windows host trust/tooling issue first.",
    "- Do not disable certificate validation or switch the customer installation to HTTP only to make a host-side check pass. Capture the helper output, Control Cloud registration, heartbeat, entitlement, and diagnostics evidence instead.",
    "- Escalate to provider support if native Windows Local API evidence is required; support may need to inspect the generated local CA trust material, hostname, Schannel policy, and customer machine certificate state.",
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

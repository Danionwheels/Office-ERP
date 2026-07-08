import type {
  IssuedSafarSuiteAppActivationToken,
  IssuedLocalServerFirstManagerSetupToken,
  LocalServerBootstrapPackage,
  LocalServerBootstrapPackageArtifact,
  LocalServerDiagnosticReport
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
  clientCode: string | null
) {
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
  downloadFile({
    content: JSON.stringify(issuedToken.signedToken, null, 2),
    fileName: `safarsuite-first-manager-${issuedToken.installationId}-${issuedToken.pendingDeviceRequestId}.json`,
    contentType: "application/json"
  });
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

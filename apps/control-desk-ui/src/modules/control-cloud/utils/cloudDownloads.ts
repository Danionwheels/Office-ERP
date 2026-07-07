import type {
  IssuedSafarSuiteAppActivationToken,
  LocalServerBootstrapPackage,
  LocalServerDiagnosticReport
} from "../types/controlCloudTypes";

const defaultBundleContentType = "application/vnd.safarsuite.local-server-bootstrap+json";

export function downloadBootstrapBundle(bootstrapPackage: LocalServerBootstrapPackage) {
  const bundleJson = JSON.stringify(bootstrapPackage.signedBundle, null, 2);
  const fallbackFileName = `${bootstrapPackage.installationId}-bootstrap.json`;

  downloadJsonFile({
    content: bundleJson,
    fileName: bootstrapPackage.bundleFileName.trim() || fallbackFileName,
    contentType: bootstrapPackage.bundleContentType || defaultBundleContentType
  });
}

export function downloadDiagnosticsReport(diagnosticsReport: LocalServerDiagnosticReport) {
  downloadJsonFile({
    content: JSON.stringify(diagnosticsReport, null, 2),
    fileName: `safarsuite-diagnostics-${diagnosticsReport.installationId}.json`,
    contentType: "application/json"
  });
}

export function downloadAppActivationImport(
  issuedAppActivation: IssuedSafarSuiteAppActivationToken
) {
  downloadJsonFile({
    content: JSON.stringify(issuedAppActivation.import, null, 2),
    fileName: `safarsuite-app-activation-${issuedAppActivation.installationId}-${issuedAppActivation.appServerInstallationId}.json`,
    contentType: "application/json"
  });
}

function downloadJsonFile({
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

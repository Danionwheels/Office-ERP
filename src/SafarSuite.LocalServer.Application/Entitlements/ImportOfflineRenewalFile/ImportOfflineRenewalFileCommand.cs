namespace SafarSuite.LocalServer.Application.Entitlements.ImportOfflineRenewalFile;

public sealed record ImportOfflineRenewalFileCommand(
    string ExpectedInstallationId,
    string RenewalFileJson);

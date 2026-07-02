using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.ImportOfflineRenewalFile;

public sealed class ImportOfflineRenewalFileHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ImportSignedEntitlementBundleHandler _bundleImportHandler;

    public ImportOfflineRenewalFileHandler(
        ImportSignedEntitlementBundleHandler bundleImportHandler)
    {
        _bundleImportHandler = bundleImportHandler;
    }

    public async Task<ImportOfflineRenewalFileResult> HandleAsync(
        ImportOfflineRenewalFileCommand command,
        CancellationToken cancellationToken = default)
    {
        var expectedInstallationId = command.ExpectedInstallationId.Trim();

        if (expectedInstallationId.Length == 0)
        {
            return ImportOfflineRenewalFileResult.Failure(
                "InstallationIdRequired",
                "Expected installation id is required before importing an offline renewal file.");
        }

        if (string.IsNullOrWhiteSpace(command.RenewalFileJson))
        {
            return ImportOfflineRenewalFileResult.Failure(
                "OfflineRenewalFileRequired",
                "Offline renewal file content is required.");
        }

        ControlCloudOfflineRenewalFileResponse renewalFile;

        try
        {
            renewalFile = JsonSerializer.Deserialize<ControlCloudOfflineRenewalFileResponse>(
                command.RenewalFileJson,
                JsonOptions) ?? throw new JsonException("Offline renewal file JSON was empty.");
        }
        catch (JsonException exception)
        {
            return ImportOfflineRenewalFileResult.Failure(
                "OfflineRenewalFileInvalid",
                $"Offline renewal file JSON could not be parsed: {exception.Message}");
        }

        if (!string.Equals(
                renewalFile.FormatVersion,
                ControlCloudOfflineRenewalFileFormat.Version,
                StringComparison.Ordinal))
        {
            return ImportOfflineRenewalFileResult.Failure(
                "OfflineRenewalFileVersionUnsupported",
                $"Offline renewal file format '{renewalFile.FormatVersion}' is not supported.");
        }

        var renewalInstallationId = renewalFile.InstallationId?.Trim() ?? "";

        if (!string.Equals(
                renewalInstallationId,
                expectedInstallationId,
                StringComparison.Ordinal))
        {
            return ImportOfflineRenewalFileResult.Failure(
                "InstallationMismatch",
                "Offline renewal file belongs to a different installation.");
        }

        if (renewalFile.SignedBundle is null)
        {
            return ImportOfflineRenewalFileResult.Failure(
                "OfflineRenewalBundleRequired",
                "Offline renewal file does not contain a signed entitlement bundle.");
        }

        var importResult = await _bundleImportHandler.HandleAsync(
            new ImportSignedEntitlementBundleCommand(
                expectedInstallationId,
                renewalFile.SignedBundle,
                LocalServerEntitlementImportSources.OfflineRenewalFile),
            cancellationToken);

        if (!importResult.IsSuccess)
        {
            return ImportOfflineRenewalFileResult.Failure(
                importResult.FailureCode ?? "OfflineRenewalImportFailed",
                importResult.Detail ?? "Offline renewal file could not be imported.");
        }

        return ImportOfflineRenewalFileResult.Success(
            importResult.Entitlement!,
            renewalFile.RenewalFileId,
            renewalFile.GeneratedBy ?? "",
            renewalFile.Reason ?? "",
            renewalFile.GeneratedAtUtc);
    }
}

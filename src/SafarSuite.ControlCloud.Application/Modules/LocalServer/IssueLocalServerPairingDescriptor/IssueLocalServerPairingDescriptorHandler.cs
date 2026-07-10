using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerPairingDescriptor;

public sealed class IssueLocalServerPairingDescriptorHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IControlCloudInstallationSetupTokenRepository _setupTokens;
    private readonly IControlCloudAppActivationIssueRepository _activationIssues;
    private readonly IControlCloudBootstrapPackageSigner _signer;
    private readonly IControlCloudClock _clock;

    public IssueLocalServerPairingDescriptorHandler(
        IControlCloudInstallationSetupTokenRepository setupTokens,
        IControlCloudAppActivationIssueRepository activationIssues,
        IControlCloudBootstrapPackageSigner signer,
        IControlCloudClock clock)
    {
        _setupTokens = setupTokens;
        _activationIssues = activationIssues;
        _signer = signer;
        _clock = clock;
    }

    public async Task<IssueLocalServerPairingDescriptorResult> HandleAsync(
        IssueLocalServerPairingDescriptorCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = OptionalText(command.InstallationId);

        if (command.ClientId == Guid.Empty)
        {
            return IssueLocalServerPairingDescriptorResult.Failure(
                "ClientIdRequired",
                "Client id is required before issuing a pairing descriptor.");
        }

        if (installationId is null)
        {
            return IssueLocalServerPairingDescriptorResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before issuing a pairing descriptor.");
        }

        if (installationId.Length > 160)
        {
            return IssueLocalServerPairingDescriptorResult.Failure(
                "InstallationIdInvalid",
                "Installation id cannot exceed 160 characters.");
        }

        var appServerInstallationId = ParseOptionalGuid(command.AppServerInstallationId);
        if (appServerInstallationId.IsInvalid)
        {
            return IssueLocalServerPairingDescriptorResult.Failure(
                "AppServerInstallationIdInvalid",
                "App server installation id must be a valid GUID.");
        }

        var package = await ResolveBootstrapPackageAsync(
            command.ClientId,
            installationId,
            command.BootstrapPackageId,
            command.SetupTokenId,
            cancellationToken);
        if (package is null)
        {
            return IssueLocalServerPairingDescriptorResult.Failure(
                "BootstrapPackageNotFound",
                "Bootstrap package was not found for this client installation.");
        }

        var now = _clock.UtcNow;
        var activeIssue = await ResolveActiveAppActivationIssueAsync(
            command.ClientId,
            installationId,
            appServerInstallationId.Value,
            now,
            cancellationToken);
        var resolvedAppServerInstallationId =
            activeIssue?.AppServerInstallationId.ToString("D")
            ?? appServerInstallationId.Value?.ToString("D");
        var resolvedFingerprintHash =
            activeIssue?.FingerprintHash
            ?? OptionalText(command.FingerprintHash);
        var urlCandidates = NormalizeUrlCandidates(command.UrlCandidates);
        if (urlCandidates.Count == 0)
        {
            urlCandidates = BuildDefaultUrlCandidates(package.DeploymentProfile);
        }

        var unsignedDescriptor = new LocalServerPairingDescriptorResponse(
            LocalServerPairingFormats.PairingDescriptorVersion,
            command.ClientId,
            installationId,
            package.BootstrapPackageId,
            package.SetupTokenId,
            BuildDisplayName(command.ClientCode, package),
            resolvedAppServerInstallationId,
            package.DeploymentProfile.SiteId,
            package.DeploymentProfile.SiteRole,
            OptionalText(command.ClientCode),
            OptionalText(command.CustomerName),
            package.DeploymentProfile.BranchCode ?? package.DeploymentProfile.SiteId,
            resolvedFingerprintHash,
            OptionalText(command.TlsCaSha256),
            OptionalText(command.TlsCertificateSha256),
            OptionalText(command.ServerPairingKeySha256),
            urlCandidates,
            now,
            package.ExpiresAtUtc,
            "ControlCloudPairingDescriptor",
            package.PackageBundleSha256,
            _signer.SigningKeyId,
            Notes:
            [
                "This descriptor was issued by Control Cloud using the bootstrap signing key lane.",
                "It does not contain setup-token plaintext, provider credentials, database credentials, or activation tokens.",
                "The SafarSuite Windows app must still validate the live LocalServer identity and require fingerprint confirmation before trust is written."
            ]);
        var payloadJson = JsonSerializer.Serialize(unsignedDescriptor, JsonOptions);
        var signature = _signer.SignPayloadJson(payloadJson);

        return IssueLocalServerPairingDescriptorResult.Success(
            unsignedDescriptor with
            {
                SignatureAlgorithm = signature.Algorithm,
                SignatureKeyId = signature.KeyId,
                PayloadSha256 = signature.PayloadSha256,
                Signature = signature.Value
            });
    }

    private async Task<ControlCloudInstallationSetupToken?> ResolveBootstrapPackageAsync(
        Guid clientId,
        string installationId,
        Guid? bootstrapPackageId,
        Guid? setupTokenId,
        CancellationToken cancellationToken)
    {
        var packages = await _setupTokens.ListBootstrapPackagesAsync(
            clientId,
            installationId,
            200,
            cancellationToken);

        return packages
            .Where(package => package.BootstrapPackageId.HasValue)
            .Where(package => bootstrapPackageId is null || package.BootstrapPackageId == bootstrapPackageId)
            .Where(package => setupTokenId is null || package.SetupTokenId == setupTokenId)
            .OrderByDescending(package => package.BootstrapPackageGeneratedAtUtc ?? package.CreatedAtUtc)
            .FirstOrDefault();
    }

    private async Task<ControlCloudAppActivationIssue?> ResolveActiveAppActivationIssueAsync(
        Guid clientId,
        string installationId,
        Guid? appServerInstallationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var issues = await _activationIssues.ListAsync(
            clientId,
            installationId,
            appServerInstallationId,
            query: null,
            take: 50,
            cancellationToken);

        return issues
            .Where(issue => issue.Status == ControlCloudAppActivationIssueStatuses.Issued)
            .Where(issue => issue.ExpiresAtUtc > now)
            .OrderByDescending(issue => issue.IssuedAtUtc)
            .FirstOrDefault();
    }

    private static IReadOnlyCollection<string> NormalizeUrlCandidates(
        IReadOnlyCollection<string>? candidates)
    {
        var result = new List<string>();

        foreach (var candidate in candidates ?? Array.Empty<string>())
        {
            var normalized = NormalizeUrlCandidate(candidate);
            if (normalized is not null
                && !result.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(normalized);
            }

            if (result.Count >= 20)
            {
                break;
            }
        }

        return result;
    }

    private static IReadOnlyCollection<string> BuildDefaultUrlCandidates(
        ControlCloudInstallationDeploymentProfile deploymentProfile)
    {
        var candidates = new List<string>
        {
            "http://localhost:5280",
            "http://127.0.0.1:5280"
        };
        if (!string.IsNullOrWhiteSpace(deploymentProfile.SiteId))
        {
            candidates.Add($"http://safarsuite-{deploymentProfile.SiteId.Trim()}.lan:5280");
        }

        if (!string.IsNullOrWhiteSpace(deploymentProfile.BranchCode))
        {
            candidates.Add($"http://safarsuite-{deploymentProfile.BranchCode.Trim()}.lan:5280");
        }

        return NormalizeUrlCandidates(candidates);
    }

    private static string? NormalizeUrlCandidate(string? value)
    {
        var normalized = OptionalText(value)?.TrimEnd('/');
        if (normalized is null)
        {
            return null;
        }

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : null;
    }

    private static string BuildDisplayName(
        string? clientCode,
        ControlCloudInstallationSetupToken package)
    {
        var clientLabel = OptionalText(clientCode) ?? package.ClientId.ToString("D");
        var siteLabel =
            OptionalText(package.DeploymentProfile.BranchCode)
            ?? OptionalText(package.DeploymentProfile.SiteId)
            ?? package.InstallationId;

        return $"{clientLabel} - {siteLabel}";
    }

    private static OptionalGuid ParseOptionalGuid(string? value)
    {
        var normalized = OptionalText(value);
        if (normalized is null)
        {
            return new OptionalGuid(null, IsInvalid: false);
        }

        return Guid.TryParse(normalized, out var parsed) && parsed != Guid.Empty
            ? new OptionalGuid(parsed, IsInvalid: false)
            : new OptionalGuid(null, IsInvalid: true);
    }

    private static string? OptionalText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private readonly record struct OptionalGuid(Guid? Value, bool IsInvalid);
}

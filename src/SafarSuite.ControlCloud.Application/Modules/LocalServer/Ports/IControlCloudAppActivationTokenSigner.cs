using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudAppActivationTokenSigner
{
    string SigningKeyId { get; }

    string PublicKeyPem { get; }

    SafarSuiteAppActivationSignedToken Sign(SafarSuiteAppActivationTokenClaims claims);
}

public sealed record SafarSuiteAppActivationSignedToken(
    string ActivationToken,
    string Signature);

public sealed record SafarSuiteAppActivationTokenClaims(
    string Version,
    string TokenUse,
    Guid ActivationRequestId,
    Guid ServerInstallationId,
    string FingerprintHash,
    string ServerPublicKey,
    Guid TenantId,
    Guid BranchId,
    string CustomerCode,
    string CustomerName,
    string BranchName,
    string EntitlementStatus,
    DateOnly PaidUntil,
    DateOnly GraceEndsOn,
    DateOnly OfflineValidUntil,
    IReadOnlyDictionary<string, bool> ModuleEntitlements,
    string SigningKeyId,
    DateTimeOffset IssuedAt,
    DateTimeOffset NotBefore,
    DateTimeOffset ExpiresAt);

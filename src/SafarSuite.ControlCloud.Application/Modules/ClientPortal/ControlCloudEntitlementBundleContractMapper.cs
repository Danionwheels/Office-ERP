using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public static class ControlCloudEntitlementBundleContractMapper
{
    public static ClientPortalSignedEntitlementBundleResponse ToResponse(
        ControlCloudSignedEntitlementBundle bundle)
    {
        return new ClientPortalSignedEntitlementBundleResponse(
            bundle.PayloadJson,
            new ClientPortalEntitlementBundlePayloadResponse(
                bundle.Payload.BundleVersion,
                bundle.Payload.Issuer,
                bundle.Payload.Audience,
                bundle.Payload.ClientId,
                bundle.Payload.InstallationId,
                bundle.Payload.EntitlementVersion,
                bundle.Payload.BundleIssueId,
                bundle.Payload.EntitlementSnapshotId,
                bundle.Payload.ClientAccessRevisionId,
                bundle.Payload.ContractId,
                bundle.Payload.ContractRevisionNumber,
                bundle.Payload.ProductCatalogRevisionId,
                bundle.Payload.ProductCatalogRevisionNumber,
                bundle.Payload.SourceInvoiceId,
                bundle.Payload.SourceInvoiceNumber,
                bundle.Payload.Status,
                bundle.Payload.BundleIssuedAtUtc,
                bundle.Payload.EntitlementIssuedAtUtc,
                bundle.Payload.ValidFrom,
                bundle.Payload.PaidUntil,
                bundle.Payload.WarningStartsAt,
                bundle.Payload.GraceUntil,
                bundle.Payload.OfflineValidUntil,
                bundle.Payload.AllowedDevices,
                bundle.Payload.AllowedBranches,
                bundle.Payload.Modules
                    .OrderBy(module => module.ModuleCode, StringComparer.Ordinal)
                    .Select(module => new ClientPortalEntitlementBundleModuleResponse(
                        module.ModuleCode,
                        module.Status,
                        module.IsEnabled))
                    .ToArray(),
                bundle.Payload.AllowedNamedUsers,
                bundle.Payload.AllowedConcurrentUsers,
                (bundle.Payload.FeatureLimits ?? [])
                    .OrderBy(limit => limit.ModuleCode, StringComparer.Ordinal)
                    .ThenBy(limit => limit.FeatureCode, StringComparer.Ordinal)
                    .Select(limit => new ClientPortalEntitlementBundleFeatureLimitResponse(
                        limit.ModuleCode,
                        limit.FeatureCode,
                        limit.LimitValue,
                        limit.Unit))
                    .ToArray(),
                bundle.Payload.EffectiveFromUtc),
            new ClientPortalEntitlementBundleSignatureResponse(
                bundle.Signature.Algorithm,
                bundle.Signature.KeyId,
                bundle.Signature.PayloadSha256,
                bundle.Signature.Value));
    }
}

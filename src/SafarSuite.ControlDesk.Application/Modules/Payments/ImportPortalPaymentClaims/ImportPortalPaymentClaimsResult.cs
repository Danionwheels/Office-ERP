using SafarSuite.ControlDesk.Application.Modules.Payments.Common;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ImportPortalPaymentClaims;

public sealed record ImportPortalPaymentClaimsResult(
    Guid ClientId,
    int RetrievedCount,
    int ImportedCount,
    int AlreadyImportedCount,
    int IgnoredCount,
    IReadOnlyCollection<PortalPaymentClaimResult> Claims);

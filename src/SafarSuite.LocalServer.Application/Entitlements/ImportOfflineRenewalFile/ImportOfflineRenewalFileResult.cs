using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.ImportOfflineRenewalFile;

public sealed record ImportOfflineRenewalFileResult(
    LocalServerCachedEntitlement? Entitlement,
    Guid? RenewalFileId,
    string? GeneratedBy,
    string? Reason,
    DateTimeOffset? GeneratedAtUtc,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Entitlement is not null;

    public static ImportOfflineRenewalFileResult Success(
        LocalServerCachedEntitlement entitlement,
        Guid renewalFileId,
        string generatedBy,
        string reason,
        DateTimeOffset generatedAtUtc)
    {
        return new ImportOfflineRenewalFileResult(
            entitlement,
            renewalFileId,
            generatedBy,
            reason,
            generatedAtUtc,
            FailureCode: null,
            Detail: null);
    }

    public static ImportOfflineRenewalFileResult Failure(
        string failureCode,
        string detail)
    {
        return new ImportOfflineRenewalFileResult(
            Entitlement: null,
            RenewalFileId: null,
            GeneratedBy: null,
            Reason: null,
            GeneratedAtUtc: null,
            failureCode,
            detail);
    }
}

using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed record JournalLineLedgerAccountMetadata(
    string? LedgerAccountCode,
    string? LedgerAccountName,
    string? LedgerAccountType,
    string? LedgerAccountNormalBalance,
    string? LedgerAccountLevel,
    bool? IsPostingAccount,
    string? LedgerAccountStatus);

public static class JournalLineLedgerAccountMetadataFactory
{
    public static JournalLineLedgerAccountMetadata From(
        LedgerAccountId ledgerAccountId,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById)
    {
        if (ledgerAccountsById is null
            || !ledgerAccountsById.TryGetValue(ledgerAccountId.Value, out var ledgerAccount))
        {
            return Empty;
        }

        return new JournalLineLedgerAccountMetadata(
            ledgerAccount.Code.Value,
            ledgerAccount.Name,
            ledgerAccount.Type.ToString(),
            ledgerAccount.NormalBalance.ToString(),
            ledgerAccount.Level.ToString(),
            ledgerAccount.IsPostingAccount,
            ledgerAccount.Status.ToString());
    }

    public static IReadOnlyDictionary<Guid, LedgerAccount> ToLookup(
        IEnumerable<LedgerAccount> ledgerAccounts)
    {
        return ledgerAccounts.ToDictionary(account => account.Id.Value);
    }

    public static IReadOnlyDictionary<Guid, LedgerAccount> EmptyLookup { get; } =
        new Dictionary<Guid, LedgerAccount>();

    private static JournalLineLedgerAccountMetadata Empty { get; } = new(
        null,
        null,
        null,
        null,
        null,
        null,
        null);
}

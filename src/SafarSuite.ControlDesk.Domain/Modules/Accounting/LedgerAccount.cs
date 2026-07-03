using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class LedgerAccount : Entity<LedgerAccountId>
{
    private LedgerAccount(
        LedgerAccountId id,
        LedgerAccountCode code,
        string name,
        LedgerAccountType type,
        NormalBalance normalBalance,
        LedgerAccountLevel level,
        LedgerAccountId? parentAccountId,
        bool isPostingAccount,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Code = code;
        Name = name;
        Type = type;
        NormalBalance = normalBalance;
        Level = level;
        ParentAccountId = parentAccountId;
        IsPostingAccount = isPostingAccount;
        CreatedAtUtc = createdAtUtc;
        Status = LedgerAccountStatus.Active;
    }

    public LedgerAccountCode Code { get; }

    public string Name { get; private set; }

    public LedgerAccountType Type { get; }

    public NormalBalance NormalBalance { get; }

    public LedgerAccountLevel Level { get; }

    public LedgerAccountId? ParentAccountId { get; }

    public bool IsPostingAccount { get; private set; }

    public LedgerAccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static LedgerAccount Create(
        LedgerAccountId id,
        LedgerAccountCode code,
        string name,
        LedgerAccountType type,
        NormalBalance normalBalance,
        LedgerAccountLevel level,
        LedgerAccountId? parentAccountId,
        bool isPostingAccount,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ledger account name is required.", nameof(name));
        }

        if (RequiresNonPostingAccount(level) && isPostingAccount)
        {
            throw new ArgumentException(
                $"{level} accounts cannot be posting accounts.",
                nameof(isPostingAccount));
        }

        if (RequiresPostingAccount(level) && !isPostingAccount)
        {
            throw new ArgumentException(
                $"{level} accounts must be posting accounts.",
                nameof(isPostingAccount));
        }

        return new LedgerAccount(
            id,
            code,
            name.Trim(),
            type,
            normalBalance,
            level,
            parentAccountId,
            isPostingAccount,
            createdAtUtc);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ledger account name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void SetPostingAccount(bool isPostingAccount)
    {
        if (RequiresNonPostingAccount(Level) && isPostingAccount)
        {
            throw new ArgumentException(
                $"{Level} accounts cannot be posting accounts.",
                nameof(isPostingAccount));
        }

        if (RequiresPostingAccount(Level) && !isPostingAccount)
        {
            throw new ArgumentException(
                $"{Level} accounts must be posting accounts.",
                nameof(isPostingAccount));
        }

        IsPostingAccount = isPostingAccount;
    }

    public void Activate()
    {
        Status = LedgerAccountStatus.Active;
    }

    public void Deactivate()
    {
        Status = LedgerAccountStatus.Inactive;
    }

    private static bool RequiresNonPostingAccount(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Header
            or LedgerAccountLevel.Total
            or LedgerAccountLevel.Master
            or LedgerAccountLevel.Control;
    }

    private static bool RequiresPostingAccount(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Detail
            or LedgerAccountLevel.Subsidiary;
    }
}

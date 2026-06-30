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
        LedgerAccountId? parentAccountId,
        bool isPostingAccount,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Code = code;
        Name = name;
        Type = type;
        NormalBalance = normalBalance;
        ParentAccountId = parentAccountId;
        IsPostingAccount = isPostingAccount;
        CreatedAtUtc = createdAtUtc;
        Status = LedgerAccountStatus.Active;
    }

    public LedgerAccountCode Code { get; }

    public string Name { get; private set; }

    public LedgerAccountType Type { get; }

    public NormalBalance NormalBalance { get; }

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
        LedgerAccountId? parentAccountId,
        bool isPostingAccount,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ledger account name is required.", nameof(name));
        }

        return new LedgerAccount(
            id,
            code,
            name.Trim(),
            type,
            normalBalance,
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
}

using SafarSuite.ControlDesk.Domain.SharedKernel;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class JournalEntry : Entity<JournalEntryId>
{
    private readonly List<JournalLine> _lines = [];

    private JournalEntry(
        JournalEntryId id,
        DateOnly entryDate,
        string currencyCode,
        JournalSourceType sourceType,
        string? sourceReference,
        string? memo,
        DateTimeOffset createdAtUtc,
        ClientId? clientId,
        Guid? sourceDocumentId)
        : base(id)
    {
        EntryDate = entryDate;
        CurrencyCode = currencyCode;
        SourceType = sourceType;
        SourceReference = sourceReference;
        Memo = memo;
        CreatedAtUtc = createdAtUtc;
        ClientId = clientId;
        SourceDocumentId = sourceDocumentId;
        Status = JournalEntryStatus.Draft;
    }

    public DateOnly EntryDate { get; }

    public string CurrencyCode { get; }

    public JournalSourceType SourceType { get; }

    public string? SourceReference { get; }

    public string? Memo { get; }

    public ClientId? ClientId { get; }

    public Guid? SourceDocumentId { get; }

    public JournalEntryStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? PostedAtUtc { get; private set; }

    public DateTimeOffset? VoidedAtUtc { get; private set; }

    public IReadOnlyCollection<JournalLine> Lines => _lines.AsReadOnly();

    public Money TotalDebit => _lines.Aggregate(Money.Zero(CurrencyCode), (total, line) => total.Add(line.Debit));

    public Money TotalCredit => _lines.Aggregate(Money.Zero(CurrencyCode), (total, line) => total.Add(line.Credit));

    public static JournalEntry Create(
        JournalEntryId id,
        DateOnly entryDate,
        string currencyCode,
        JournalSourceType sourceType,
        string? sourceReference,
        string? memo,
        DateTimeOffset createdAtUtc,
        ClientId? clientId = null,
        Guid? sourceDocumentId = null)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Journal currency code is required.", nameof(currencyCode));
        }

        return new JournalEntry(
            id,
            entryDate,
            currencyCode.Trim().ToUpperInvariant(),
            sourceType,
            CleanText(sourceReference),
            CleanText(memo),
            createdAtUtc,
            clientId,
            sourceDocumentId);
    }

    public void AddLine(JournalLine line)
    {
        EnsureDraft();

        if (!string.Equals(line.Debit.CurrencyCode, CurrencyCode, StringComparison.Ordinal)
            || !string.Equals(line.Credit.CurrencyCode, CurrencyCode, StringComparison.Ordinal))
        {
            throw new ArgumentException("Journal line currency must match the journal entry currency.", nameof(line));
        }

        _lines.Add(line);
    }

    public void Post(DateTimeOffset postedAtUtc)
    {
        EnsureDraft();

        if (_lines.Count < 2)
        {
            throw new InvalidOperationException("Journal entry must have at least two lines before posting.");
        }

        if (TotalDebit.Amount <= 0 || TotalCredit.Amount <= 0)
        {
            throw new InvalidOperationException("Journal entry must include debit and credit amounts before posting.");
        }

        if (TotalDebit.Amount != TotalCredit.Amount)
        {
            throw new InvalidOperationException("Journal entry debits and credits must balance before posting.");
        }

        Status = JournalEntryStatus.Posted;
        PostedAtUtc = postedAtUtc;
    }

    public void Void(DateTimeOffset voidedAtUtc)
    {
        if (Status == JournalEntryStatus.Voided)
        {
            return;
        }

        if (Status != JournalEntryStatus.Posted)
        {
            throw new InvalidOperationException("Only posted journal entries can be voided.");
        }

        Status = JournalEntryStatus.Voided;
        VoidedAtUtc = voidedAtUtc;
    }

    private void EnsureDraft()
    {
        if (Status != JournalEntryStatus.Draft)
        {
            throw new InvalidOperationException("Only draft journal entries can be changed.");
        }
    }

    private static string? CleanText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

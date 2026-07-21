namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IJournalEntryRegisterReader
{
    Task<JournalEntryRegisterReadPage> ReadPageAsync(
        JournalEntryRegisterReadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record JournalEntryRegisterReadRequest(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string Search,
    string? SourceType,
    DateOnly? AfterEntryDate,
    DateTimeOffset? AfterCreatedAtUtc,
    Guid? AfterJournalEntryId,
    int Take);

public sealed record JournalEntryRegisterReadPage(
    IReadOnlyCollection<JournalEntryRegisterReadItem> Items,
    long FilteredCount);

public sealed record JournalEntryRegisterReadItem(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount,
    DateTimeOffset CreatedAtUtc);

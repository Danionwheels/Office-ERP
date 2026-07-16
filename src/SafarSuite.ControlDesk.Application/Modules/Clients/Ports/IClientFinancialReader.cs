using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

public interface IClientFinancialReader
{
    Task<bool> ClientExistsAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default);

    Task<ClientFinancialSummaryReadModel> ReadSummaryAsync(
        ClientId clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<ClientInvoiceReadPage> ReadInvoicePageAsync(
        ClientInvoiceReadRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientPaymentReadPage> ReadPaymentPageAsync(
        ClientPaymentReadRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientFinancialActivityReadPage> ReadActivityPageAsync(
        ClientFinancialActivityReadRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientJournalPostingReadPage> ReadJournalPageAsync(
        ClientJournalPostingReadRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientCreditBalanceReadModel> ReadCreditBalanceAsync(
        ClientId clientId,
        string currencyCode,
        CancellationToken cancellationToken = default);
}

public enum ClientInvoiceRegisterState
{
    All,
    Open,
    Paid,
    Draft,
    Void
}

public sealed record ClientFinancialSummaryReadModel(
    IReadOnlyCollection<ClientFinancialCurrencySummaryReadModel> Currencies);

public sealed record ClientFinancialCurrencySummaryReadModel(
    string CurrencyCode,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal AvailableCredit,
    decimal BalanceDue,
    long InvoiceCount,
    long OpenInvoiceCount);

public sealed record ClientInvoiceReadRequest(
    ClientId ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string Search,
    ClientInvoiceRegisterState State,
    DateOnly? AfterIssueDate,
    DateTimeOffset? AfterCreatedAtUtc,
    Guid? AfterInvoiceId,
    int Take);

public sealed record ClientInvoiceReadPage(
    IReadOnlyCollection<ClientInvoiceReadItem> Items,
    long FilteredCount);

public sealed record ClientInvoiceReadItem(
    Guid InvoiceId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string CurrencyCode,
    Guid? JournalEntryId,
    DateTimeOffset CreatedAtUtc);

public sealed record ClientPaymentReadRequest(
    ClientId ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string Search,
    string? Status,
    DateOnly? AfterReceivedOn,
    DateTimeOffset? AfterRecordedAtUtc,
    Guid? AfterPaymentId,
    int Take);

public sealed record ClientPaymentReadPage(
    IReadOnlyCollection<ClientPaymentReadItem> Items,
    long FilteredCount);

public sealed record ClientPaymentReadItem(
    Guid PaymentId,
    Guid InvoiceId,
    string Reference,
    string Method,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid? JournalEntryId,
    DateTimeOffset RecordedAtUtc);

public sealed record ClientFinancialActivityReadRequest(
    ClientId ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string Search,
    DateOnly? AfterEntryDate,
    int? AfterSortOrder,
    string? AfterReference,
    Guid? AfterDocumentId,
    int Take);

public sealed record ClientFinancialActivityReadPage(
    IReadOnlyCollection<ClientFinancialActivityReadItem> Items,
    long FilteredCount);

public sealed record ClientFinancialActivityReadItem(
    DateOnly EntryDate,
    int SortOrder,
    string DocumentType,
    string Reference,
    Guid DocumentId,
    Guid? InvoiceId,
    Guid? PaymentId,
    Guid? RefundId,
    Guid? CreditApplicationId,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string CurrencyCode,
    Guid? JournalEntryId);

public sealed record ClientJournalPostingReadRequest(
    ClientId ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string Search,
    string? SourceType,
    DateOnly? AfterEntryDate,
    DateTimeOffset? AfterCreatedAtUtc,
    Guid? AfterJournalEntryId,
    int Take);

public sealed record ClientJournalPostingReadPage(
    IReadOnlyCollection<ClientJournalPostingReadItem> Items,
    long FilteredCount);

public sealed record ClientJournalPostingReadItem(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    int LineCount,
    DateTimeOffset CreatedAtUtc);

public sealed record ClientCreditBalanceReadModel(
    string CurrencyCode,
    decimal InvoiceBalance,
    decimal CreditNoteAmount,
    decimal RefundAmount,
    decimal AppliedCreditAmount);

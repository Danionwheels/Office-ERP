namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Clients;

public sealed record CreateClientRequest(
    string Code,
    string LegalName,
    string? DisplayName = null);

public sealed record CreateClientResponse(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status);

public sealed record ListClientsResponse(
    IReadOnlyCollection<ClientLookupResponse> Clients);

public sealed record ClientLookupResponse(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status);

public sealed record ClientDetailsResponse(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    IReadOnlyCollection<ClientContactResponse> Contacts,
    IReadOnlyCollection<ClientSupportNoteResponse> SupportNotes);

public sealed record UpdateClientRequest(
    string LegalName,
    string? DisplayName = null);

public sealed record AddClientContactRequest(
    string Role,
    string FullName,
    string? JobTitle,
    string? Email,
    string? Phone,
    bool IsPrimary = false);

public sealed record AddClientContactResponse(
    Guid ClientId,
    ClientContactResponse Contact);

public sealed record ListClientContactsResponse(
    Guid ClientId,
    IReadOnlyCollection<ClientContactResponse> Contacts);

public sealed record ClientContactResponse(
    Guid ClientContactId,
    string Role,
    string FullName,
    string? JobTitle,
    string? Email,
    string? Phone,
    bool IsPrimary,
    DateTimeOffset CreatedAtUtc);

public sealed record AddClientSupportNoteRequest(
    string Text,
    string CreatedBy);

public sealed record AddClientSupportNoteResponse(
    Guid ClientId,
    ClientSupportNoteResponse SupportNote);

public sealed record ListClientSupportNotesResponse(
    Guid ClientId,
    IReadOnlyCollection<ClientSupportNoteResponse> SupportNotes);

public sealed record ClientSupportNoteResponse(
    string Text,
    string CreatedBy,
    DateTimeOffset CreatedAtUtc);

public sealed record ConfigureClientAccountingProfileRequest(
    Guid AccountsReceivableAccountId,
    string DefaultCurrencyCode,
    string? CloudCustomerId = null);

public sealed record ClientAccountingProfileResponse(
    Guid ClientId,
    Guid AccountsReceivableAccountId,
    string DefaultCurrencyCode,
    string? CloudCustomerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ClientStatementResponse(
    Guid ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyCollection<ClientStatementCurrencySummaryResponse> CurrencySummaries,
    IReadOnlyCollection<ClientStatementInvoiceResponse> Invoices,
    IReadOnlyCollection<ClientStatementPaymentResponse> Payments,
    IReadOnlyCollection<ClientStatementLineResponse> Lines,
    IReadOnlyCollection<ClientStatementJournalPostingResponse> JournalPostings);

public sealed record ClientStatementCurrencySummaryResponse(
    string CurrencyCode,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal BalanceDue,
    int InvoiceCount,
    int OpenInvoiceCount);

public sealed record ClientStatementInvoiceResponse(
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
    Guid? JournalEntryId);

public sealed record ClientStatementPaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string Reference,
    string Method,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid? JournalEntryId);

public sealed record ClientStatementLineResponse(
    DateOnly EntryDate,
    string DocumentType,
    string Reference,
    Guid? InvoiceId,
    Guid? PaymentId,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string CurrencyCode,
    Guid? JournalEntryId);

public sealed record ClientStatementJournalPostingResponse(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<ClientStatementJournalLineResponse> Lines);

public sealed record ClientStatementJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);

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
    IReadOnlyCollection<ClientLookupResponse> Clients,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount,
    ClientDirectorySummaryResponse Summary);

public sealed record ClientDirectorySummaryResponse(
    long TotalCount,
    long DraftCount,
    long ActiveCount,
    long SuspendedCount,
    long ArchivedCount);

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

public sealed record InviteClientPortalContactRequest(
    string? PortalRole = null,
    int ExpiresInDays = 7,
    string CreatedBy = "SafarSuite Control Desk");

public sealed record InviteClientPortalContactResponse(
    Guid InvitationId,
    Guid ClientId,
    Guid ClientContactId,
    string Email,
    string FullName,
    string Role,
    string Status,
    DateTimeOffset InvitedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? InvitationToken,
    string? InvitationUrl);

public sealed record ClientPortalInvitationResponse(
    Guid InvitationId,
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    string Status,
    DateTimeOffset InvitedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? InvitationToken,
    string? InvitationUrl);

public sealed record ListClientPortalInvitationsResponse(
    Guid ClientId,
    IReadOnlyCollection<ClientPortalInvitationResponse> Invitations);

public sealed record ResendClientPortalInvitationRequest(
    int ExpiresInDays = 7,
    string CreatedBy = "SafarSuite Control Desk");

public sealed record RevokeClientPortalInvitationRequest(
    string RevokedBy = "SafarSuite Control Desk");

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

public sealed record ConfigureClientDeploymentRequest(
    string DisplayName,
    string BootstrapMode,
    string ClientDeploymentMode,
    string SiteId,
    string SiteRole,
    string? ParentSiteId = null,
    string? BranchCode = null,
    string? SyncTopologyId = null,
    string LocalServerVersion = "latest",
    string? SafarSuiteAppVersion = null,
    bool IsPrimary = true);

public sealed record ClientDeploymentResponse(
    Guid ClientDeploymentId,
    Guid ClientId,
    string DisplayName,
    string InstallationId,
    string BootstrapMode,
    string ClientDeploymentMode,
    string SiteId,
    string SiteRole,
    string? ParentSiteId,
    string? BranchCode,
    string? SyncTopologyId,
    string LocalServerVersion,
    string SafarSuiteAppVersion,
    bool IsPrimary,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ListClientDeploymentsResponse(
    Guid ClientId,
    IReadOnlyCollection<ClientDeploymentResponse> Deployments);

public sealed record ClientFinancialSummaryResponse(
    Guid ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyCollection<ClientFinancialCurrencySummaryResponse> CurrencySummaries);

public sealed record ClientFinancialCurrencySummaryResponse(
    string CurrencyCode,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal AvailableCredit,
    decimal BalanceDue,
    long InvoiceCount,
    long OpenInvoiceCount);

public sealed record ClientInvoiceRegisterPageResponse(
    IReadOnlyCollection<ClientInvoiceRegisterItemResponse> Invoices,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientInvoiceRegisterItemResponse(
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

public sealed record ClientPaymentRegisterPageResponse(
    IReadOnlyCollection<ClientPaymentRegisterItemResponse> Payments,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientPaymentRegisterItemResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string Reference,
    string Method,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid? JournalEntryId);

public sealed record ClientFinancialActivityPageResponse(
    IReadOnlyCollection<ClientFinancialActivityItemResponse> Lines,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientFinancialActivityItemResponse(
    DateOnly EntryDate,
    string DocumentType,
    string Reference,
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

public sealed record ClientJournalPostingPageResponse(
    IReadOnlyCollection<ClientJournalPostingItemResponse> JournalPostings,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientJournalPostingItemResponse(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    int LineCount);

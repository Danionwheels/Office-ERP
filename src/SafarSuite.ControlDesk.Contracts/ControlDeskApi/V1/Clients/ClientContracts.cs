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

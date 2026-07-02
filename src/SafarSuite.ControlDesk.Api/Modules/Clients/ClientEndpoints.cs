using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Clients;
using SafarSuite.ControlDesk.Application.Modules.Clients.ActivateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientContact;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientSupportNote;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;
using SafarSuite.ControlDesk.Application.Modules.Clients.InviteClientPortalContact;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientContacts;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientSupportNotes;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;
using SafarSuite.ControlDesk.Application.Modules.Clients.SuspendClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.UpdateClient;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Clients;

namespace SafarSuite.ControlDesk.Api.Modules.Clients;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/clients")
            .WithTags("Clients");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{clientId:guid}", GetAsync);
        group.MapPut("/{clientId:guid}", UpdateAsync);
        group.MapPost("/{clientId:guid}/activate", ActivateAsync);
        group.MapPost("/{clientId:guid}/suspend", SuspendAsync);
        group.MapPost("/{clientId:guid}/contacts", AddContactAsync);
        group.MapGet("/{clientId:guid}/contacts", ListContactsAsync);
        group.MapPost(
            "/{clientId:guid}/contacts/{clientContactId:guid}/portal-invitation",
            InvitePortalContactAsync);
        group.MapPost("/{clientId:guid}/support-notes", AddSupportNoteAsync);
        group.MapGet("/{clientId:guid}/support-notes", ListSupportNotesAsync);
        group.MapPut("/{clientId:guid}/accounting-profile", ConfigureAccountingProfileAsync);
        group.MapGet("/{clientId:guid}/accounting-profile", GetAccountingProfileAsync);
        group.MapGet("/{clientId:guid}/statement", GetStatementAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateClientRequest request,
        CreateClientHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateClientCommand(
            request.Code,
            request.LegalName,
            request.DisplayName);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new CreateClientResponse(
            result.Value.ClientId,
            result.Value.Code,
            result.Value.LegalName,
            result.Value.DisplayName,
            result.Value.Status);

        return Results.Created($"/api/v1/clients/{response.ClientId}", response);
    }

    private static async Task<IResult> ListAsync(
        ListClientsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ListClientsResponse(
            result.Value.Clients.Select(client => new ClientLookupResponse(
                client.ClientId,
                client.Code,
                client.LegalName,
                client.DisplayName,
                client.Status)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAsync(
        Guid clientId,
        GetClientHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetClientQuery(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToClientDetailsResponse(result.Value));
    }

    private static async Task<IResult> AddContactAsync(
        Guid clientId,
        AddClientContactRequest request,
        AddClientContactHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AddClientContactCommand(
                clientId,
                request.Role,
                request.FullName,
                request.JobTitle,
                request.Email,
                request.Phone,
                request.IsPrimary),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new AddClientContactResponse(
            clientId,
            ToClientContactResponse(result.Value));

        return Results.Created($"/api/v1/clients/{clientId}/contacts", response);
    }

    private static async Task<IResult> ListContactsAsync(
        Guid clientId,
        ListClientContactsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListClientContactsQuery(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListClientContactsResponse(
            result.Value.ClientId,
            result.Value.Contacts.Select(ToClientContactResponse).ToArray()));
    }

    private static async Task<IResult> InvitePortalContactAsync(
        Guid clientId,
        Guid clientContactId,
        InviteClientPortalContactRequest request,
        InviteClientPortalContactHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new InviteClientPortalContactCommand(
                clientId,
                clientContactId,
                request.PortalRole,
                request.ExpiresInDays,
                request.CreatedBy),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new InviteClientPortalContactResponse(
            result.Value.InvitationId,
            result.Value.ClientId,
            result.Value.ClientContactId,
            result.Value.Email,
            result.Value.FullName,
            result.Value.Role,
            result.Value.Status,
            result.Value.InvitedAtUtc,
            result.Value.ExpiresAtUtc,
            result.Value.InvitationToken,
            result.Value.InvitationUrl));
    }

    private static async Task<IResult> AddSupportNoteAsync(
        Guid clientId,
        AddClientSupportNoteRequest request,
        AddClientSupportNoteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AddClientSupportNoteCommand(clientId, request.Text, request.CreatedBy),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new AddClientSupportNoteResponse(
            clientId,
            ToClientSupportNoteResponse(result.Value));

        return Results.Created($"/api/v1/clients/{clientId}/support-notes", response);
    }

    private static async Task<IResult> ListSupportNotesAsync(
        Guid clientId,
        ListClientSupportNotesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListClientSupportNotesQuery(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListClientSupportNotesResponse(
            result.Value.ClientId,
            result.Value.SupportNotes.Select(ToClientSupportNoteResponse).ToArray()));
    }

    private static async Task<IResult> UpdateAsync(
        Guid clientId,
        UpdateClientRequest request,
        UpdateClientHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateClientCommand(clientId, request.LegalName, request.DisplayName),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToClientDetailsResponse(result.Value));
    }

    private static async Task<IResult> ActivateAsync(
        Guid clientId,
        ActivateClientHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ActivateClientCommand(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToClientDetailsResponse(result.Value));
    }

    private static async Task<IResult> SuspendAsync(
        Guid clientId,
        SuspendClientHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SuspendClientCommand(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToClientDetailsResponse(result.Value));
    }

    private static async Task<IResult> ConfigureAccountingProfileAsync(
        Guid clientId,
        ConfigureClientAccountingProfileRequest request,
        ConfigureClientAccountingProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ConfigureClientAccountingProfileCommand(
            clientId,
            request.AccountsReceivableAccountId,
            request.DefaultCurrencyCode,
            request.CloudCustomerId);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientAccountingProfileResponse(
            result.Value.ClientId,
            result.Value.AccountsReceivableAccountId,
            result.Value.DefaultCurrencyCode,
            result.Value.CloudCustomerId,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc));
    }

    private static async Task<IResult> GetAccountingProfileAsync(
        Guid clientId,
        GetClientAccountingProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetClientAccountingProfileQuery(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientAccountingProfileResponse(
            result.Value.ClientId,
            result.Value.AccountsReceivableAccountId,
            result.Value.DefaultCurrencyCode,
            result.Value.CloudCustomerId,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc));
    }

    private static async Task<IResult> GetStatementAsync(
        Guid clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        GetClientStatementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetClientStatementQuery(clientId, fromDate, toDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientStatementResponse(
            result.Value.ClientId,
            result.Value.FromDate,
            result.Value.ToDate,
            result.Value.CurrencySummaries.Select(summary => new ClientStatementCurrencySummaryResponse(
                summary.CurrencyCode,
                summary.TotalInvoiced,
                summary.TotalPaid,
                summary.AvailableCredit,
                summary.BalanceDue,
                summary.InvoiceCount,
                summary.OpenInvoiceCount)).ToArray(),
            result.Value.Invoices.Select(invoice => new ClientStatementInvoiceResponse(
                invoice.InvoiceId,
                invoice.ContractId,
                invoice.InvoiceNumber,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.Status,
                invoice.TotalAmount,
                invoice.AmountPaid,
                invoice.BalanceDue,
                invoice.CurrencyCode,
                invoice.JournalEntryId)).ToArray(),
            result.Value.Payments.Select(payment => new ClientStatementPaymentResponse(
                payment.PaymentId,
                payment.InvoiceId,
                payment.Reference,
                payment.Method,
                payment.Status,
                payment.Amount,
                payment.CurrencyCode,
                payment.ReceivedOn,
                payment.JournalEntryId)).ToArray(),
            result.Value.Lines.Select(line => new ClientStatementLineResponse(
                line.EntryDate,
                line.DocumentType,
                line.Reference,
                line.InvoiceId,
                line.PaymentId,
                line.RefundId,
                line.CreditApplicationId,
                line.Description,
                line.Debit,
                line.Credit,
                line.RunningBalance,
                line.CurrencyCode,
                line.JournalEntryId)).ToArray(),
            result.Value.JournalPostings.Select(posting => new ClientStatementJournalPostingResponse(
                posting.JournalEntryId,
                posting.EntryDate,
                posting.SourceType,
                posting.SourceReference,
                posting.Memo,
                posting.Status,
                posting.TotalDebit,
                posting.TotalCredit,
                posting.CurrencyCode,
                posting.Lines.Select(line => new ClientStatementJournalLineResponse(
                    line.LedgerAccountId,
                    line.Debit,
                    line.Credit,
                    line.Description)).ToArray())).ToArray()));
    }

    private static ClientDetailsResponse ToClientDetailsResponse(ClientDetailsResult client)
    {
        return new ClientDetailsResponse(
            client.ClientId,
            client.Code,
            client.LegalName,
            client.DisplayName,
            client.Status,
            client.CreatedAtUtc,
            client.ActivatedAtUtc,
            client.SuspendedAtUtc,
            client.Contacts.Select(ToClientContactResponse).ToArray(),
            client.SupportNotes.Select(ToClientSupportNoteResponse).ToArray());
    }

    private static ClientContactResponse ToClientContactResponse(ClientContactResult contact)
    {
        return new ClientContactResponse(
            contact.ClientContactId,
            contact.Role,
            contact.FullName,
            contact.JobTitle,
            contact.Email,
            contact.Phone,
            contact.IsPrimary,
            contact.CreatedAtUtc);
    }

    private static ClientSupportNoteResponse ToClientSupportNoteResponse(ClientSupportNoteResult note)
    {
        return new ClientSupportNoteResponse(
            note.Text,
            note.CreatedBy,
            note.CreatedAtUtc);
    }
}

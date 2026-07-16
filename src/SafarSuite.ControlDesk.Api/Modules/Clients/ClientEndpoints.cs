using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Clients;
using SafarSuite.ControlDesk.Application.Modules.Clients.ActivateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientContact;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientSupportNote;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientDeployment;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.Financials;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.InviteClientPortalContact;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientContacts;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientDeployments;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientPortalInvitations;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientSupportNotes;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;
using SafarSuite.ControlDesk.Application.Modules.Clients.ResendClientPortalInvitation;
using SafarSuite.ControlDesk.Application.Modules.Clients.RevokeClientPortalInvitation;
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
        group.MapGet("/{clientId:guid}/portal-invitations", ListPortalInvitationsAsync);
        group.MapPost(
            "/{clientId:guid}/portal-invitations/{invitationId:guid}/resend",
            ResendPortalInvitationAsync);
        group.MapPost(
            "/{clientId:guid}/portal-invitations/{invitationId:guid}/revoke",
            RevokePortalInvitationAsync);
        group.MapPost("/{clientId:guid}/support-notes", AddSupportNoteAsync);
        group.MapGet("/{clientId:guid}/support-notes", ListSupportNotesAsync);
        group.MapPut("/{clientId:guid}/accounting-profile", ConfigureAccountingProfileAsync);
        group.MapGet("/{clientId:guid}/accounting-profile", GetAccountingProfileAsync);
        group.MapGet("/{clientId:guid}/deployments", ListDeploymentsAsync);
        group.MapPut("/{clientId:guid}/deployments/{installationId}", ConfigureDeploymentAsync);
        group.MapGet("/{clientId:guid}/financial-summary", GetFinancialSummaryAsync);
        group.MapGet("/{clientId:guid}/invoices", ListInvoicesAsync);
        group.MapGet("/{clientId:guid}/payments", ListPaymentsAsync);
        group.MapGet("/{clientId:guid}/financial-activity", ListFinancialActivityAsync);
        group.MapGet("/{clientId:guid}/journal-postings", ListJournalPostingsAsync);

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
        string? search,
        string? status,
        string? sort,
        string? direction,
        int? take,
        string? cursor,
        ListClientsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientsQuery(search, status, sort, direction, take ?? 50, cursor),
            cancellationToken);

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
                client.Status)).ToArray(),
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount,
            new ClientDirectorySummaryResponse(
                result.Value.Summary.TotalCount,
                result.Value.Summary.DraftCount,
                result.Value.Summary.ActiveCount,
                result.Value.Summary.SuspendedCount,
                result.Value.Summary.ArchivedCount));

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

    private static async Task<IResult> ListPortalInvitationsAsync(
        Guid clientId,
        ListClientPortalInvitationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientPortalInvitationsQuery(clientId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListClientPortalInvitationsResponse(
            result.Value.ClientId,
            result.Value.Invitations.Select(ToClientPortalInvitationResponse).ToArray()));
    }

    private static async Task<IResult> ResendPortalInvitationAsync(
        Guid clientId,
        Guid invitationId,
        ResendClientPortalInvitationRequest request,
        ResendClientPortalInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ResendClientPortalInvitationCommand(
                clientId,
                invitationId,
                request.ExpiresInDays,
                request.CreatedBy),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(ToClientPortalInvitationResponse(result.Value));
    }

    private static async Task<IResult> RevokePortalInvitationAsync(
        Guid clientId,
        Guid invitationId,
        RevokeClientPortalInvitationRequest request,
        RevokeClientPortalInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RevokeClientPortalInvitationCommand(
                clientId,
                invitationId,
                request.RevokedBy),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(ToClientPortalInvitationResponse(result.Value));
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

    private static async Task<IResult> ListDeploymentsAsync(
        Guid clientId,
        ListClientDeploymentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListClientDeploymentsQuery(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListClientDeploymentsResponse(
            result.Value.ClientId,
            result.Value.Deployments.Select(ToClientDeploymentResponse).ToArray()));
    }

    private static async Task<IResult> ConfigureDeploymentAsync(
        Guid clientId,
        string installationId,
        ConfigureClientDeploymentRequest request,
        ConfigureClientDeploymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ConfigureClientDeploymentCommand(
                clientId,
                installationId,
                request.DisplayName,
                request.BootstrapMode,
                request.ClientDeploymentMode,
                request.SiteId,
                request.SiteRole,
                request.ParentSiteId,
                request.BranchCode,
                request.SyncTopologyId,
                request.LocalServerVersion,
                request.SafarSuiteAppVersion,
                request.IsPrimary),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(ToClientDeploymentResponse(result.Value));
    }

    private static async Task<IResult> GetFinancialSummaryAsync(
        Guid clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        GetClientFinancialSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetClientFinancialSummaryQuery(clientId, fromDate, toDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientFinancialSummaryResponse(
            result.Value.ClientId,
            result.Value.FromDate,
            result.Value.ToDate,
            result.Value.CurrencySummaries.Select(summary => new ClientFinancialCurrencySummaryResponse(
                summary.CurrencyCode,
                summary.TotalInvoiced,
                summary.TotalPaid,
                summary.AvailableCredit,
                summary.BalanceDue,
                summary.InvoiceCount,
                summary.OpenInvoiceCount)).ToArray()));
    }

    private static async Task<IResult> ListInvoicesAsync(
        Guid clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? search,
        string? state,
        int? take,
        string? cursor,
        ListClientInvoicesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientInvoicesQuery(
                clientId,
                fromDate,
                toDate,
                search,
                state,
                take ?? 25,
                cursor),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientInvoiceRegisterPageResponse(
            result.Value.Invoices.Select(invoice => new ClientInvoiceRegisterItemResponse(
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
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount));
    }

    private static async Task<IResult> ListPaymentsAsync(
        Guid clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? search,
        string? status,
        int? take,
        string? cursor,
        ListClientPaymentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientPaymentsQuery(
                clientId,
                fromDate,
                toDate,
                search,
                status,
                take ?? 25,
                cursor),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientPaymentRegisterPageResponse(
            result.Value.Payments.Select(payment => new ClientPaymentRegisterItemResponse(
                payment.PaymentId,
                payment.InvoiceId,
                payment.Reference,
                payment.Method,
                payment.Status,
                payment.Amount,
                payment.CurrencyCode,
                payment.ReceivedOn,
                payment.JournalEntryId)).ToArray(),
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount));
    }

    private static async Task<IResult> ListFinancialActivityAsync(
        Guid clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? search,
        int? take,
        string? cursor,
        ListClientFinancialActivityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientFinancialActivityQuery(
                clientId,
                fromDate,
                toDate,
                search,
                take ?? 25,
                cursor),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientFinancialActivityPageResponse(
            result.Value.Lines.Select(line => new ClientFinancialActivityItemResponse(
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
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount));
    }

    private static async Task<IResult> ListJournalPostingsAsync(
        Guid clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? search,
        string? sourceType,
        int? take,
        string? cursor,
        ListClientJournalPostingsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientJournalPostingsQuery(
                clientId,
                fromDate,
                toDate,
                search,
                sourceType,
                take ?? 20,
                cursor),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientJournalPostingPageResponse(
            result.Value.JournalPostings.Select(posting => new ClientJournalPostingItemResponse(
                posting.JournalEntryId,
                posting.EntryDate,
                posting.SourceType,
                posting.SourceReference,
                posting.Memo,
                posting.Status,
                posting.TotalDebit,
                posting.TotalCredit,
                posting.CurrencyCode,
                posting.LineCount)).ToArray(),
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount));
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

    private static ClientPortalInvitationResponse ToClientPortalInvitationResponse(
        ClientPortalInvitationResult invitation)
    {
        return new ClientPortalInvitationResponse(
            invitation.InvitationId,
            invitation.ClientId,
            invitation.Email,
            invitation.FullName,
            invitation.Role,
            invitation.Status,
            invitation.InvitedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.InvitationToken,
            invitation.InvitationUrl);
    }

    private static ClientDeploymentResponse ToClientDeploymentResponse(ClientDeploymentResult deployment)
    {
        return new ClientDeploymentResponse(
            deployment.ClientDeploymentId,
            deployment.ClientId,
            deployment.DisplayName,
            deployment.InstallationId,
            deployment.BootstrapMode,
            deployment.ClientDeploymentMode,
            deployment.SiteId,
            deployment.SiteRole,
            deployment.ParentSiteId,
            deployment.BranchCode,
            deployment.SyncTopologyId,
            deployment.LocalServerVersion,
            deployment.SafarSuiteAppVersion,
            deployment.IsPrimary,
            deployment.CreatedAtUtc,
            deployment.UpdatedAtUtc);
    }

    private static ClientSupportNoteResponse ToClientSupportNoteResponse(ClientSupportNoteResult note)
    {
        return new ClientSupportNoteResponse(
            note.Text,
            note.CreatedBy,
            note.CreatedAtUtc);
    }
}

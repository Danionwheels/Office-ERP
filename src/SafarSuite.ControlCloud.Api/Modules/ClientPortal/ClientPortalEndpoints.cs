using SafarSuite.ControlCloud.Api.Modules.ProviderAccess;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.ClientPortal;

public static class ClientPortalEndpoints
{
    public static IEndpointRouteBuilder MapClientPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/client-portal")
            .WithTags("Client Portal");

        group.MapPost("/invitations", CreateInvitationAsync)
            .WithName("CreateClientPortalInvitation");
        group.MapGet("/clients/{clientId:guid}/invitations", ListInvitationsAsync)
            .WithName("ListClientPortalInvitations");
        group.MapPost(
                "/clients/{clientId:guid}/invitations/{invitationId:guid}/resend",
                ResendInvitationAsync)
            .WithName("ResendClientPortalInvitation");
        group.MapPost(
                "/clients/{clientId:guid}/invitations/{invitationId:guid}/revoke",
                RevokeInvitationAsync)
            .WithName("RevokeClientPortalInvitation");
        group.MapPost("/invitations/accept", AcceptInvitationAsync)
            .WithName("AcceptClientPortalInvitation");
        group.MapPost("/sessions", CreateSessionAsync)
            .WithName("CreateClientPortalSession");
        group.MapGet("/clients/{clientId:guid}/commercial-summary", GetCommercialSummaryAsync)
            .WithName("GetClientPortalCommercialSummary");
        group.MapGet("/clients/{clientId:guid}/entitlement-bundle", GetSignedEntitlementBundleAsync)
            .WithName("GetClientPortalSignedEntitlementBundle");
        group.MapGet(
                "/clients/{clientId:guid}/installations/{installationId}/status",
                GetInstallationStatusAsync)
            .WithName("GetClientPortalInstallationStatus");

        return endpoints;
    }

    private static async Task<IResult> CreateInvitationAsync(
        CreateClientPortalInvitationRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        IClientPortalInvitationDeliveryRecorder deliveries,
        IClientPortalAuditRecorder audit,
        CreateClientPortalInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(httpRequest, providerAccess);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var invitation = await handler.HandleAsync(
            new CreateClientPortalInvitationCommand(
                request.ClientId,
                request.Email,
                request.FullName,
                request.Role,
                request.ExpiresInDays,
                request.CreatedBy),
            cancellationToken);

        if (invitation.IsSuccess)
        {
            var invitationUrl = BuildInvitationUrl(httpRequest, invitation.InvitationToken!);
            var delivered = await RecordDeliveryAsync(
                deliveries,
                invitation.InvitationId,
                invitation.ClientId,
                invitation.Email,
                invitation.FullName,
                invitation.Role,
                invitation.ExpiresAtUtc,
                invitation.InvitationToken!,
                invitationUrl,
                "Created",
                cancellationToken);
            await RecordAuditAsync(
                audit,
                new ClientPortalAuditRecord(
                    Guid.NewGuid(),
                    invitation.ClientId,
                    invitation.InvitationId,
                    null,
                    invitation.Email,
                    ClientPortalAuditEventTypes.InvitationDeliveryRecorded,
                    NormalizeActor(request.CreatedBy),
                    delivered
                        ? "Client portal invitation delivery was recorded."
                        : "Client portal invitation delivery recording failed.",
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return Results.Ok(new ClientPortalInvitationResponse(
                invitation.InvitationId,
                invitation.ClientId,
                invitation.Email,
                invitation.FullName,
                invitation.Role,
                invitation.Status,
                invitation.InvitedAtUtc,
                invitation.ExpiresAtUtc,
                invitation.InvitationToken,
                invitationUrl));
        }

        return invitation.FailureCode switch
        {
            "ClientNotFound" => Results.NotFound(new { code = invitation.FailureCode, detail = invitation.Detail }),
            "PortalUserAlreadyExists" => Results.Conflict(new { code = invitation.FailureCode, detail = invitation.Detail }),
            _ => Results.BadRequest(new { code = invitation.FailureCode, detail = invitation.Detail })
        };
    }

    private static IResult ToInvitationFailureResult(string? failureCode, string? detail)
    {
        var response = new { code = failureCode, detail };

        return failureCode switch
        {
            "ClientNotFound" => Results.NotFound(response),
            "InvitationNotFound" => Results.NotFound(response),
            "PortalUserAlreadyExists" => Results.Conflict(response),
            "InvitationClientMismatch" => Results.Conflict(response),
            "InvitationNotUsable" => Results.Conflict(response),
            _ => Results.BadRequest(response)
        };
    }

    private static ClientPortalInvitationResponse ToResponse(
        HttpRequest request,
        ClientPortalInvitationItemResult invitation)
    {
        var invitationUrl = string.IsNullOrWhiteSpace(invitation.InvitationToken)
            ? null
            : BuildInvitationUrl(request, invitation.InvitationToken);

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
            invitationUrl);
    }

    private static async Task<bool> RecordDeliveryAsync(
        IClientPortalInvitationDeliveryRecorder deliveries,
        Guid invitationId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        DateTimeOffset expiresAtUtc,
        string invitationToken,
        string invitationUrl,
        string deliveryReason,
        CancellationToken cancellationToken)
    {
        try
        {
            await deliveries.RecordAsync(
                new ClientPortalInvitationDeliveryRecord(
                    Guid.NewGuid(),
                    invitationId,
                    clientId,
                    email,
                    fullName,
                    role,
                    deliveryReason,
                    invitationUrl,
                    invitationToken,
                    DateTimeOffset.UtcNow,
                    expiresAtUtc),
                cancellationToken);

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static async Task RecordAuditAsync(
        IClientPortalAuditRecorder audit,
        ClientPortalAuditRecord record,
        CancellationToken cancellationToken)
    {
        try
        {
            await audit.RecordAsync(record, cancellationToken);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static async Task<IResult> ListInvitationsAsync(
        Guid clientId,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        ListClientPortalInvitationsHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(httpRequest, providerAccess);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var invitations = await handler.HandleAsync(
            new ListClientPortalInvitationsQuery(clientId),
            cancellationToken);

        if (invitations.IsSuccess)
        {
            return Results.Ok(new ListClientPortalInvitationsResponse(
                invitations.ClientId,
                invitations.Invitations!
                    .Select(invitation => ToResponse(httpRequest, invitation))
                    .ToArray()));
        }

        return ToInvitationFailureResult(invitations.FailureCode, invitations.Detail);
    }

    private static async Task<IResult> ResendInvitationAsync(
        Guid clientId,
        Guid invitationId,
        ResendClientPortalInvitationRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        IClientPortalInvitationDeliveryRecorder deliveries,
        IClientPortalAuditRecorder audit,
        ResendClientPortalInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(httpRequest, providerAccess);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var invitation = await handler.HandleAsync(
            new ResendClientPortalInvitationCommand(
                clientId,
                invitationId,
                request.ExpiresInDays,
                request.CreatedBy),
            cancellationToken);

        if (invitation.IsSuccess)
        {
            var invitationItem = invitation.Invitation!;
            var response = ToResponse(httpRequest, invitationItem);

            var delivered = await RecordDeliveryAsync(
                deliveries,
                invitationItem.InvitationId,
                invitationItem.ClientId,
                invitationItem.Email,
                invitationItem.FullName,
                invitationItem.Role,
                invitationItem.ExpiresAtUtc,
                invitationItem.InvitationToken!,
                response.InvitationUrl!,
                "Resent",
                cancellationToken);
            await RecordAuditAsync(
                audit,
                new ClientPortalAuditRecord(
                    Guid.NewGuid(),
                    invitationItem.ClientId,
                    invitationItem.InvitationId,
                    null,
                    invitationItem.Email,
                    ClientPortalAuditEventTypes.InvitationDeliveryRecorded,
                    NormalizeActor(request.CreatedBy),
                    delivered
                        ? "Client portal invitation resend delivery was recorded."
                        : "Client portal invitation resend delivery recording failed.",
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return Results.Ok(response);
        }

        return ToInvitationFailureResult(invitation.FailureCode, invitation.Detail);
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid clientId,
        Guid invitationId,
        RevokeClientPortalInvitationRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        RevokeClientPortalInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        _ = request;
        var providerAuthorizationFailure = AuthorizeProviderAccess(httpRequest, providerAccess);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var invitation = await handler.HandleAsync(
            new RevokeClientPortalInvitationCommand(clientId, invitationId),
            cancellationToken);

        return invitation.IsSuccess
            ? Results.Ok(ToResponse(httpRequest, invitation.Invitation!))
            : ToInvitationFailureResult(invitation.FailureCode, invitation.Detail);
    }

    private static IResult? AuthorizeProviderAccess(
        HttpRequest request,
        ProviderAccessSessionService providerAccess)
    {
        var authorization = providerAccess.Authorize(
            request,
            ProviderAccessScopes.ClientPortalManage);

        if (authorization.IsSuccess)
        {
            return null;
        }

        var code = authorization.FailureCode switch
        {
            "ProviderAccessNotConfigured" => "ClientPortalProviderAccessNotConfigured",
            "ProviderAccessScopeDenied" => "ClientPortalProviderAccessScopeDenied",
            "ProviderAccessExpired" => "ClientPortalProviderAccessExpired",
            _ => "ClientPortalProviderAccessDenied"
        };

        return Results.Json(
            new { code, detail = authorization.Detail },
            statusCode: authorization.StatusCode);
    }

    private static string NormalizeActor(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ClientPortalAuditActors.ControlDesk
            : value.Trim();
    }

    private static async Task<IResult> AcceptInvitationAsync(
        AcceptClientPortalInvitationRequest request,
        AcceptClientPortalInvitationHandler handler,
        CancellationToken cancellationToken)
    {
        var accepted = await handler.HandleAsync(
            new AcceptClientPortalInvitationCommand(
                request.InvitationToken,
                request.Password,
                request.FullName),
            cancellationToken);

        if (accepted.IsSuccess)
        {
            return Results.Ok(new AcceptClientPortalInvitationResponse(
                accepted.UserId,
                accepted.ClientId,
                accepted.Email,
                accepted.FullName,
                accepted.Role,
                accepted.AccessToken!,
                accepted.ExpiresAtUtc!.Value));
        }

        return accepted.FailureCode switch
        {
            "InvitationNotFound" => Results.NotFound(new { code = accepted.FailureCode, detail = accepted.Detail }),
            "InvitationNotUsable" => Results.Conflict(new { code = accepted.FailureCode, detail = accepted.Detail }),
            "PortalUserAlreadyExists" => Results.Conflict(new { code = accepted.FailureCode, detail = accepted.Detail }),
            _ => Results.BadRequest(new { code = accepted.FailureCode, detail = accepted.Detail })
        };
    }

    private static async Task<IResult> CreateSessionAsync(
        CreateClientPortalSessionRequest request,
        CreateClientPortalSessionHandler handler,
        CancellationToken cancellationToken)
    {
        var session = await handler.HandleAsync(
            new CreateClientPortalSessionCommand(
                request.ClientId,
                request.Email,
                request.Password),
            cancellationToken);

        if (session.IsSuccess)
        {
            return Results.Ok(new ClientPortalSessionResponse(
                session.ClientId,
                session.AccessToken!,
                session.ExpiresAtUtc!.Value,
                session.Role!));
        }

        return session.FailureCode switch
        {
            "InvalidCredentials" => Results.Json(
                new { code = session.FailureCode, detail = session.Detail },
                statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.BadRequest(new { code = session.FailureCode, detail = session.Detail })
        };
    }

    private static async Task<IResult> GetCommercialSummaryAsync(
        Guid clientId,
        HttpRequest request,
        IClientPortalSessionService sessions,
        GetClientPortalCommercialSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeClientSession(request, sessions, clientId);

        if (authorizationFailure is not null)
        {
            return authorizationFailure;
        }

        var projection = await handler.HandleAsync(
            new GetClientPortalCommercialSummaryQuery(clientId),
            cancellationToken);

        return projection is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(projection));
    }

    private static async Task<IResult> GetSignedEntitlementBundleAsync(
        Guid clientId,
        string? installationId,
        HttpRequest request,
        IClientPortalSessionService sessions,
        GetClientPortalSignedEntitlementBundleHandler handler,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeClientSession(request, sessions, clientId);

        if (authorizationFailure is not null)
        {
            return authorizationFailure;
        }

        var bundle = await handler.HandleAsync(
            new GetClientPortalSignedEntitlementBundleQuery(clientId, installationId),
            cancellationToken);

        if (bundle.IsSuccess)
        {
            return Results.Ok(ToResponse(bundle.Bundle!));
        }

        return bundle.FailureCode switch
        {
            "EntitlementNotFound" => Results.NotFound(new { code = bundle.FailureCode, detail = bundle.Detail }),
            "InstallationClientMismatch" => Results.Conflict(new { code = bundle.FailureCode, detail = bundle.Detail }),
            "EntitlementVersionRejected" => Results.Conflict(new { code = bundle.FailureCode, detail = bundle.Detail }),
            _ => Results.BadRequest(new { code = bundle.FailureCode, detail = bundle.Detail })
        };
    }

    private static async Task<IResult> GetInstallationStatusAsync(
        Guid clientId,
        string installationId,
        HttpRequest request,
        IClientPortalSessionService sessions,
        GetInstallationStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeClientSession(request, sessions, clientId);

        if (authorizationFailure is not null)
        {
            return authorizationFailure;
        }

        var status = await handler.HandleAsync(
            new GetInstallationStatusQuery(clientId, installationId),
            cancellationToken);

        if (status.IsSuccess)
        {
            return Results.Ok(status.Status);
        }

        return status.FailureCode switch
        {
            "InstallationNotFound" => Results.NotFound(new { code = status.FailureCode, detail = status.Detail }),
            "InstallationClientMismatch" => Results.Conflict(new { code = status.FailureCode, detail = status.Detail }),
            _ => Results.BadRequest(new { code = status.FailureCode, detail = status.Detail })
        };
    }

    private static IResult? AuthorizeClientSession(
        HttpRequest request,
        IClientPortalSessionService sessions,
        Guid clientId)
    {
        var session = sessions.Validate(request.Headers.Authorization.ToString());

        if (!session.IsSuccess)
        {
            return Results.Json(
                new { code = session.FailureCode, detail = session.Detail },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (session.Principal!.ClientId != clientId)
        {
            return Results.Json(
                new
                {
                    code = "ClientPortalSessionScopeMismatch",
                    detail = "Client Portal session is not allowed to read this client."
                },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return null;
    }

    private static string BuildInvitationUrl(HttpRequest request, string invitationToken)
    {
        return $"{request.Scheme}://{request.Host}/client-portal/index.html?invite={Uri.EscapeDataString(invitationToken)}";
    }

    private static ClientPortalCommercialSummaryResponse ToResponse(
        ControlCloudClientCommercialProjection projection)
    {
        return new ClientPortalCommercialSummaryResponse(
            projection.ClientId,
            projection.CurrencyCode,
            projection.TotalInvoiced,
            projection.TotalPaid,
            projection.TotalCredited,
            projection.TotalRefunded,
            projection.TotalCreditApplied,
            projection.BalanceDue,
            projection.AvailableCredit,
            projection.IsPaid,
            projection.LastUpdatedAtUtc,
            projection.Invoices.Values
                .OrderBy(invoice => invoice.IssueDate)
                .ThenBy(invoice => invoice.InvoiceNumber)
                .Select(invoice => new ClientPortalInvoiceSummaryResponse(
                    invoice.InvoiceId,
                    invoice.InvoiceNumber,
                    invoice.ContractId,
                    invoice.InvoiceStatus,
                    invoice.IssueDate,
                    invoice.DueDate,
                    invoice.TotalAmount,
                    invoice.BalanceDue,
                    invoice.CurrencyCode,
                    invoice.VoidedOn,
                    invoice.VoidReason))
                .ToArray(),
            projection.Payments.Values
                .OrderBy(payment => payment.ReceivedOn)
                .ThenBy(payment => payment.PaymentReference)
                .Select(payment => new ClientPortalPaymentSummaryResponse(
                    payment.PaymentId,
                    payment.InvoiceId,
                    payment.InvoiceNumber,
                    payment.PaymentStatus,
                    payment.PaymentMethod,
                    payment.PaymentReference,
                    payment.Amount,
                    payment.InvoiceBalanceDue,
                    payment.CurrencyCode,
                    payment.ReceivedOn))
                .ToArray(),
            projection.CreditNotes.Values
                .OrderBy(creditNote => creditNote.CreditDate)
                .ThenBy(creditNote => creditNote.CreditNoteNumber)
                .Select(creditNote => new ClientPortalCreditNoteSummaryResponse(
                    creditNote.CreditNoteId,
                    creditNote.CreditNoteNumber,
                    creditNote.InvoiceId,
                    creditNote.InvoiceNumber,
                    creditNote.CreditNoteStatus,
                    creditNote.CreditDate,
                    creditNote.Amount,
                    creditNote.CurrencyCode,
                    creditNote.Reason))
                .ToArray(),
            projection.Refunds.Values
                .OrderBy(refund => refund.RefundedOn)
                .ThenBy(refund => refund.RefundReference)
                .Select(refund => new ClientPortalRefundSummaryResponse(
                    refund.RefundId,
                    refund.RefundStatus,
                    refund.RefundMethod,
                    refund.RefundReference,
                    refund.Amount,
                    refund.ClientBalanceBefore,
                    refund.ClientBalanceAfter,
                    refund.CurrencyCode,
                    refund.RefundedOn))
                .ToArray(),
            projection.CreditApplications.Values
                .OrderBy(application => application.AppliedOn)
                .ThenBy(application => application.Reference)
                .Select(application => new ClientPortalCreditApplicationSummaryResponse(
                    application.CreditApplicationId,
                    application.InvoiceId,
                    application.InvoiceNumber,
                    application.InvoiceStatus,
                    application.CreditApplicationStatus,
                    application.Reference,
                    application.Amount,
                    application.InvoiceBalanceBefore,
                    application.InvoiceBalanceAfter,
                    application.AvailableCreditBefore,
                    application.AvailableCreditAfter,
                    application.ClientBalanceBefore,
                    application.ClientBalanceAfter,
                    application.CurrencyCode,
                    application.AppliedOn))
                .ToArray(),
            projection.LatestEntitlement is null
                ? null
                : new ClientPortalEntitlementSummaryResponse(
                    projection.LatestEntitlement.EntitlementSnapshotId,
                    projection.LatestEntitlement.ContractId,
                    projection.LatestEntitlement.SourceInvoiceId,
                    projection.LatestEntitlement.SourceInvoiceNumber,
                    projection.LatestEntitlement.Status,
                    projection.LatestEntitlement.PaidUntil,
                    projection.LatestEntitlement.GraceUntil,
                    projection.LatestEntitlement.OfflineValidUntil,
                    projection.LatestEntitlement.AllowedDevices,
                    projection.LatestEntitlement.AllowedBranches,
                    projection.LatestEntitlement.IssuedAtUtc,
                    projection.LatestEntitlement.Modules
                        .OrderBy(module => module.ModuleCode)
                        .Select(module => new ClientPortalEntitlementModuleSummaryResponse(
                            module.ModuleCode,
                            module.IsEnabled))
                        .ToArray()));
    }

    internal static ClientPortalSignedEntitlementBundleResponse ToResponse(
        ControlCloudSignedEntitlementBundle bundle)
    {
        return ControlCloudEntitlementBundleContractMapper.ToResponse(bundle);
    }
}

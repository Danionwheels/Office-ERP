using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.ClientPortal;

public static class ClientPortalEndpoints
{
    private const string ProviderAccessHeaderName = "X-SafarSuite-Provider-Key";

    public static IEndpointRouteBuilder MapClientPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/client-portal")
            .WithTags("Client Portal");

        group.MapPost("/invitations", CreateInvitationAsync)
            .WithName("CreateClientPortalInvitation");
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
        ClientPortalProviderAccessOptions providerAccess,
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

    private static IResult? AuthorizeProviderAccess(
        HttpRequest request,
        ClientPortalProviderAccessOptions providerAccess)
    {
        var expectedSecret = providerAccess.SharedSecret.Trim();

        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            return Results.Json(
                new
                {
                    code = "ClientPortalProviderAccessNotConfigured",
                    detail = "Client Portal provider invitation access is not configured."
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var providedSecret = request.Headers[ProviderAccessHeaderName].ToString().Trim();

        if (string.IsNullOrWhiteSpace(providedSecret)
            || !FixedTimeEquals(providedSecret, expectedSecret))
        {
            return Results.Json(
                new
                {
                    code = "ClientPortalProviderAccessDenied",
                    detail = "Provider access is required before creating a Client Portal invitation."
                },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return null;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
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
        return new ClientPortalSignedEntitlementBundleResponse(
            bundle.PayloadJson,
            new ClientPortalEntitlementBundlePayloadResponse(
                bundle.Payload.BundleVersion,
                bundle.Payload.Issuer,
                bundle.Payload.Audience,
                bundle.Payload.ClientId,
                bundle.Payload.InstallationId,
                bundle.Payload.EntitlementVersion,
                bundle.Payload.BundleIssueId,
                bundle.Payload.EntitlementSnapshotId,
                bundle.Payload.ContractId,
                bundle.Payload.SourceInvoiceId,
                bundle.Payload.SourceInvoiceNumber,
                bundle.Payload.Status,
                bundle.Payload.BundleIssuedAtUtc,
                bundle.Payload.EntitlementIssuedAtUtc,
                bundle.Payload.ValidFrom,
                bundle.Payload.PaidUntil,
                bundle.Payload.WarningStartsAt,
                bundle.Payload.GraceUntil,
                bundle.Payload.OfflineValidUntil,
                bundle.Payload.AllowedDevices,
                bundle.Payload.AllowedBranches,
                bundle.Payload.Modules
                    .OrderBy(module => module.ModuleCode, StringComparer.Ordinal)
                    .Select(module => new ClientPortalEntitlementBundleModuleResponse(
                        module.ModuleCode,
                        module.Status,
                        module.IsEnabled))
                    .ToArray()),
            new ClientPortalEntitlementBundleSignatureResponse(
                bundle.Signature.Algorithm,
                bundle.Signature.KeyId,
                bundle.Signature.PayloadSha256,
                bundle.Signature.Value));
    }
}

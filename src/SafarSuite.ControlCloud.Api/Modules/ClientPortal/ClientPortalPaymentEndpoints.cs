using Microsoft.AspNetCore.Mvc;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.CreatePortalPaymentClaim;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalBankDetails;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalBillingSummary;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalInvoice;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaim;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.ListPortalInvoices;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.ListPortalPaymentClaims;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.UploadPortalAttachment;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.ClientPortal;

public static class ClientPortalPaymentEndpoints
{
    private const long MaximumUploadRequestBytes = ClientPortalAttachmentContentValidator.MaximumSizeBytes + 256 * 1024;
    private static readonly string[] BillingReadRoles = ["ClientOwner", "ClientBilling", "ClientViewer"];
    private static readonly string[] BillingWriteRoles = ["ClientOwner", "ClientBilling"];

    public static IEndpointRouteBuilder MapClientPortalPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/portal/api/v1").WithTags("Client Portal Billing");

        group.MapGet("/billing-summary", GetBillingSummaryAsync).WithName("GetClientPortalBillingSummary");
        group.MapGet("/invoices", ListInvoicesAsync).WithName("ListClientPortalInvoices");
        group.MapGet("/invoices/{invoiceId:guid}", GetInvoiceAsync).WithName("GetClientPortalInvoice");
        group.MapGet("/payment-claims", ListPaymentClaimsAsync).WithName("ListClientPortalPaymentClaims");
        group.MapGet("/payment-claims/{claimId:guid}", GetPaymentClaimAsync).WithName("GetClientPortalPaymentClaim");
        group.MapPost("/payment-claims", CreatePaymentClaimAsync).WithName("CreateClientPortalPaymentClaim");
        group.MapPost("/attachments", UploadAttachmentAsync)
            .WithName("UploadClientPortalPaymentAttachment")
            .WithMetadata(new RequestSizeLimitAttribute(MaximumUploadRequestBytes));
        group.MapGet("/config/bank-details", GetBankDetailsAsync).WithName("GetClientPortalBankDetails");

        return endpoints;
    }

    private static async Task<IResult> GetBillingSummaryAsync(
        HttpRequest request,
        IClientPortalSessionService sessions,
        GetClientPortalBillingSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingReadRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var result = await handler.HandleAsync(authorization.Principal!.ClientId, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new ClientPortalBillingSummaryResponse(
                result.Value!.TotalOutstanding,
                result.Value.UnpaidInvoiceCount,
                result.Value.LastPaymentDate,
                result.Value.CurrencyCode))
            : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ListInvoicesAsync(
        HttpRequest request,
        IClientPortalSessionService sessions,
        ListClientPortalInvoicesHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingReadRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var result = await handler.HandleAsync(authorization.Principal!.ClientId, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new ClientPortalInvoiceListResponse(result.Value!.Select(ToInvoiceListItem).ToArray()))
            : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetInvoiceAsync(
        Guid invoiceId,
        HttpRequest request,
        IClientPortalSessionService sessions,
        GetClientPortalInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingReadRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var result = await handler.HandleAsync(
            authorization.Principal!.ClientId,
            invoiceId,
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToInvoiceDetail(result.Value!)) : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ListPaymentClaimsAsync(
        HttpRequest request,
        IClientPortalSessionService sessions,
        ListClientPortalPaymentClaimsHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingReadRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var result = await handler.HandleAsync(authorization.Principal!.ClientId, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new ClientPortalPaymentClaimListResponse(result.Value!.Select(ToClaimResponse).ToArray()))
            : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetPaymentClaimAsync(
        Guid claimId,
        HttpRequest request,
        IClientPortalSessionService sessions,
        GetClientPortalPaymentClaimHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingReadRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var result = await handler.HandleAsync(
            claimId,
            authorization.Principal!.ClientId,
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToClaimResponse(result.Value!)) : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> CreatePaymentClaimAsync(
        CreateClientPortalPaymentClaimRequest requestBody,
        HttpRequest request,
        IClientPortalSessionService sessions,
        CreateClientPortalPaymentClaimHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingWriteRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var principal = authorization.Principal!;
        var result = await handler.HandleAsync(
            new CreateClientPortalPaymentClaimCommand(
                principal.ClientId,
                principal.UserId,
                requestBody.InvoiceId,
                requestBody.Amount,
                requestBody.TransferReferenceNumber,
                requestBody.ProofAttachmentId),
            cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/portal/api/v1/payment-claims/{result.Value!.Claim.ClaimId:D}", ToClaimResponse(result.Value))
            : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> UploadAttachmentAsync(
        HttpRequest request,
        IClientPortalSessionService sessions,
        UploadClientPortalAttachmentHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingWriteRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        if (!request.HasFormContentType)
        {
            return Results.Json(
                new { code = "PaymentProofContentTypeInvalid", detail = "Multipart form data is required." },
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        if (request.ContentLength > MaximumUploadRequestBytes)
        {
            return UploadTooLarge();
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return UploadTooLarge();
        }

        if (form.Files.Count != 1)
        {
            return Results.BadRequest(new { code = "PaymentProofRequired", detail = "Exactly one payment proof file is required." });
        }

        var file = form.Files[0];
        if (file.Length is <= 0 or > ClientPortalAttachmentContentValidator.MaximumSizeBytes)
        {
            return UploadTooLarge();
        }

        await using var input = file.OpenReadStream();
        await using var content = new MemoryStream((int)file.Length);
        await input.CopyToAsync(content, cancellationToken);
        var principal = authorization.Principal!;
        var result = await handler.HandleAsync(
            new UploadClientPortalAttachmentCommand(
                principal.ClientId,
                principal.UserId,
                file.FileName,
                file.ContentType,
                content.ToArray()),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToFailure(result.FailureCode, result.Detail);
        }

        var attachment = result.Value!;
        return Results.Created(
            $"/portal/api/v1/attachments/{attachment.AttachmentId:D}",
            new ClientPortalAttachmentUploadResponse(
                attachment.AttachmentId,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeBytes,
                attachment.UploadedAtUtc));
    }

    private static async Task<IResult> GetBankDetailsAsync(
        HttpRequest request,
        IClientPortalSessionService sessions,
        GetClientPortalBankDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request, sessions, BillingReadRoles, cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var details = await handler.HandleAsync(cancellationToken);
        return Results.Ok(new ClientPortalBankDetailsResponse(
            details.IsConfigured,
            details.BankName,
            details.AccountTitle,
            details.AccountNumber,
            details.Iban,
            details.BranchOrRoutingInfo));
    }

    private static async Task<(ClientPortalSessionPrincipal? Principal, IResult? Failure)> AuthorizeAsync(
        HttpRequest request,
        IClientPortalSessionService sessions,
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken)
    {
        var validation = await sessions.ValidateAsync(
            request.Headers.Authorization.ToString(),
            cancellationToken: cancellationToken);
        if (!validation.IsSuccess)
        {
            return (null, Results.Json(
                new { code = validation.FailureCode, detail = validation.Detail },
                statusCode: StatusCodes.Status401Unauthorized));
        }

        var principal = validation.Principal!;
        if (!allowedRoles.Any(role => role.Equals(principal.Role, StringComparison.Ordinal)))
        {
            return (null, Results.Json(
                new { code = "ClientPortalRoleDenied", detail = "Client Portal role is not allowed to access billing." },
                statusCode: StatusCodes.Status403Forbidden));
        }

        if (principal.UserId == Guid.Empty || principal.ClientId == Guid.Empty)
        {
            return (null, Results.Json(
                new { code = "PortalSessionInvalid", detail = "Client Portal session identity is invalid." },
                statusCode: StatusCodes.Status401Unauthorized));
        }

        return (principal, null);
    }

    private static IResult ToFailure(string? code, string? detail)
    {
        var response = new { code, detail };
        return code switch
        {
            "ClientBillingNotFound" or "PortalInvoiceNotFound" or "PaymentClaimNotFound" or "PaymentProofNotFound" => Results.NotFound(response),
            "PaymentClaimDuplicate" or "PaymentClaimConflict" => Results.Conflict(response),
            "PaymentClaimAmountInvalid" or "PortalInvoiceNotPayable" => Results.Json(response, statusCode: StatusCodes.Status422UnprocessableEntity),
            "PaymentProofSizeInvalid" => UploadTooLarge(),
            "PaymentProofTypeInvalid" => Results.Json(response, statusCode: StatusCodes.Status415UnsupportedMediaType),
            _ => Results.BadRequest(response)
        };
    }

    private static IResult UploadTooLarge() => Results.Json(
        new { code = "PaymentProofSizeInvalid", detail = "Payment proof must be 5 MB or smaller." },
        statusCode: StatusCodes.Status413PayloadTooLarge);

    private static ClientPortalInvoiceListItemResponse ToInvoiceListItem(ClientPortalInvoiceListItem item) =>
        new(
            item.Invoice.InvoiceId,
            item.Invoice.InvoiceNumber,
            item.Invoice.IssueDate,
            item.Invoice.DueDate,
            item.Invoice.TotalAmount,
            item.AmountPaid,
            item.Invoice.BalanceDue,
            item.Invoice.CurrencyCode,
            item.Invoice.InvoiceStatus);

    private static ClientPortalInvoiceDetailResponse ToInvoiceDetail(ClientPortalInvoiceDetail detail)
    {
        var invoice = detail.Invoice;
        var client = invoice.Client ?? new ControlCloudClientBillingDetailProjection("", null, null, null);
        return new ClientPortalInvoiceDetailResponse(
            invoice.InvoiceId,
            invoice.InvoiceNumber,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.TotalAmount,
            detail.AmountPaid,
            invoice.BalanceDue,
            invoice.CurrencyCode,
            invoice.InvoiceStatus,
            new ClientPortalBillingClientResponse(client.Name, client.ContactName, client.Email, client.Phone),
            (invoice.Lines ?? []).Select(line => new ClientPortalInvoiceLineResponse(
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.LineTotal,
                line.CurrencyCode)).ToArray(),
            detail.Payments.Select(payment => new ClientPortalInvoicePaymentResponse(
                payment.PaymentId,
                payment.PaymentReference,
                payment.Amount,
                payment.CurrencyCode,
                payment.ReceivedOn,
                payment.PaymentStatus,
                payment.PaymentMethod)).ToArray());
    }

    internal static ClientPortalPaymentClaimResponse ToClaimResponse(ClientPortalPaymentClaimView view)
    {
        var claim = view.Claim;
        var attachment = view.ProofAttachment;
        return new ClientPortalPaymentClaimResponse(
            claim.ClaimId,
            claim.ClientId,
            claim.InvoiceId,
            claim.InvoiceNumber,
            claim.Amount,
            claim.CurrencyCode,
            claim.TransferReferenceNumber,
            claim.ProofAttachmentId,
            attachment is null
                ? null
                : new ClientPortalAttachmentSummaryResponse(
                    attachment.AttachmentId,
                    attachment.FileName,
                    attachment.ContentType,
                    attachment.SizeBytes,
                    attachment.UploadedAtUtc),
            claim.Status switch
            {
                ControlCloudClientPortalPaymentClaimStatus.PendingVerification => "pending_verification",
                ControlCloudClientPortalPaymentClaimStatus.Verified => "verified",
                ControlCloudClientPortalPaymentClaimStatus.Rejected => "rejected",
                _ => "pending_verification"
            },
            claim.SubmittedAtUtc,
            claim.ReviewedAtUtc,
            claim.RejectionReason);
    }
}

using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ImportPortalPaymentClaims;

public sealed class ImportPortalPaymentClaimsHandler
{
    private readonly IClientRepository _clients;
    private readonly IPortalPaymentClaimRepository _claims;
    private readonly IControlCloudPaymentClaimClient _cloudClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ImportPortalPaymentClaimsHandler(
        IClientRepository clients,
        IPortalPaymentClaimRepository claims,
        IControlCloudPaymentClaimClient cloudClient,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _clients = clients;
        _claims = claims;
        _cloudClient = cloudClient;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ImportPortalPaymentClaimsResult>> HandleAsync(
        ImportPortalPaymentClaimsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return Result<ImportPortalPaymentClaimsResult>.Failure(ApplicationError.Validation(
                nameof(command.ClientId),
                "Client id cannot be empty."));
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<ImportPortalPaymentClaimsResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            // The external read deliberately completes before the local transaction begins.
            var remoteResult = await _cloudClient.ListAsync(command.ClientId, cancellationToken);

            if (!remoteResult.IsSuccess)
            {
                return Result<ImportPortalPaymentClaimsResult>.Failure(ApplicationError.ServiceUnavailable(
                    remoteResult.Detail ?? "Control Cloud payment claims are unavailable.",
                    nameof(command.ClientId)));
            }

            var remoteClaims = remoteResult.Claims
                .GroupBy(claim => claim.ClaimId)
                .Select(group => group.First())
                .ToArray();
            var localClaims = await _claims.ListAsync(clientId, cancellationToken);
            var localIds = localClaims.Select(claim => claim.Id.Value).ToHashSet();
            var alreadyImportedCount = remoteClaims.Count(claim => localIds.Contains(claim.ClaimId));
            var candidates = remoteClaims
                .Where(claim => !localIds.Contains(claim.ClaimId))
                .Where(claim => claim.ClientId == command.ClientId)
                .Where(claim => IsPendingVerificationStatus(claim.Status))
                .ToArray();
            var imported = candidates.Select(ToDomainClaim).ToArray();

            await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    foreach (var claim in imported)
                    {
                        await _claims.AddAsync(claim, token);
                    }
                },
                cancellationToken);

            var refreshedClaims = await _claims.ListAsync(clientId, cancellationToken);
            var ignoredCount = remoteClaims.Length - alreadyImportedCount - imported.Length;

            return Result<ImportPortalPaymentClaimsResult>.Success(new ImportPortalPaymentClaimsResult(
                command.ClientId,
                remoteClaims.Length,
                imported.Length,
                alreadyImportedCount,
                ignoredCount,
                refreshedClaims.Select(PortalPaymentClaimResultFactory.From).ToArray()));
        }
        catch (ArgumentException exception)
        {
            return Result<ImportPortalPaymentClaimsResult>.Failure(ApplicationError.ServiceUnavailable(
                $"Control Cloud returned an invalid payment claim: {exception.Message}",
                nameof(command.ClientId)));
        }
    }

    private PortalPaymentClaim ToDomainClaim(ClientPortalPaymentClaimResponse claim)
    {
        return PortalPaymentClaim.Import(
            PortalPaymentClaimId.Create(claim.ClaimId),
            ClientId.Create(claim.ClientId),
            InvoiceId.Create(claim.InvoiceId),
            claim.InvoiceNumber,
            Money.Of(claim.Amount, claim.CurrencyCode),
            claim.TransferReferenceNumber,
            claim.ProofAttachmentId,
            claim.ProofAttachment?.FileName,
            claim.ProofAttachment?.ContentType,
            claim.ProofAttachment?.SizeBytes,
            claim.ProofAttachment?.UploadedAtUtc,
            claim.SubmittedAtUtc,
            _clock.UtcNow);
    }

    private static bool IsPendingVerificationStatus(string status)
    {
        var normalized = new string(status.Where(char.IsLetterOrDigit).ToArray());

        return string.Equals(
            normalized,
            PortalPaymentClaimStatus.PendingVerification.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}

using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.GetClientRefundDocument;

public sealed class GetClientRefundDocumentHandler
{
    private readonly IClientRefundRepository _refunds;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ClientCreditBalanceService _creditBalanceService;

    public GetClientRefundDocumentHandler(
        IClientRefundRepository refunds,
        IJournalEntryRepository journalEntries,
        ClientCreditBalanceService creditBalanceService)
    {
        _refunds = refunds;
        _journalEntries = journalEntries;
        _creditBalanceService = creditBalanceService;
    }

    public async Task<Result<ClientRefundDocumentResult>> HandleAsync(
        GetClientRefundDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.RefundId == Guid.Empty)
        {
            return Result<ClientRefundDocumentResult>.Failure(ApplicationError.Validation(
                nameof(query.RefundId),
                "Refund id cannot be empty."));
        }

        var refund = await _refunds.GetByIdAsync(ClientRefundId.Create(query.RefundId), cancellationToken);

        if (refund is null)
        {
            return Result<ClientRefundDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(query.RefundId),
                "Refund was not found."));
        }

        var journalEntry = await FindJournalAsync(refund, cancellationToken);

        if (journalEntry is null)
        {
            return Result<ClientRefundDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(query.RefundId),
                "Refund journal entry was not found."));
        }

        var currentBalance = await _creditBalanceService.CalculateAsync(
            ClientId.Create(refund.ClientId.Value),
            refund.CurrencyCode,
            cancellationToken);
        var clientBalanceAfter = currentBalance.StatementBalance;
        var clientBalanceBefore = clientBalanceAfter - refund.Amount.Amount;

        return Result<ClientRefundDocumentResult>.Success(new ClientRefundDocumentResult(
            PaymentDocumentResultFactory.ToIssueClientRefundResult(
                refund,
                journalEntry,
                clientBalanceBefore,
                clientBalanceAfter)));
    }

    private async Task<JournalEntry?> FindJournalAsync(
        ClientRefund refund,
        CancellationToken cancellationToken)
    {
        var entries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.ClientRefund,
            cancellationToken: cancellationToken);

        return entries
            .Where(entry => string.Equals(entry.SourceReference, refund.Reference.Value, StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.Equals(entry.CurrencyCode, refund.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            .Where(entry => entry.TotalDebit.Amount == refund.Amount.Amount)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .FirstOrDefault();
    }
}

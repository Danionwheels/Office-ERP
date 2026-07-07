using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;

public sealed class IssueClientRefundHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientRefundRepository _refunds;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly PaymentPostingService _postingService;
    private readonly ClientCreditBalanceService _creditBalanceService;
    private readonly PaymentCloudOutboxMessageFactory _outboxMessageFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly IssueClientRefundValidator _validator;

    public IssueClientRefundHandler(
        IClientRepository clients,
        IClientRefundRepository refunds,
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        AccountingPeriodPostingGuard periodGuard,
        PaymentPostingService postingService,
        ClientCreditBalanceService creditBalanceService,
        PaymentCloudOutboxMessageFactory outboxMessageFactory,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        IssueClientRefundValidator validator)
    {
        _clients = clients;
        _refunds = refunds;
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
        _cloudOutboxMessages = cloudOutboxMessages;
        _periodGuard = periodGuard;
        _postingService = postingService;
        _creditBalanceService = creditBalanceService;
        _outboxMessageFactory = outboxMessageFactory;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<IssueClientRefundResult>> HandleAsync(
        IssueClientRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<IssueClientRefundResult>.Failure(validationErrors);
        }

        try
        {
            if (!Enum.TryParse<PaymentMethod>(command.Method, ignoreCase: true, out var refundMethod))
            {
                return Result<IssueClientRefundResult>.Failure(ApplicationError.Validation(
                    nameof(command.Method),
                    "Refund method is invalid."));
            }

            var clientId = ClientId.Create(command.ClientId);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<IssueClientRefundResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var refundReference = ClientRefundReference.Create(command.Reference);

            if (await _refunds.ExistsByReferenceAsync(refundReference, cancellationToken))
            {
                return Result<IssueClientRefundResult>.Failure(ApplicationError.Conflict(
                    nameof(command.Reference),
                    $"Refund {refundReference.Value} already exists."));
            }

            var cashOrBankAccountId = LedgerAccountId.Create(command.CashOrBankAccountId);
            var accountsReceivableAccountId = LedgerAccountId.Create(command.AccountsReceivableAccountId);
            var cashAccountResult = await _postingService.ValidateAssetPostingAccountAsync(
                cashOrBankAccountId,
                nameof(command.CashOrBankAccountId),
                "Cash or bank account",
                cancellationToken);

            if (cashAccountResult.IsFailure)
            {
                return Result<IssueClientRefundResult>.Failure(cashAccountResult.Errors);
            }

            var receivableAccountResult = await _postingService.ValidateAssetPostingAccountAsync(
                accountsReceivableAccountId,
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable account",
                cancellationToken);

            if (receivableAccountResult.IsFailure)
            {
                return Result<IssueClientRefundResult>.Failure(receivableAccountResult.Errors);
            }

            var refundAmount = Money.Of(command.Amount, command.CurrencyCode);

            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.PostingDate,
                nameof(command.PostingDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<IssueClientRefundResult>.Failure(periodError);
            }

            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var creditBalance = await _creditBalanceService.CalculateAsync(
                        clientId,
                        refundAmount.CurrencyCode,
                        token);

                    if (refundAmount.Amount > creditBalance.RefundableCredit)
                    {
                        return Result<IssueClientRefundResult>.Failure(ApplicationError.Validation(
                            nameof(command.Amount),
                            $"Refund exceeds refundable client credit of {creditBalance.RefundableCredit:0.00} {refundAmount.CurrencyCode}."));
                    }

                    var refund = ClientRefund.Issue(
                        ClientRefundId.Create(_idGenerator.NewGuid()),
                        clientId,
                        refundMethod,
                        refundReference,
                        refundAmount,
                        command.RefundedOn,
                        command.Note,
                        _clock.UtcNow);
                    var journalEntry = _postingService.CreateClientRefundJournalEntry(
                        refund,
                        cashOrBankAccountId,
                        accountsReceivableAccountId,
                        command.PostingDate);
                    var clientBalanceAfter = creditBalance.StatementBalance + refund.Amount.Amount;

                    journalEntry.Post(_clock.UtcNow);

                    await _refunds.AddAsync(refund, token);
                    await _journalEntries.AddAsync(journalEntry, token);
                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreateClientRefundIssued(
                            refund,
                            journalEntry,
                            creditBalance.StatementBalance,
                            clientBalanceAfter),
                        token);

                    return Result<IssueClientRefundResult>.Success(await ToResultAsync(
                        refund,
                        journalEntry,
                        creditBalance.StatementBalance,
                        clientBalanceAfter,
                        token));
                },
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Result<IssueClientRefundResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<IssueClientRefundResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<IssueClientRefundResult> ToResultAsync(
        ClientRefund refund,
        JournalEntry journalEntry,
        decimal clientBalanceBefore,
        decimal clientBalanceAfter,
        CancellationToken cancellationToken)
    {
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return PaymentDocumentResultFactory.ToIssueClientRefundResult(
            refund,
            journalEntry,
            clientBalanceBefore,
            clientBalanceAfter,
            ledgerAccountsById);
    }
}

using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.UpdateLedgerAccount;

public sealed class UpdateLedgerAccountHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateLedgerAccountValidator _validator;

    public UpdateLedgerAccountHandler(
        ILedgerAccountRepository ledgerAccounts,
        IUnitOfWork unitOfWork,
        UpdateLedgerAccountValidator validator)
    {
        _ledgerAccounts = ledgerAccounts;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<UpdateLedgerAccountResult>> HandleAsync(
        UpdateLedgerAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<UpdateLedgerAccountResult>.Failure(validationErrors);
        }

        try
        {
            if (!Enum.TryParse<LedgerAccountStatus>(command.Status, true, out var status)
                || !Enum.IsDefined(status))
            {
                return Result<UpdateLedgerAccountResult>.Failure(ApplicationError.Validation(
                    nameof(command.Status),
                    "Ledger account status is invalid."));
            }

            var account = await _ledgerAccounts.GetByIdAsync(
                LedgerAccountId.Create(command.LedgerAccountId),
                cancellationToken);

            if (account is null)
            {
                return Result<UpdateLedgerAccountResult>.Failure(ApplicationError.NotFound(
                    nameof(command.LedgerAccountId),
                    "Ledger account was not found."));
            }

            account.Rename(command.Name);
            account.SetPostingAccount(command.IsPostingAccount);

            if (status == LedgerAccountStatus.Active)
            {
                account.Activate();
            }
            else
            {
                account.Deactivate();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UpdateLedgerAccountResult>.Success(new UpdateLedgerAccountResult(
                account.Id.Value,
                account.Code.Value,
                account.Name,
                account.Type.ToString(),
                account.NormalBalance.ToString(),
                account.ParentAccountId?.Value,
                account.IsPostingAccount,
                account.Status.ToString(),
                account.CreatedAtUtc));
        }
        catch (ArgumentException exception)
        {
            return Result<UpdateLedgerAccountResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}

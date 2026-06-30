using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;

public sealed class CreateLedgerAccountHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateLedgerAccountValidator _validator;

    public CreateLedgerAccountHandler(
        ILedgerAccountRepository ledgerAccounts,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateLedgerAccountValidator validator)
    {
        _ledgerAccounts = ledgerAccounts;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CreateLedgerAccountResult>> HandleAsync(
        CreateLedgerAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateLedgerAccountResult>.Failure(validationErrors);
        }

        try
        {
            if (!Enum.TryParse<LedgerAccountType>(command.Type, true, out var type))
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Validation(
                    nameof(command.Type),
                    "Ledger account type is invalid."));
            }

            if (!Enum.TryParse<NormalBalance>(command.NormalBalance, true, out var normalBalance))
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Validation(
                    nameof(command.NormalBalance),
                    "Normal balance is invalid."));
            }

            var code = LedgerAccountCode.Create(command.Code);

            if (await _ledgerAccounts.ExistsByCodeAsync(code, cancellationToken))
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Conflict(
                    nameof(command.Code),
                    $"Ledger account {code.Value} already exists."));
            }

            var parentAccountId = command.ParentAccountId.HasValue
                ? LedgerAccountId.Create(command.ParentAccountId.Value)
                : (LedgerAccountId?)null;

            if (parentAccountId.HasValue
                && await _ledgerAccounts.GetByIdAsync(parentAccountId.Value, cancellationToken) is null)
            {
                return Result<CreateLedgerAccountResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ParentAccountId),
                    "Parent ledger account was not found."));
            }

            var ledgerAccount = LedgerAccount.Create(
                LedgerAccountId.Create(_idGenerator.NewGuid()),
                code,
                command.Name,
                type,
                normalBalance,
                parentAccountId,
                command.IsPostingAccount,
                _clock.UtcNow);

            await _ledgerAccounts.AddAsync(ledgerAccount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CreateLedgerAccountResult>.Success(new CreateLedgerAccountResult(
                ledgerAccount.Id.Value,
                ledgerAccount.Code.Value,
                ledgerAccount.Name,
                ledgerAccount.Type.ToString(),
                ledgerAccount.NormalBalance.ToString(),
                ledgerAccount.IsPostingAccount,
                ledgerAccount.Status.ToString()));
        }
        catch (ArgumentException exception)
        {
            return Result<CreateLedgerAccountResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}

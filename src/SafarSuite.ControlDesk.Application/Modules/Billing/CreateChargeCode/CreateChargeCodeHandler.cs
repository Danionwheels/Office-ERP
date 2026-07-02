using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;

public sealed class CreateChargeCodeHandler
{
    private readonly IChargeCodeRepository _chargeCodes;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateChargeCodeValidator _validator;

    public CreateChargeCodeHandler(
        IChargeCodeRepository chargeCodes,
        ILedgerAccountRepository ledgerAccounts,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateChargeCodeValidator validator)
    {
        _chargeCodes = chargeCodes;
        _ledgerAccounts = ledgerAccounts;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CreateChargeCodeResult>> HandleAsync(
        CreateChargeCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateChargeCodeResult>.Failure(validationErrors);
        }

        try
        {
            var code = ChargeCodeKey.Create(command.Code);

            if (await _chargeCodes.ExistsByCodeAsync(code, cancellationToken))
            {
                return Result<CreateChargeCodeResult>.Failure(ApplicationError.Conflict(
                    nameof(command.Code),
                    $"Charge code {code.Value} already exists."));
            }

            var revenueAccountId = LedgerAccountId.Create(command.RevenueAccountId);
            var revenueAccount = await _ledgerAccounts.GetByIdAsync(revenueAccountId, cancellationToken);

            if (revenueAccount is null)
            {
                return Result<CreateChargeCodeResult>.Failure(ApplicationError.NotFound(
                    nameof(command.RevenueAccountId),
                    "Revenue ledger account was not found."));
            }

            var revenueAccountErrors = ValidatePostingAccount(
                nameof(command.RevenueAccountId),
                revenueAccount,
                LedgerAccountType.Revenue,
                "Revenue ledger account");

            if (revenueAccountErrors.Count > 0)
            {
                return Result<CreateChargeCodeResult>.Failure(revenueAccountErrors);
            }

            var taxAccountId = command.TaxAccountId.HasValue
                ? LedgerAccountId.Create(command.TaxAccountId.Value)
                : (LedgerAccountId?)null;

            if (taxAccountId.HasValue)
            {
                var taxAccount = await _ledgerAccounts.GetByIdAsync(taxAccountId.Value, cancellationToken);

                if (taxAccount is null)
                {
                    return Result<CreateChargeCodeResult>.Failure(ApplicationError.NotFound(
                        nameof(command.TaxAccountId),
                        "Tax ledger account was not found."));
                }

                var taxAccountErrors = ValidatePostingAccount(
                    nameof(command.TaxAccountId),
                    taxAccount,
                    LedgerAccountType.Liability,
                    "Tax ledger account");

                if (taxAccountErrors.Count > 0)
                {
                    return Result<CreateChargeCodeResult>.Failure(taxAccountErrors);
                }
            }

            var chargeCode = ChargeCode.Create(
                ChargeCodeId.Create(_idGenerator.NewGuid()),
                code,
                command.Name,
                command.Description,
                Money.Of(command.DefaultUnitPriceAmount, command.CurrencyCode),
                revenueAccountId,
                taxAccountId,
                _clock.UtcNow);

            await _chargeCodes.AddAsync(chargeCode, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CreateChargeCodeResult>.Success(new CreateChargeCodeResult(
                chargeCode.Id.Value,
                chargeCode.Code.Value,
                chargeCode.Name,
                chargeCode.DefaultUnitPrice.Amount,
                chargeCode.DefaultUnitPrice.CurrencyCode,
                chargeCode.RevenueAccountId.Value,
                chargeCode.TaxAccountId?.Value,
                chargeCode.Status.ToString()));
        }
        catch (ArgumentException exception)
        {
            return Result<CreateChargeCodeResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private static IReadOnlyCollection<ApplicationError> ValidatePostingAccount(
        string fieldName,
        LedgerAccount ledgerAccount,
        LedgerAccountType expectedType,
        string accountRole)
    {
        var errors = new List<ApplicationError>();

        if (ledgerAccount.Type != expectedType)
        {
            errors.Add(ApplicationError.Validation(
                fieldName,
                $"{accountRole} must be a {expectedType.ToString().ToLowerInvariant()} account."));
        }

        if (!ledgerAccount.IsPostingAccount)
        {
            errors.Add(ApplicationError.Validation(
                fieldName,
                $"{accountRole} must be a posting account."));
        }

        if (ledgerAccount.Status != LedgerAccountStatus.Active)
        {
            errors.Add(ApplicationError.Validation(
                fieldName,
                $"{accountRole} must be active."));
        }

        return errors;
    }
}

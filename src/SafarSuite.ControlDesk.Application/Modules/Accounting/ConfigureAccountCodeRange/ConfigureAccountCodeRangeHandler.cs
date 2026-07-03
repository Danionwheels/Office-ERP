using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountCodeRanges;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountCodeRange;

public sealed class ConfigureAccountCodeRangeHandler
{
    private readonly IAccountCodeRangeRepository _ranges;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly ConfigureAccountCodeRangeValidator _validator;

    public ConfigureAccountCodeRangeHandler(
        IAccountCodeRangeRepository ranges,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        ConfigureAccountCodeRangeValidator validator)
    {
        _ranges = ranges;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AccountCodeRangeResult>> HandleAsync(
        ConfigureAccountCodeRangeCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<AccountCodeRangeResult>.Failure(validationErrors);
        }

        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<AccountCodeRangeResult>.Failure(companyError);
        }

        if (!Enum.TryParse<LedgerAccountType>(command.AccountType, true, out var accountType))
        {
            return Result<AccountCodeRangeResult>.Failure(ApplicationError.Validation(
                nameof(command.AccountType),
                "Account type is invalid."));
        }

        if (!Enum.TryParse<NormalBalance>(command.NormalBalance, true, out var normalBalance))
        {
            return Result<AccountCodeRangeResult>.Failure(ApplicationError.Validation(
                nameof(command.NormalBalance),
                "Normal balance is invalid."));
        }

        try
        {
            var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);
            var range = await _ranges.GetByCompanyAndRoleAsync(
                companyCode,
                command.Role,
                cancellationToken);

            if (range is null)
            {
                range = AccountCodeRange.Create(
                    AccountCodeRangeId.Create(_idGenerator.NewGuid()),
                    companyCode,
                    command.Role,
                    command.DisplayName,
                    command.SearchPrefix,
                    command.RangeStart,
                    command.RangeEnd,
                    command.CodeLength,
                    accountType,
                    normalBalance,
                    command.IsPostingAccount,
                    command.ParentCode,
                    command.IsActive,
                    _clock.UtcNow);

                await _ranges.AddAsync(range, cancellationToken);
            }
            else
            {
                range.Update(
                    command.DisplayName,
                    command.SearchPrefix,
                    command.RangeStart,
                    command.RangeEnd,
                    command.CodeLength,
                    accountType,
                    normalBalance,
                    command.IsPostingAccount,
                    command.ParentCode,
                    command.IsActive,
                    _clock.UtcNow);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AccountCodeRangeResult>.Success(ToResult(range));
        }
        catch (ArgumentException exception)
        {
            return Result<AccountCodeRangeResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private static AccountCodeRangeResult ToResult(AccountCodeRange range)
    {
        return new AccountCodeRangeResult(
            range.Id.Value,
            range.CompanyCode,
            range.Role,
            range.DisplayName,
            range.SearchPrefix,
            range.RangeStart,
            range.RangeEnd,
            range.CodeLength,
            range.AccountType.ToString(),
            range.NormalBalance.ToString(),
            range.IsPostingAccount,
            range.ParentCode,
            range.IsActive);
    }
}

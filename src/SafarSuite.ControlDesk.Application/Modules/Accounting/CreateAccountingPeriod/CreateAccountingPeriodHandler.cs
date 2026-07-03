using System.Globalization;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateAccountingPeriod;

public sealed class CreateAccountingPeriodHandler
{
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateAccountingPeriodValidator _validator;

    public CreateAccountingPeriodHandler(
        IAccountingPeriodRepository periods,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateAccountingPeriodValidator validator)
    {
        _periods = periods;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AccountingPeriodResult>> HandleAsync(
        CreateAccountingPeriodCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<AccountingPeriodResult>.Failure(validationErrors);
        }

        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<AccountingPeriodResult>.Failure(companyError);
        }

        try
        {
            var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);

            if (await _periods.GetByCompanyAndStartDateAsync(companyCode, command.StartsOn, cancellationToken) is not null)
            {
                return Result<AccountingPeriodResult>.Failure(ApplicationError.Conflict(
                    nameof(command.StartsOn),
                    "An accounting period already starts on this date."));
            }

            var overlaps = await _periods.ListByCompanyAsync(
                companyCode,
                command.StartsOn,
                command.EndsOn,
                cancellationToken);

            if (overlaps.Count > 0)
            {
                return Result<AccountingPeriodResult>.Failure(ApplicationError.Conflict(
                    nameof(command.StartsOn),
                    "Accounting period dates overlap an existing period."));
            }

            var period = AccountingPeriod.Create(
                AccountingPeriodId.Create(_idGenerator.NewGuid()),
                companyCode,
                string.IsNullOrWhiteSpace(command.Name)
                    ? FormatPeriodName(command.StartsOn)
                    : command.Name,
                command.StartsOn,
                command.EndsOn,
                _clock.UtcNow);

            await _periods.AddAsync(period, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AccountingPeriodResult>.Success(ListAccountingPeriodsHandler.ToResult(period));
        }
        catch (ArgumentException exception)
        {
            return Result<AccountingPeriodResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private static string FormatPeriodName(DateOnly startsOn)
    {
        return startsOn.ToString("MMM yyyy", CultureInfo.InvariantCulture);
    }
}

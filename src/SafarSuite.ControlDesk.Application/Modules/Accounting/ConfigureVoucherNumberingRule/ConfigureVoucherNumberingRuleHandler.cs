using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListVoucherNumberingRules;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureVoucherNumberingRule;

public sealed class ConfigureVoucherNumberingRuleHandler
{
    private readonly IVoucherNumberingRuleRepository _rules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public ConfigureVoucherNumberingRuleHandler(
        IVoucherNumberingRuleRepository rules,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _rules = rules;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<VoucherNumberingRuleResult>> HandleAsync(
        ConfigureVoucherNumberingRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<VoucherNumberingRuleResult>.Failure(companyError);
        }

        if (!Enum.TryParse<JournalSourceType>(command.SourceType, ignoreCase: true, out var sourceType))
        {
            return Result<VoucherNumberingRuleResult>.Failure(ApplicationError.Validation(
                nameof(command.SourceType),
                $"Journal source type '{command.SourceType}' is not supported."));
        }

        try
        {
            var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);
            var rule = await _rules.GetByCompanyAndSourceTypeAsync(
                companyCode,
                sourceType,
                cancellationToken);

            if (rule is null)
            {
                rule = VoucherNumberingRule.Create(
                    VoucherNumberingRuleId.Create(_idGenerator.NewGuid()),
                    companyCode,
                    sourceType,
                    command.Prefix,
                    command.NumberPaddingWidth,
                    command.IsActive,
                    _clock.UtcNow);

                await _rules.AddAsync(rule, cancellationToken);
            }
            else
            {
                rule.Configure(
                    command.Prefix,
                    command.NumberPaddingWidth,
                    command.IsActive,
                    _clock.UtcNow);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<VoucherNumberingRuleResult>.Success(
                ListVoucherNumberingRulesHandler.ToResult(rule));
        }
        catch (ArgumentException exception)
        {
            return Result<VoucherNumberingRuleResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}

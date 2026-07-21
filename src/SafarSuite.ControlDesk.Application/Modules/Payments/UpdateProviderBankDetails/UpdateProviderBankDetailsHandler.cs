using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.UpdateProviderBankDetails;

public sealed class UpdateProviderBankDetailsHandler
{
    private readonly IProviderBankDetailsRepository _bankDetails;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly ProviderBankDetailsCloudOutboxMessageFactory _outboxMessageFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateProviderBankDetailsHandler(
        IProviderBankDetailsRepository bankDetails,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        ProviderBankDetailsCloudOutboxMessageFactory outboxMessageFactory,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _bankDetails = bankDetails;
        _cloudOutboxMessages = cloudOutboxMessages;
        _outboxMessageFactory = outboxMessageFactory;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProviderBankDetailsResult>> HandleAsync(
        UpdateProviderBankDetailsCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var details = await _bankDetails.GetAsync(token);
                    var isNew = details is null;
                    details ??= ProviderBankDetails.CreateEmpty();
                    details.Update(
                        command.BankName,
                        command.AccountTitle,
                        command.AccountNumber,
                        command.Iban,
                        command.BranchOrRoutingInfo,
                        _clock.UtcNow);

                    if (isNew)
                    {
                        await _bankDetails.AddAsync(details, token);
                    }

                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreateUpdated(details),
                        token);

                    return ProviderBankDetailsResult.From(details);
                },
                cancellationToken);

            return Result<ProviderBankDetailsResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<ProviderBankDetailsResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}

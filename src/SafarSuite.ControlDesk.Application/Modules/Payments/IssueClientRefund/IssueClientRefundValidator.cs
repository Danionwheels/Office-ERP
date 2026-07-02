using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;

public sealed class IssueClientRefundValidator
{
    public IReadOnlyCollection<ApplicationError> Validate(IssueClientRefundCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Method))
        {
            errors.Add(ApplicationError.Validation(nameof(command.Method), "Refund method is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Reference))
        {
            errors.Add(ApplicationError.Validation(nameof(command.Reference), "Refund reference is required."));
        }

        if (command.Amount <= 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.Amount), "Refund amount must be positive."));
        }

        if (string.IsNullOrWhiteSpace(command.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(nameof(command.CurrencyCode), "Currency code is required."));
        }

        if (command.CashOrBankAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.CashOrBankAccountId),
                "Cash or bank account id is required."));
        }

        if (command.AccountsReceivableAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable account id is required."));
        }

        if (!string.IsNullOrWhiteSpace(command.Note) && command.Note.Trim().Length > 1000)
        {
            errors.Add(ApplicationError.Validation(nameof(command.Note), "Refund note cannot exceed 1000 characters."));
        }

        return errors;
    }
}

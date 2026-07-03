using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PostManualJournalEntry;

public sealed class PostManualJournalEntryValidator : IValidator<PostManualJournalEntryCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(PostManualJournalEntryCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.EntryDate == default)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.EntryDate),
                "Journal entry date is required."));
        }

        if (string.IsNullOrWhiteSpace(value.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.CurrencyCode),
                "Journal currency code is required."));
        }

        if (value.Lines is null)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Lines),
                "Manual journal entry lines are required."));

            return errors;
        }

        if (value.Lines.Count < 2)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Lines),
                "Manual journal entry must include at least two lines."));
        }

        var lineNumber = 0;

        foreach (var line in value.Lines)
        {
            lineNumber++;
            var target = $"{nameof(value.Lines)}[{lineNumber}]";

            if (line.LedgerAccountId == Guid.Empty)
            {
                errors.Add(ApplicationError.Validation(
                    target,
                    "Journal line ledger account is required."));
            }

            if (line.Debit < 0 || line.Credit < 0)
            {
                errors.Add(ApplicationError.Validation(
                    target,
                    "Journal line debit and credit amounts cannot be negative."));
            }

            if (line.Debit > 0 && line.Credit > 0)
            {
                errors.Add(ApplicationError.Validation(
                    target,
                    "Journal line cannot include both debit and credit amounts."));
            }

            if (line.Debit == 0 && line.Credit == 0)
            {
                errors.Add(ApplicationError.Validation(
                    target,
                    "Journal line must include either a debit or credit amount."));
            }
        }

        var totalDebit = value.Lines.Sum(line => decimal.Round(line.Debit, 2));
        var totalCredit = value.Lines.Sum(line => decimal.Round(line.Credit, 2));

        if (totalDebit <= 0 || totalCredit <= 0)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Lines),
                "Manual journal entry must include debit and credit amounts."));
        }

        if (totalDebit != totalCredit)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Lines),
                "Manual journal entry debits and credits must balance."));
        }

        return errors;
    }
}

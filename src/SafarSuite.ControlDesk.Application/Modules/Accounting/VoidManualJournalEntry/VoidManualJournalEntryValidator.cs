using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.VoidManualJournalEntry;

public sealed class VoidManualJournalEntryValidator : IValidator<VoidManualJournalEntryCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(VoidManualJournalEntryCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.JournalEntryId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.JournalEntryId),
                "Journal entry id is required."));
        }

        if (value.VoidDate == default)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.VoidDate),
                "Void date is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Reason))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Reason),
                "Void reason is required."));
        }

        return errors;
    }
}

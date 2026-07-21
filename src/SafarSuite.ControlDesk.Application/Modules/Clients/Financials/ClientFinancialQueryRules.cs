using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Financials;

internal static class ClientFinancialQueryRules
{
    public const int MaximumPageSize = 100;
    public const int MaximumSearchLength = 128;

    public static ApplicationError? ValidateClientAndDates(
        Guid clientId,
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        if (clientId == Guid.Empty)
        {
            return ApplicationError.Validation(nameof(clientId), "Client id is required.");
        }

        return fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value
            ? ApplicationError.Validation(nameof(fromDate), "From date cannot be after to date.")
            : null;
    }

    public static ApplicationError? ValidatePage(int take, string search)
    {
        if (take is < 1 or > MaximumPageSize)
        {
            return ApplicationError.Validation(
                nameof(take),
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        return search.Length > MaximumSearchLength
            ? ApplicationError.Validation(
                nameof(search),
                $"Search text must be {MaximumSearchLength} characters or fewer.")
            : null;
    }

    public static string NormalizeSearch(string? search)
    {
        return search?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IRevenueSummaryReader
{
    Task<IReadOnlyCollection<RevenueSummaryPeriodReadModel>> ReadAsync(
        RevenueSummaryReadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record RevenueSummaryReadRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    string Period,
    string CurrencyCode);

public sealed record RevenueSummaryPeriodReadModel(
    DateOnly PeriodStart,
    decimal Debit,
    decimal Credit,
    int ActivityCount);

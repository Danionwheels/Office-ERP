namespace SafarSuite.ControlDesk.Application.Modules.CommandCenter.Ports;

public interface IClientWorkQueueReader
{
    Task<ClientWorkQueueReadPage> ReadPageAsync(
        ClientWorkQueueReadRequest request,
        CancellationToken cancellationToken = default);
}

public enum ClientWorkQueueLane
{
    All,
    Setup,
    Billing,
    Payments,
    Access,
    Cloud,
    Overview
}

public enum ClientWorkQueueSort
{
    Priority,
    Client,
    Action
}

public sealed record ClientWorkQueueReadRequest(
    string Search,
    ClientWorkQueueLane Lane,
    ClientWorkQueueSort Sort,
    string? AfterSortValue,
    int? AfterPriority,
    string? AfterCode,
    Guid? AfterClientId,
    int Take);

public sealed record ClientWorkQueueReadPage(
    IReadOnlyCollection<ClientWorkQueueReadItem> Items,
    long FilteredCount,
    ClientWorkQueueReadSummary Summary);

public sealed record ClientWorkQueueReadItem(
    Guid ClientId,
    string Code,
    string Name,
    string Status,
    string ActionLabel,
    string Detail,
    string Tab,
    string Tone,
    int Priority,
    string SortValue);

public sealed record ClientWorkQueueReadSummary(
    long TotalCount,
    long SetupCount,
    long BillingCount,
    long PaymentsCount,
    long AccessCount,
    long CloudCount,
    long OverviewCount);

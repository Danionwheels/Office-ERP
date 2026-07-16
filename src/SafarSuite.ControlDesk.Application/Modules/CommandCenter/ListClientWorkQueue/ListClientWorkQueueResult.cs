namespace SafarSuite.ControlDesk.Application.Modules.CommandCenter.ListClientWorkQueue;

public sealed record ListClientWorkQueueResult(
    IReadOnlyCollection<ClientWorkQueueItemResult> Items,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount,
    ClientWorkQueueSummaryResult Summary);

public sealed record ClientWorkQueueItemResult(
    Guid ClientId,
    string Code,
    string Name,
    string Status,
    string ActionLabel,
    string Detail,
    string Tab,
    string Tone,
    int Priority);

public sealed record ClientWorkQueueSummaryResult(
    long TotalCount,
    long SetupCount,
    long BillingCount,
    long PaymentsCount,
    long AccessCount,
    long CloudCount,
    long OverviewCount);

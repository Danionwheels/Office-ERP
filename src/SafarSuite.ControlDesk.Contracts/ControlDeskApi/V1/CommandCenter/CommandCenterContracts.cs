namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.CommandCenter;

public sealed record ListClientWorkQueueResponse(
    IReadOnlyCollection<ClientWorkQueueItemResponse> Items,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount,
    ClientWorkQueueSummaryResponse Summary);

public sealed record ClientWorkQueueItemResponse(
    Guid ClientId,
    string Code,
    string Name,
    string Status,
    string ActionLabel,
    string Detail,
    string Tab,
    string Tone,
    int Priority);

public sealed record ClientWorkQueueSummaryResponse(
    long TotalCount,
    long SetupCount,
    long BillingCount,
    long PaymentsCount,
    long AccessCount,
    long CloudCount,
    long OverviewCount);

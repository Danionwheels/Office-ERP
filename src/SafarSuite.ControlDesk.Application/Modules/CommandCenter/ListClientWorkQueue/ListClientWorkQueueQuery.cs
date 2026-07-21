namespace SafarSuite.ControlDesk.Application.Modules.CommandCenter.ListClientWorkQueue;

public sealed record ListClientWorkQueueQuery(
    string? Lane = null,
    string? Search = null,
    string? Sort = null,
    int Take = 25,
    string? Cursor = null);

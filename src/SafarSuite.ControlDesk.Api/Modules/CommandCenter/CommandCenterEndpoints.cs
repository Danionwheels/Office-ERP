using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Api.Modules.Auth;
using SafarSuite.ControlDesk.Application.Modules.CommandCenter.ListClientWorkQueue;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.CommandCenter;

namespace SafarSuite.ControlDesk.Api.Modules.CommandCenter;

public static class CommandCenterEndpoints
{
    public static IEndpointRouteBuilder MapCommandCenterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/api/v1/command-center/client-work", ListClientWorkAsync)
            .WithTags("Command Center")
            .RequireAuthorization(ControlDeskPolicies.CommandCenterRead);

        return endpoints;
    }

    private static async Task<IResult> ListClientWorkAsync(
        string? lane,
        string? search,
        string? sort,
        int? take,
        string? cursor,
        ListClientWorkQueueHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientWorkQueueQuery(lane, search, sort, take ?? 25, cursor),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListClientWorkQueueResponse(
            result.Value.Items.Select(item => new ClientWorkQueueItemResponse(
                item.ClientId,
                item.Code,
                item.Name,
                item.Status,
                item.ActionLabel,
                item.Detail,
                item.Tab,
                item.Tone,
                item.Priority)).ToArray(),
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount,
            new ClientWorkQueueSummaryResponse(
                result.Value.Summary.TotalCount,
                result.Value.Summary.SetupCount,
                result.Value.Summary.BillingCount,
                result.Value.Summary.PaymentsCount,
                result.Value.Summary.AccessCount,
                result.Value.Summary.CloudCount,
                result.Value.Summary.OverviewCount)));
    }
}

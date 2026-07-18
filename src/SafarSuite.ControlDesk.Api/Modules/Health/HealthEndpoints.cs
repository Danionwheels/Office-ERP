using SafarSuite.ControlDesk.Application.Modules.Diagnostics.GetOfficeReadiness;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Health;

namespace SafarSuite.ControlDesk.Api.Modules.Health;

public static class HealthEndpoints
{
    private const string ServiceName = "SafarSuite Control Desk API";

    public static IEndpointRouteBuilder MapControlDeskHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", GetLiveness)
            .AllowAnonymous();
        endpoints.MapGet("/health/live", GetLiveness)
            .AllowAnonymous();
        endpoints.MapGet("/ready", GetReadinessAsync)
            .AllowAnonymous();
        endpoints.MapGet("/health/ready", GetReadinessAsync)
            .AllowAnonymous();

        return endpoints;
    }

    private static IResult GetLiveness(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";

        return Results.Ok(new HealthResponse(
            ServiceName,
            "Healthy",
            DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> GetReadinessAsync(
        HttpContext context,
        GetOfficeReadinessHandler handler,
        OfficeReadinessTransitionRecorder transitionRecorder,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var result = await handler.HandleAsync(cancellationToken);
        transitionRecorder.Record(result);
        var response = new ReadinessResponse(
            ServiceName,
            result.Status,
            result.Code,
            DateTimeOffset.UtcNow,
            new DatabaseReadinessResponse(
                result.Database.IsReady ? "Ready" : "NotReady",
                result.Database.Code,
                result.Database.PersistenceProvider,
                result.Database.ConnectivityStatus.ToString(),
                result.Database.MigrationStatus.ToString()));

        return Results.Json(
            response,
            statusCode: result.IsReady
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable);
    }
}

using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.GetOfficeReadiness;

public sealed class GetOfficeReadinessHandler(IOfficeDatabaseReadinessProbe databaseReadiness)
{
    public async Task<GetOfficeReadinessResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var database = await databaseReadiness.CheckAsync(cancellationToken);

        return new GetOfficeReadinessResult(
            database.IsReady,
            database.IsReady
                ? GetOfficeReadinessResult.ReadyStatus
                : GetOfficeReadinessResult.NotReadyStatus,
            database.Code,
            database);
    }
}

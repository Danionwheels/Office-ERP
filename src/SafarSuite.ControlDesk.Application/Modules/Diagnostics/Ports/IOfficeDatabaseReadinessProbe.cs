namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

public interface IOfficeDatabaseReadinessProbe
{
    Task<OfficeDatabaseReadinessResult> CheckAsync(
        CancellationToken cancellationToken = default);
}

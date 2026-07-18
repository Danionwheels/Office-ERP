namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

public interface IControlCloudReachabilityProbe
{
    Task<ControlCloudReachabilityResult> CheckAsync(
        CancellationToken cancellationToken = default);
}

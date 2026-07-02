using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Application.Registration.Ports;

public interface ILocalServerBootstrapConfigurationStore
{
    Task<LocalServerBootstrapConfiguration?> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LocalServerBootstrapConfiguration configuration,
        CancellationToken cancellationToken = default);
}

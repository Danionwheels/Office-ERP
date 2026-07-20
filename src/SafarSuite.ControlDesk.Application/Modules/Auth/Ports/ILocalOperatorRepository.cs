using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth.Ports;

public interface ILocalOperatorRepository
{
    Task<LocalOperator?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);
}

using SafarSuite.LocalServer.Domain.Commands;

namespace SafarSuite.LocalServer.Application.Commands.Ports;

public interface ILocalServerAppActivationRevocationStore
{
    Task SaveAsync(
        LocalServerAppActivationRevocationRecord record,
        CancellationToken cancellationToken = default);

    Task<LocalServerAppActivationRevocationRecord?> GetByActivationIssueIdAsync(
        Guid activationIssueId,
        CancellationToken cancellationToken = default);
}

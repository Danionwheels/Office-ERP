using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth;

public sealed class LocalOperatorAdministratorGuard(ILocalOperatorRepository operators)
{
    public async Task<bool> IsAuthorizedAsync(
        Guid actingOperatorId,
        CancellationToken cancellationToken = default)
    {
        if (actingOperatorId == Guid.Empty)
        {
            return false;
        }

        var actor = await operators.GetByIdAsync(
            LocalOperatorId.Create(actingOperatorId),
            cancellationToken);

        return actor is not null
            && actor.Status == LocalOperatorStatus.Active
            && actor.Roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal)
            && actor.Scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal);
    }
}

using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryOfficeDatabaseReadinessProbe : IOfficeDatabaseReadinessProbe
{
    public Task<OfficeDatabaseReadinessResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new OfficeDatabaseReadinessResult(
            true,
            OfficeDatabaseReadinessCodes.EphemeralPersistenceReady,
            "InMemory",
            OfficeDatabaseConnectivityStatus.NotApplicable,
            OfficeDatabaseMigrationStatus.NotApplicable,
            0,
            0,
            0,
            0));
    }
}

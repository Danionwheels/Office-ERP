using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfOfficeDatabaseReadinessProbe(ControlDeskDbContext dbContext)
    : IOfficeDatabaseReadinessProbe
{
    private static readonly TimeSpan InspectionTimeout = TimeSpan.FromSeconds(5);

    public async Task<OfficeDatabaseReadinessResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        using var inspectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        inspectionCancellation.CancelAfter(InspectionTimeout);

        var knownMigrations = Array.Empty<string>();
        var appliedMigrations = Array.Empty<string>();
        var connectivityEstablished = false;

        try
        {
            knownMigrations = dbContext.Database.GetMigrations().ToArray();

            if (!await dbContext.Database.CanConnectAsync(inspectionCancellation.Token))
            {
                return NotReady(
                    OfficeDatabaseReadinessCodes.DatabaseUnavailable,
                    OfficeDatabaseConnectivityStatus.Unavailable,
                    OfficeDatabaseMigrationStatus.Indeterminate);
            }

            connectivityEstablished = true;
            appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(
                    inspectionCancellation.Token))
                .ToArray();

            var knownMigrationSet = knownMigrations.ToHashSet(StringComparer.Ordinal);
            var appliedMigrationSet = appliedMigrations.ToHashSet(StringComparer.Ordinal);
            var pendingMigrations = knownMigrations
                .Where(migration => !appliedMigrationSet.Contains(migration))
                .ToArray();
            var unknownAppliedMigrations = appliedMigrations
                .Where(migration => !knownMigrationSet.Contains(migration))
                .ToArray();

            if (unknownAppliedMigrations.Length > 0)
            {
                return NotReady(
                    OfficeDatabaseReadinessCodes.UnknownMigrationsApplied,
                    OfficeDatabaseConnectivityStatus.Ready,
                    OfficeDatabaseMigrationStatus.UnknownApplied,
                    knownMigrations.Length,
                    appliedMigrations.Length,
                    pendingMigrations.Length,
                    unknownAppliedMigrations.Length);
            }

            if (pendingMigrations.Length > 0)
            {
                return NotReady(
                    OfficeDatabaseReadinessCodes.MigrationsPending,
                    OfficeDatabaseConnectivityStatus.Ready,
                    OfficeDatabaseMigrationStatus.Pending,
                    knownMigrations.Length,
                    appliedMigrations.Length,
                    pendingMigrations.Length,
                    unknownAppliedMigrations.Length);
            }

            if (!knownMigrations.SequenceEqual(appliedMigrations, StringComparer.Ordinal))
            {
                return NotReady(
                    OfficeDatabaseReadinessCodes.MigrationOrderMismatch,
                    OfficeDatabaseConnectivityStatus.Ready,
                    OfficeDatabaseMigrationStatus.OrderMismatch,
                    knownMigrations.Length,
                    appliedMigrations.Length,
                    0,
                    0);
            }

            return new OfficeDatabaseReadinessResult(
                true,
                OfficeDatabaseReadinessCodes.Ready,
                "Postgres",
                OfficeDatabaseConnectivityStatus.Ready,
                OfficeDatabaseMigrationStatus.Current,
                knownMigrations.Length,
                appliedMigrations.Length,
                0,
                0);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return NotReady(
                OfficeDatabaseReadinessCodes.InspectionTimedOut,
                connectivityEstablished
                    ? OfficeDatabaseConnectivityStatus.Ready
                    : OfficeDatabaseConnectivityStatus.Indeterminate,
                OfficeDatabaseMigrationStatus.TimedOut);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return NotReady(
                OfficeDatabaseReadinessCodes.InspectionFailed,
                connectivityEstablished
                    ? OfficeDatabaseConnectivityStatus.Ready
                    : OfficeDatabaseConnectivityStatus.Indeterminate,
                OfficeDatabaseMigrationStatus.InspectionFailed);
        }
    }

    private static OfficeDatabaseReadinessResult NotReady(
        string code,
        OfficeDatabaseConnectivityStatus connectivityStatus,
        OfficeDatabaseMigrationStatus migrationStatus,
        int? knownMigrationCount = null,
        int? appliedMigrationCount = null,
        int? pendingMigrationCount = null,
        int? unknownAppliedMigrationCount = null)
    {
        return new OfficeDatabaseReadinessResult(
            false,
            code,
            "Postgres",
            connectivityStatus,
            migrationStatus,
            knownMigrationCount,
            appliedMigrationCount,
            pendingMigrationCount,
            unknownAppliedMigrationCount);
    }
}

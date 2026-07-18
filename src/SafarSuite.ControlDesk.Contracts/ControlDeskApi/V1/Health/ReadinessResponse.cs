namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Health;

public sealed record ReadinessResponse(
    string Service,
    string Status,
    string Code,
    DateTimeOffset CheckedAtUtc,
    DatabaseReadinessResponse Database);

public sealed record DatabaseReadinessResponse(
    string Status,
    string Code,
    string PersistenceProvider,
    string ConnectivityStatus,
    string MigrationStatus);

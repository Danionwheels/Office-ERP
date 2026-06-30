namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Health;

public sealed record HealthResponse(
    string Service,
    string Status,
    DateTimeOffset CheckedAtUtc);

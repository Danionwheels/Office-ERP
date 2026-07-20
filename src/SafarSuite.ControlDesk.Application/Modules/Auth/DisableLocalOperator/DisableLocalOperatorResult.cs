namespace SafarSuite.ControlDesk.Application.Modules.Auth.DisableLocalOperator;

public sealed record DisableLocalOperatorResult(
    Guid OperatorId,
    string Status,
    long SecurityVersion,
    DateTimeOffset UpdatedAtUtc);

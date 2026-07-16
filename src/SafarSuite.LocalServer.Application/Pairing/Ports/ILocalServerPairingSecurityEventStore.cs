using SafarSuite.LocalServer.Domain.Pairing;

namespace SafarSuite.LocalServer.Application.Pairing.Ports;

public interface ILocalServerPairingSecurityEventStore
{
    Task<IReadOnlyCollection<LocalServerPairingSecurityEvent>> ListEventsAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task RecordEventAsync(
        LocalServerPairingSecurityEvent record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LocalServerPairingAbuseSourceDecision>> ListSourceDecisionsAsync(
        CancellationToken cancellationToken = default);

    Task<LocalServerPairingAbuseSourceDecision?> GetActiveSourceDecisionAsync(
        string sourceKey,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task SaveSourceDecisionAsync(
        LocalServerPairingAbuseSourceDecision decision,
        CancellationToken cancellationToken = default);
}

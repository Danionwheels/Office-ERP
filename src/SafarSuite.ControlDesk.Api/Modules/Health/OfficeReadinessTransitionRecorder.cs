using SafarSuite.ControlDesk.Application.Modules.Diagnostics.GetOfficeReadiness;

namespace SafarSuite.ControlDesk.Api.Modules.Health;

public sealed class OfficeReadinessTransitionRecorder(
    ILogger<OfficeReadinessTransitionRecorder> logger)
{
    private readonly object _sync = new();
    private bool? _lastReady;
    private string? _lastCode;

    public void Record(GetOfficeReadinessResult readiness)
    {
        lock (_sync)
        {
            if (_lastReady == readiness.IsReady
                && string.Equals(_lastCode, readiness.Code, StringComparison.Ordinal))
            {
                return;
            }

            var wasReady = _lastReady;
            _lastReady = readiness.IsReady;
            _lastCode = readiness.Code;

            if (readiness.IsReady)
            {
                logger.LogInformation(
                    "Control Desk readiness is available. ReadinessCode={ReadinessCode} EventCode={EventCode}",
                    readiness.Code,
                    wasReady == false
                        ? "OfficeReadinessRestored"
                        : "OfficeReadinessConfirmed");
                return;
            }

            logger.LogWarning(
                "Control Desk readiness is unavailable. ReadinessCode={ReadinessCode} EventCode={EventCode}",
                readiness.Code,
                "OfficeReadinessUnavailable");
        }
    }
}

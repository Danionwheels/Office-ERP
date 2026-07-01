using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class ConfiguredCloudOutboxPublishPolicy : ICloudOutboxPublishPolicy
{
    private readonly IOptions<ControlCloudPublisherOptions> _options;

    public ConfiguredCloudOutboxPublishPolicy(IOptions<ControlCloudPublisherOptions> options)
    {
        _options = options;
    }

    public int MaximumAttemptCount
    {
        get
        {
            var configured = _options.Value.MaximumAttemptCount;

            return configured < 1 ? 1 : configured;
        }
    }

    public TimeSpan RetryDelay
    {
        get
        {
            var configuredSeconds = _options.Value.RetryDelaySeconds;

            return TimeSpan.FromSeconds(configuredSeconds < 1 ? 60 : configuredSeconds);
        }
    }
}

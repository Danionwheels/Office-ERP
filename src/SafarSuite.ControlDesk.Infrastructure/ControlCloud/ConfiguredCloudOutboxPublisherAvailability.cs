using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class ConfiguredCloudOutboxPublisherAvailability(
    IOptions<ControlCloudPublisherOptions> options,
    IHostEnvironment environment) : ICloudOutboxPublisherAvailability
{
    private static readonly CloudOutboxPublisherAvailabilitySnapshot Unavailable =
        new(false, false, "PublisherNotConfigured");

    public CloudOutboxPublisherAvailabilitySnapshot GetSnapshot()
    {
        var configured = options.Value;
        var developmentBoundary = environment.IsDevelopment()
                                  || environment.IsEnvironment("Testing");

        if (configured.Mode.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            return developmentBoundary
                ? new CloudOutboxPublisherAvailabilitySnapshot(
                    true,
                    false,
                    "DevelopmentLocalPublisher")
                : Unavailable;
        }

        if (!configured.Mode.Equals("Http", StringComparison.OrdinalIgnoreCase))
        {
            return Unavailable;
        }

        var requireHttps = configured.RequireHttps || !developmentBoundary;

        return ControlCloudPublisherEndpointPolicy.IsConfigured(configured, requireHttps)
               && ControlCloudPublisherEndpointPolicy.HasValidEnvelopeConfiguration(
                   configured,
                   developmentBoundary)
            ? new CloudOutboxPublisherAvailabilitySnapshot(true, true, "Ready")
            : Unavailable;
    }
}

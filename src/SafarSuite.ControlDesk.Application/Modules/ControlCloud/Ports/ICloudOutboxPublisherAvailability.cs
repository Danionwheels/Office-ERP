namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxPublisherAvailability
{
    CloudOutboxPublisherAvailabilitySnapshot GetSnapshot();
}

public sealed record CloudOutboxPublisherAvailabilitySnapshot(
    bool CanPublish,
    bool CanPublishAutomatically,
    string Code);

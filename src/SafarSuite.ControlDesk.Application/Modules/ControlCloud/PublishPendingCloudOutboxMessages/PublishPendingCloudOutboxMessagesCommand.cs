namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;

public sealed record PublishPendingCloudOutboxMessagesCommand(
    int BatchSize);

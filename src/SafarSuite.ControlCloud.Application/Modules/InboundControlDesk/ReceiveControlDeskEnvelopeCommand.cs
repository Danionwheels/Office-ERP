using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;

public sealed record ReceiveControlDeskEnvelopeCommand(ControlCloudEnvelope Envelope);

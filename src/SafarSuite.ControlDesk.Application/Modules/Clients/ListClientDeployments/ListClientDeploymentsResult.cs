namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientDeployments;

public sealed record ListClientDeploymentsResult(
    Guid ClientId,
    IReadOnlyCollection<ClientDeploymentResult> Deployments);

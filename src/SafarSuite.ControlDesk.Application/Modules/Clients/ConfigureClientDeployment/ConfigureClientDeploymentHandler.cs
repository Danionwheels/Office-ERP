using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientDeployment;

public sealed class ConfigureClientDeploymentHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientDeploymentRepository _deployments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly ConfigureClientDeploymentValidator _validator;

    public ConfigureClientDeploymentHandler(
        IClientRepository clients,
        IClientDeploymentRepository deployments,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        ConfigureClientDeploymentValidator validator)
    {
        _clients = clients;
        _deployments = deployments;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ClientDeploymentResult>> HandleAsync(
        ConfigureClientDeploymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ClientDeploymentResult>.Failure(validationErrors);
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<ClientDeploymentResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var deployments = await _deployments.ListByClientIdAsync(clientId, cancellationToken);
            var deployment = deployments
                .SingleOrDefault(item => item.InstallationId.Equals(
                    command.InstallationId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            var now = _clock.UtcNow;
            var hasOtherPrimary = deployments.Any(item =>
                item.IsPrimary && (deployment is null || item.Id != deployment.Id));
            var shouldBePrimary = command.IsPrimary || deployments.Count == 0 || !hasOtherPrimary;

            if (deployment is null)
            {
                deployment = ClientDeployment.Create(
                    ClientDeploymentId.Create(_idGenerator.NewGuid()),
                    clientId,
                    command.DisplayName,
                    command.InstallationId,
                    ControlCloudBootstrapModes.NormalizeOrDefault(command.BootstrapMode),
                    SafarSuiteClientDeploymentModes.NormalizeOrDefault(command.ClientDeploymentMode),
                    command.SiteId,
                    SafarSuiteDeploymentSiteRoles.NormalizeOrDefault(
                        command.SiteRole,
                        command.ClientDeploymentMode),
                    command.ParentSiteId,
                    command.BranchCode,
                    command.SyncTopologyId,
                    command.LocalServerVersion,
                    command.SafarSuiteAppVersion,
                    shouldBePrimary,
                    now);

                await _deployments.AddAsync(deployment, cancellationToken);
            }
            else
            {
                deployment.UpdateProfile(
                    command.DisplayName,
                    ControlCloudBootstrapModes.NormalizeOrDefault(command.BootstrapMode),
                    SafarSuiteClientDeploymentModes.NormalizeOrDefault(command.ClientDeploymentMode),
                    command.SiteId,
                    SafarSuiteDeploymentSiteRoles.NormalizeOrDefault(
                        command.SiteRole,
                        command.ClientDeploymentMode),
                    command.ParentSiteId,
                    command.BranchCode,
                    command.SyncTopologyId,
                    command.LocalServerVersion,
                    command.SafarSuiteAppVersion,
                    now);
                deployment.SetPrimary(shouldBePrimary, now);
            }

            if (shouldBePrimary)
            {
                foreach (var otherDeployment in deployments.Where(item => item.Id != deployment.Id))
                {
                    if (otherDeployment.IsPrimary)
                    {
                        otherDeployment.SetPrimary(false, now);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ClientDeploymentResult>.Success(ToResult(deployment));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientDeploymentResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private static ClientDeploymentResult ToResult(ClientDeployment deployment)
    {
        return ClientDeploymentResultMapper.ToResult(deployment);
    }
}

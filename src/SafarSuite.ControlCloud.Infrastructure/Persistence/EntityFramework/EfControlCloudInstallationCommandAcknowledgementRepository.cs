using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudInstallationCommandAcknowledgementRepository
    : IControlCloudInstallationCommandAcknowledgementRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudInstallationCommandAcknowledgementRepository(
        ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ControlCloudInstallationCommandAcknowledgement acknowledgement,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.InstallationCommandAcknowledgements.AddAsync(
            new ControlCloudInstallationCommandAcknowledgementEntity
            {
                AcknowledgementId = acknowledgement.AcknowledgementId,
                CommandId = acknowledgement.CommandId,
                ClientId = acknowledgement.ClientId,
                InstallationId = acknowledgement.InstallationId,
                CommandVersion = acknowledgement.CommandVersion,
                ResultStatus = acknowledgement.ResultStatus,
                Detail = acknowledgement.Detail,
                PayloadJson = acknowledgement.PayloadJson,
                AcknowledgedAtUtc = acknowledgement.AcknowledgedAtUtc
            },
            cancellationToken);
    }
}

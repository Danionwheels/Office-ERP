using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalAttachmentRepository
{
    Task<ControlCloudClientPortalAttachment?> GetByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlCloudClientPortalAttachment attachment,
        CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientPortalAttachmentRepository : IClientPortalAttachmentRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfClientPortalAttachmentRepository(ControlCloudDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<ControlCloudClientPortalAttachment?> GetByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                attachment => attachment.AttachmentId == attachmentId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public Task AddAsync(
        ControlCloudClientPortalAttachment attachment,
        CancellationToken cancellationToken = default) =>
        _dbContext.ClientPortalAttachments.AddAsync(ToEntity(attachment), cancellationToken).AsTask();

    private static ControlCloudClientPortalAttachment ToDomain(
        ControlCloudClientPortalAttachmentEntity entity) => new()
    {
        AttachmentId = entity.AttachmentId,
        ClientId = entity.ClientId,
        UploadedByUserId = entity.UploadedByUserId,
        FileName = entity.FileName,
        ContentType = entity.ContentType,
        SizeBytes = entity.SizeBytes,
        Sha256 = entity.Sha256,
        Content = entity.Content,
        UploadedAtUtc = entity.UploadedAtUtc
    };

    private static ControlCloudClientPortalAttachmentEntity ToEntity(
        ControlCloudClientPortalAttachment attachment) => new()
    {
        AttachmentId = attachment.AttachmentId,
        ClientId = attachment.ClientId,
        UploadedByUserId = attachment.UploadedByUserId,
        FileName = attachment.FileName,
        ContentType = attachment.ContentType,
        SizeBytes = attachment.SizeBytes,
        Sha256 = attachment.Sha256,
        Content = attachment.Content,
        UploadedAtUtc = attachment.UploadedAtUtc
    };
}

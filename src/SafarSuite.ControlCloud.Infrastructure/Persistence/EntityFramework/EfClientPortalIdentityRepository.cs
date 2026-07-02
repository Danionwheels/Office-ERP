using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientPortalIdentityRepository : IClientPortalIdentityRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfClientPortalIdentityRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlCloudClientPortalInvitation?> GetInvitationByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalInvitations
            .SingleOrDefaultAsync(invitation => invitation.InvitationId == invitationId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<ControlCloudClientPortalInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalInvitations
            .SingleOrDefaultAsync(invitation => invitation.TokenHash == tokenHash, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyCollection<ControlCloudClientPortalInvitation>> ListInvitationsByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientPortalInvitations
            .Where(invitation => invitation.ClientId == clientId)
            .OrderByDescending(invitation => invitation.InvitedAtUtc)
            .ThenBy(invitation => invitation.Email)
            .Select(invitation => ToDomain(invitation))
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientPortalInvitations.AddAsync(ToEntity(invitation), cancellationToken);
    }

    public async Task SaveInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalInvitations
            .SingleAsync(stored => stored.InvitationId == invitation.InvitationId, cancellationToken);
        Apply(invitation, entity);
    }

    public async Task<ControlCloudClientPortalUser?> GetUserByClientAndEmailAsync(
        Guid clientId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = ControlCloudClientPortalInvitation.NormalizeEmail(email);
        var entity = await _dbContext.ClientPortalUsers
            .SingleOrDefaultAsync(
                user => user.ClientId == clientId && user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientPortalUsers.AddAsync(ToEntity(user), cancellationToken);
    }

    public async Task SaveUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalUsers
            .SingleAsync(stored => stored.UserId == user.UserId, cancellationToken);
        Apply(user, entity);
    }

    private static ControlCloudClientPortalInvitation ToDomain(
        ControlCloudClientPortalInvitationEntity entity)
    {
        return new ControlCloudClientPortalInvitation
        {
            InvitationId = entity.InvitationId,
            ClientId = entity.ClientId,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = entity.Role,
            TokenHash = entity.TokenHash,
            Status = entity.Status,
            CreatedBy = entity.CreatedBy,
            InvitedAtUtc = entity.InvitedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            AcceptedAtUtc = entity.AcceptedAtUtc,
            AcceptedUserId = entity.AcceptedUserId
        };
    }

    private static ControlCloudClientPortalUser ToDomain(
        ControlCloudClientPortalUserEntity entity)
    {
        return new ControlCloudClientPortalUser
        {
            UserId = entity.UserId,
            ClientId = entity.ClientId,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = entity.Role,
            PasswordHash = entity.PasswordHash,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastLoginAtUtc = entity.LastLoginAtUtc
        };
    }

    private static ControlCloudClientPortalInvitationEntity ToEntity(
        ControlCloudClientPortalInvitation invitation)
    {
        var entity = new ControlCloudClientPortalInvitationEntity();
        Apply(invitation, entity);

        return entity;
    }

    private static ControlCloudClientPortalUserEntity ToEntity(
        ControlCloudClientPortalUser user)
    {
        var entity = new ControlCloudClientPortalUserEntity();
        Apply(user, entity);

        return entity;
    }

    private static void Apply(
        ControlCloudClientPortalInvitation invitation,
        ControlCloudClientPortalInvitationEntity entity)
    {
        entity.InvitationId = invitation.InvitationId;
        entity.ClientId = invitation.ClientId;
        entity.Email = invitation.Email;
        entity.NormalizedEmail = ControlCloudClientPortalInvitation.NormalizeEmail(invitation.Email);
        entity.FullName = invitation.FullName;
        entity.Role = invitation.Role;
        entity.TokenHash = invitation.TokenHash;
        entity.Status = invitation.Status;
        entity.CreatedBy = invitation.CreatedBy;
        entity.InvitedAtUtc = invitation.InvitedAtUtc;
        entity.ExpiresAtUtc = invitation.ExpiresAtUtc;
        entity.AcceptedAtUtc = invitation.AcceptedAtUtc;
        entity.AcceptedUserId = invitation.AcceptedUserId;
    }

    private static void Apply(
        ControlCloudClientPortalUser user,
        ControlCloudClientPortalUserEntity entity)
    {
        entity.UserId = user.UserId;
        entity.ClientId = user.ClientId;
        entity.Email = user.Email;
        entity.NormalizedEmail = ControlCloudClientPortalInvitation.NormalizeEmail(user.Email);
        entity.FullName = user.FullName;
        entity.Role = user.Role;
        entity.PasswordHash = user.PasswordHash;
        entity.Status = user.Status;
        entity.CreatedAtUtc = user.CreatedAtUtc;
        entity.LastLoginAtUtc = user.LastLoginAtUtc;
    }
}

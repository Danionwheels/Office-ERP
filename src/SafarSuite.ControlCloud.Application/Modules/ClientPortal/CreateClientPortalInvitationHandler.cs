using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class CreateClientPortalInvitationHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public CreateClientPortalInvitationHandler(
        IControlCloudClientCommercialProjectionRepository projections,
        IClientPortalIdentityRepository identities,
        IClientPortalCredentialService credentials,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _projections = projections;
        _identities = identities;
        _credentials = credentials;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<CreateClientPortalInvitationResult> HandleAsync(
        CreateClientPortalInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return CreateClientPortalInvitationResult.Failure(
                "ClientIdRequired",
                "Client id is required before inviting a portal user.");
        }

        if (!IsValidEmail(command.Email))
        {
            return CreateClientPortalInvitationResult.Failure(
                "EmailInvalid",
                "A valid email address is required before inviting a portal user.");
        }

        var projection = await _projections.GetByClientIdAsync(command.ClientId, cancellationToken);

        if (projection is null)
        {
            return CreateClientPortalInvitationResult.Failure(
                "ClientNotFound",
                "Client is not available in Control Cloud yet.");
        }

        var existingUser = await _identities.GetUserByClientAndEmailAsync(
            command.ClientId,
            command.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            return CreateClientPortalInvitationResult.Failure(
                "PortalUserAlreadyExists",
                "A portal user already exists for this client and email.");
        }

        var token = _credentials.CreateInvitationToken();
        var now = _clock.UtcNow;
        var expiresAtUtc = now.AddDays(Math.Clamp(command.ExpiresInDays, 1, 30));
        var invitation = ControlCloudClientPortalInvitation.Create(
            Guid.NewGuid(),
            command.ClientId,
            command.Email,
            command.FullName,
            command.Role,
            _credentials.HashSecret(token),
            command.CreatedBy,
            now,
            expiresAtUtc);

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transactionToken =>
            {
                await _identities.AddInvitationAsync(invitation, transactionToken);

                return CreateClientPortalInvitationResult.Success(
                    invitation.InvitationId,
                    invitation.ClientId,
                    invitation.Email,
                    invitation.FullName,
                    invitation.Role,
                    invitation.Status,
                    invitation.InvitedAtUtc,
                    invitation.ExpiresAtUtc,
                    token);
            },
            cancellationToken);
    }

    private static bool IsValidEmail(string value)
    {
        var email = value.Trim();

        return email.Length <= 320
            && email.Contains('@', StringComparison.Ordinal)
            && email.IndexOf('@') > 0
            && email.LastIndexOf('@') < email.Length - 1;
    }
}

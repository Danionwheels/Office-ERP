using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class RequestClientPortalPasswordResetHandler
{
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalPasswordResetRepository _passwordResets;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalMailDeliveryQueue _mail;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public RequestClientPortalPasswordResetHandler(
        IClientPortalIdentityRepository identities,
        IClientPortalPasswordResetRepository passwordResets,
        IClientPortalCredentialService credentials,
        IClientPortalMailDeliveryQueue mail,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _identities = identities;
        _passwordResets = passwordResets;
        _credentials = credentials;
        _mail = mail;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RequestClientPortalPasswordResetResult> HandleAsync(
        RequestClientPortalPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Email)
            || command.Email.Length > 320)
        {
            return RequestClientPortalPasswordResetResult.GenericAccepted();
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var user = await _identities.GetUserByClientAndEmailAsync(
                    command.ClientId,
                    command.Email,
                    token);

                if (user is null
                    || !string.Equals(user.Status, ControlCloudClientPortalUserStatuses.Active, StringComparison.Ordinal))
                {
                    return RequestClientPortalPasswordResetResult.GenericAccepted();
                }

                var now = _clock.UtcNow;
                var rawToken = _credentials.CreateSecureToken(
                    Math.Clamp(command.TokenBytes, 32, 128));
                var reset = ControlCloudClientPortalPasswordReset.Create(
                    Guid.NewGuid(),
                    user.UserId,
                    user.ClientId,
                    _credentials.HashSecret($"client-portal-password-reset:{rawToken}"),
                    now,
                    now.AddMinutes(Math.Clamp(command.ExpiresInMinutes, 5, 120)));
                await _passwordResets.AddAsync(reset, token);

                var resetLink = $"{command.ResetLinkBase.Split('#')[0]}#reset={Uri.EscapeDataString(rawToken)}";
                await _mail.EnqueueAsync(
                    new ClientPortalMailDeliveryRequest(
                        Guid.NewGuid(),
                        user.ClientId,
                        user.Email,
                        user.FullName,
                        "SafarSuite Client Portal password reset",
                        BuildMailBody(user.FullName, resetLink, reset.ExpiresAtUtc),
                        now),
                    token);
                return RequestClientPortalPasswordResetResult.GenericAccepted();
            },
            cancellationToken);
    }

    private static string BuildMailBody(string fullName, string resetLink, DateTimeOffset expiresAtUtc) =>
        string.Join(
            Environment.NewLine,
            [
                $"Hello {(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim())},",
                "",
                "A password reset was requested for your SafarSuite Client Portal account.",
                "Open this link to choose a new password:",
                resetLink,
                "",
                $"This link expires at {expiresAtUtc:O}.",
                "If you did not request this reset, you can ignore this email."
            ]);
}

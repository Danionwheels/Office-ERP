using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class ValidateClientPortalPasswordResetHandler
{
    private readonly IClientPortalPasswordResetRepository _passwordResets;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IControlCloudClock _clock;

    public ValidateClientPortalPasswordResetHandler(
        IClientPortalPasswordResetRepository passwordResets,
        IClientPortalCredentialService credentials,
        IControlCloudClock clock)
    {
        _passwordResets = passwordResets;
        _credentials = credentials;
        _clock = clock;
    }

    public async Task<bool> HandleAsync(string resetToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resetToken) || resetToken.Length > 256) return false;
        var reset = await _passwordResets.GetByTokenHashAsync(
            _credentials.HashSecret($"client-portal-password-reset:{resetToken.Trim()}"),
            cancellationToken);
        return reset?.IsUsableAt(_clock.UtcNow) == true;
    }
}

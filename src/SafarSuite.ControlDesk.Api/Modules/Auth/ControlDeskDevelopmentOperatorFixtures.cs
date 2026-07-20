using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

internal static class ControlDeskDevelopmentOperatorFixtures
{
    public static LocalOperator[] Create(ControlDeskOperatorAccessOptions options)
    {
        return options.Users.Select(Create).ToArray();
    }

    private static LocalOperator Create(ControlDeskOperatorUserOptions user)
    {
        var localOperator = LocalOperator.Create(
            LocalOperatorId.Create(CreateStableId(user.UserId)),
            LocalOperatorEmail.Create(user.Email),
            user.FullName,
            user.PasswordHash,
            user.Roles,
            user.Scopes,
            DateTimeOffset.UnixEpoch);

        if (string.Equals(user.Status, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            localOperator.Disable(DateTimeOffset.UnixEpoch);
        }

        return localOperator;
    }

    private static Guid CreateStableId(string userId)
    {
        if (Guid.TryParse(userId?.Trim(), out var configuredId) && configuredId != Guid.Empty)
        {
            return configuredId;
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(userId?.Trim() ?? string.Empty));
        return new Guid(digest.AsSpan(0, 16));
    }
}

using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public sealed class ControlDeskSessionSigningKeyStartupValidator(
    IControlDeskSessionSigningKeyProvider provider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provider.SessionSigningKeyId))
            {
                throw new InvalidOperationException(
                    "Control Desk session signing material is unavailable.");
            }

            var key = provider.CopySessionSigningKey();

            try
            {
                if (key.Length < 32)
                {
                    throw new InvalidOperationException(
                        "Control Desk session signing material is unavailable.");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
        catch (Exception)
        {
            throw new InvalidOperationException(
                "Control Desk session signing material is unavailable.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

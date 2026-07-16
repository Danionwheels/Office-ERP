using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryProviderBankDetailsRepository : IProviderBankDetailsRepository
{
    private readonly object _sync = new();
    private ProviderBankDetails? _details;

    public Task<ProviderBankDetails?> GetAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_details);
        }
    }

    public Task AddAsync(ProviderBankDetails details, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _details ??= details;
        }

        return Task.CompletedTask;
    }
}

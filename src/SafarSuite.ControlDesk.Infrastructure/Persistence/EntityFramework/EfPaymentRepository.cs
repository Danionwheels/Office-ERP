using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfPaymentRepository : IPaymentRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfPaymentRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .SingleOrDefaultAsync(payment => payment.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Payment>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var payments = _dbContext.Payments
            .Where(payment => payment.ClientId == clientId);

        if (fromDate.HasValue)
        {
            payments = payments.Where(payment => payment.ReceivedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            payments = payments.Where(payment => payment.ReceivedOn <= toDate.Value);
        }

        return await payments
            .OrderBy(payment => payment.ReceivedOn)
            .ThenBy(payment => payment.RecordedAtUtc)
            .ThenBy(payment => payment.Id)
            .ToArrayAsync(cancellationToken);
    }
}

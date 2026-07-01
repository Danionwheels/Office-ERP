using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
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
}

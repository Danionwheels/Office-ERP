using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(InvoiceNumber number, CancellationToken cancellationToken = default);
}

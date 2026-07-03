using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

public interface ICreditNoteRepository
{
    Task AddAsync(CreditNote creditNote, CancellationToken cancellationToken = default);

    Task<CreditNote?> GetByIdAsync(CreditNoteId id, CancellationToken cancellationToken = default);

    Task<CreditNote?> GetByNumberAsync(CreditNoteNumber number, CancellationToken cancellationToken = default);

    Task<CreditNote?> GetForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CreditNote>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(CreditNoteNumber number, CancellationToken cancellationToken = default);

    Task<bool> ExistsForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken = default);
}

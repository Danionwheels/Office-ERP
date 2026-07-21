using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IControlCloudClientCommercialProjectionRepository
{
    Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudClientCommercialProjection projection,
        CancellationToken cancellationToken = default);

    Task ApplyChangeAsync(
        ControlCloudCommercialProjectionChange change,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudCommercialDocumentProjection>> ListDocumentsAsync(
        Guid clientId,
        string documentType,
        DateOnly? beforeDate,
        Guid? beforeDocumentId,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudInvoiceProjection>> ListInvoicesAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<ControlCloudInvoiceProjection?> GetInvoiceAsync(
        Guid clientId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudPaymentProjection>> ListPaymentsAsync(
        Guid clientId,
        Guid? invoiceId = null,
        CancellationToken cancellationToken = default);
}

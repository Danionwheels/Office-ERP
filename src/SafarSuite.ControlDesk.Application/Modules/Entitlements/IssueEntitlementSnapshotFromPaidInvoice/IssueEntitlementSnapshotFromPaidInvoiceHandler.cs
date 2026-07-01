using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;

public sealed class IssueEntitlementSnapshotFromPaidInvoiceHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IInvoiceRepository _invoices;
    private readonly IEntitlementSnapshotRepository _entitlementSnapshots;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly IssueEntitlementSnapshotFromPaidInvoiceValidator _validator;

    public IssueEntitlementSnapshotFromPaidInvoiceHandler(
        IInvoiceRepository invoices,
        IEntitlementSnapshotRepository entitlementSnapshots,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        IssueEntitlementSnapshotFromPaidInvoiceValidator validator)
    {
        _invoices = invoices;
        _entitlementSnapshots = entitlementSnapshots;
        _cloudOutboxMessages = cloudOutboxMessages;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<IssueEntitlementSnapshotFromPaidInvoiceResult>> HandleAsync(
        IssueEntitlementSnapshotFromPaidInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(validationErrors);
        }

        try
        {
            var invoiceId = InvoiceId.Create(command.InvoiceId);
            var invoice = await _invoices.GetByIdAsync(invoiceId, cancellationToken);

            if (invoice is null)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Invoice was not found."));
            }

            if (invoice.Status != InvoiceStatus.Paid)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                    nameof(command.InvoiceId),
                    "Entitlement snapshots can only be issued from paid invoices."));
            }

            var modules = command.Modules
                .Select(module => EntitlementModule.Create(ModuleCode.Create(module.ModuleCode), module.IsEnabled))
                .ToArray();

            var duplicateModuleCode = modules
                .GroupBy(module => module.ModuleCode.Value, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .FirstOrDefault();

            if (duplicateModuleCode is not null)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                    nameof(command.Modules),
                    $"Module code {duplicateModuleCode} is duplicated."));
            }

            var issuedAtUtc = _clock.UtcNow;
            var snapshot = EntitlementSnapshot.Issue(
                EntitlementSnapshotId.Create(_idGenerator.NewGuid()),
                invoice.ClientId,
                invoice.ContractId,
                EntitlementStatus.Active,
                command.PaidUntil,
                command.GraceUntil,
                command.OfflineValidUntil,
                command.AllowedDevices,
                command.AllowedBranches,
                modules,
                issuedAtUtc);

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _entitlementSnapshots.AddAsync(snapshot, token);
                    await _cloudOutboxMessages.AddAsync(
                        CreateEntitlementSnapshotIssuedOutboxMessage(snapshot, invoice),
                        token);

                    return ToResult(snapshot, invoice);
                },
                cancellationToken);

            return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private CloudOutboxMessage CreateEntitlementSnapshotIssuedOutboxMessage(
        EntitlementSnapshot snapshot,
        Invoice invoice)
    {
        var payload = new EntitlementSnapshotIssuedCloudPayload(
            "1",
            snapshot.Id.Value,
            snapshot.ClientId.Value,
            snapshot.ContractId.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            snapshot.Status.ToString(),
            snapshot.PaidUntil,
            snapshot.GraceUntil,
            snapshot.OfflineValidUntil,
            snapshot.AllowedDevices,
            snapshot.AllowedBranches,
            snapshot.IssuedAtUtc,
            snapshot.Modules.Select(module => new EntitlementSnapshotIssuedCloudPayloadModule(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray());

        return CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            "EntitlementSnapshotIssued",
            "EntitlementSnapshot",
            snapshot.Id.Value.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions),
            _clock.UtcNow);
    }

    private static IssueEntitlementSnapshotFromPaidInvoiceResult ToResult(
        EntitlementSnapshot snapshot,
        Invoice invoice)
    {
        return new IssueEntitlementSnapshotFromPaidInvoiceResult(
            snapshot.Id.Value,
            snapshot.ClientId.Value,
            snapshot.ContractId.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            snapshot.Status.ToString(),
            snapshot.PaidUntil,
            snapshot.GraceUntil,
            snapshot.OfflineValidUntil,
            snapshot.AllowedDevices,
            snapshot.AllowedBranches,
            snapshot.IssuedAtUtc,
            snapshot.Modules.Select(module => new IssueEntitlementSnapshotModuleResult(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray());
    }

    private sealed record EntitlementSnapshotIssuedCloudPayload(
        string EventVersion,
        Guid EntitlementSnapshotId,
        Guid ClientId,
        Guid ContractId,
        Guid SourceInvoiceId,
        string SourceInvoiceNumber,
        string Status,
        DateOnly PaidUntil,
        DateOnly GraceUntil,
        DateOnly OfflineValidUntil,
        int AllowedDevices,
        int AllowedBranches,
        DateTimeOffset IssuedAtUtc,
        IReadOnlyCollection<EntitlementSnapshotIssuedCloudPayloadModule> Modules);

    private sealed record EntitlementSnapshotIssuedCloudPayloadModule(
        string ModuleCode,
        bool IsEnabled);
}

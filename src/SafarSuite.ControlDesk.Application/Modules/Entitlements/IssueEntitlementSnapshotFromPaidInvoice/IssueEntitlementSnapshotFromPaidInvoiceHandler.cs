using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
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
    private readonly IContractRepository _contracts;
    private readonly IClientAccessRevisionRepository _clientAccessRevisions;
    private readonly IEntitlementSnapshotRepository _entitlementSnapshots;
    private readonly IEntitlementVersionAllocator _entitlementVersions;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly IssueEntitlementSnapshotFromPaidInvoiceValidator _validator;

    public IssueEntitlementSnapshotFromPaidInvoiceHandler(
        IInvoiceRepository invoices,
        IContractRepository contracts,
        IClientAccessRevisionRepository clientAccessRevisions,
        IEntitlementSnapshotRepository entitlementSnapshots,
        IEntitlementVersionAllocator entitlementVersions,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        IssueEntitlementSnapshotFromPaidInvoiceValidator validator)
    {
        _invoices = invoices;
        _contracts = contracts;
        _clientAccessRevisions = clientAccessRevisions;
        _entitlementSnapshots = entitlementSnapshots;
        _entitlementVersions = entitlementVersions;
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

            var contract = await _contracts.GetByIdAsync(invoice.ContractId, cancellationToken);

            if (contract is null)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(invoice.ContractId),
                    "The contract revision referenced by the paid invoice was not found."));
            }

            if (contract.ClientId != invoice.ClientId)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                    nameof(invoice.ContractId),
                    "The paid invoice and contract revision belong to different clients."));
            }

            var modules = command.Modules
                .Select(module => ClientAccessRevisionModule.Create(
                    ModuleCode.Create(module.ModuleCode),
                    module.IsEnabled))
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

            var featureLimits = (command.FeatureLimits ?? [])
                .Select(limit => ModuleFeatureLimit.Create(
                    limit.ModuleCode,
                    limit.FeatureCode,
                    limit.LimitValue,
                    limit.Unit))
                .ToArray();

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var latestRevision = await _clientAccessRevisions.GetLatestForClientForUpdateAsync(
                        invoice.ClientId,
                        token);
                    var entitlementVersion = await _entitlementVersions.AllocateNextAsync(token);

                    if (latestRevision is not null
                        && entitlementVersion <= latestRevision.RevisionNumber)
                    {
                        throw new InvalidOperationException(
                            "Allocated entitlement version must be newer than the current client access revision.");
                    }

                    var approvedAtUtc = _clock.UtcNow;
                    var effectiveFromUtc = command.EffectiveFromUtc.HasValue
                                           && command.EffectiveFromUtc.Value > approvedAtUtc
                        ? command.EffectiveFromUtc.Value.ToUniversalTime()
                        : approvedAtUtc;
                    var revision = ClientAccessRevision.ApproveFromPaidInvoice(
                        ClientAccessRevisionId.Create(_idGenerator.NewGuid()),
                        invoice.ClientId,
                        invoice.ContractId,
                        contract.RevisionNumber,
                        contract.ProductCatalogRevisionId,
                        contract.ProductCatalogRevisionNumber,
                        invoice.Id,
                        invoice.Number.Value,
                        entitlementVersion,
                        latestRevision?.Id,
                        command.PaidUntil,
                        command.GraceUntil,
                        command.OfflineValidUntil,
                        command.AllowedDevices,
                        command.AllowedBranches,
                        modules,
                        command.ApprovedBy,
                        command.ApprovalReason,
                        approvedAtUtc,
                        command.AllowedNamedUsers,
                        command.AllowedConcurrentUsers,
                        featureLimits,
                        effectiveFromUtc);
                    var snapshot = EntitlementSnapshot.IssueFromApprovedRevision(
                        EntitlementSnapshotId.Create(_idGenerator.NewGuid()),
                        revision,
                        EntitlementStatus.Active,
                        approvedAtUtc);

                    await _clientAccessRevisions.AddAsync(revision, token);
                    await _entitlementSnapshots.AddAsync(snapshot, token);
                    await _cloudOutboxMessages.AddAsync(
                        CreateEntitlementSnapshotIssuedOutboxMessage(snapshot, revision, invoice),
                        token);

                    return ToResult(snapshot, revision, invoice);
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
        ClientAccessRevision revision,
        Invoice invoice)
    {
        var payload = new EntitlementSnapshotIssuedCloudPayload(
            "6",
            snapshot.Id.Value,
            snapshot.ClientId.Value,
            snapshot.ContractId.Value,
            revision.ContractRevisionNumber,
            revision.ProductCatalogRevisionId.Value,
            revision.ProductCatalogRevisionNumber,
            snapshot.ClientAccessRevisionId.Value,
            snapshot.EntitlementVersion,
            invoice.Id.Value,
            invoice.Number.Value,
            snapshot.Status.ToString(),
            snapshot.PaidUntil,
            snapshot.GraceUntil,
            snapshot.OfflineValidUntil,
            snapshot.AllowedDevices,
            snapshot.AllowedBranches,
            snapshot.IssuedAtUtc,
            snapshot.EffectiveFromUtc,
            snapshot.Modules.Select(module => new EntitlementSnapshotIssuedCloudPayloadModule(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray(),
            snapshot.AllowedNamedUsers,
            snapshot.AllowedConcurrentUsers,
            snapshot.FeatureLimits.Select(limit => new EntitlementSnapshotIssuedCloudPayloadFeatureLimit(
                limit.ModuleCode.Value,
                limit.FeatureCode.Value,
                limit.LimitValue,
                limit.Unit)).ToArray());

        return CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            snapshot.ClientId,
            "EntitlementSnapshotIssued",
            "EntitlementSnapshot",
            snapshot.Id.Value.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions),
            _clock.UtcNow);
    }

    private static IssueEntitlementSnapshotFromPaidInvoiceResult ToResult(
        EntitlementSnapshot snapshot,
        ClientAccessRevision revision,
        Invoice invoice)
    {
        return new IssueEntitlementSnapshotFromPaidInvoiceResult(
            snapshot.Id.Value,
            snapshot.ClientId.Value,
            snapshot.ContractId.Value,
            revision.ContractRevisionNumber,
            revision.ProductCatalogRevisionId.Value,
            revision.ProductCatalogRevisionNumber,
            revision.Id.Value,
            snapshot.EntitlementVersion,
            invoice.Id.Value,
            invoice.Number.Value,
            snapshot.Status.ToString(),
            snapshot.PaidUntil,
            snapshot.GraceUntil,
            snapshot.OfflineValidUntil,
            snapshot.AllowedDevices,
            snapshot.AllowedBranches,
            snapshot.IssuedAtUtc,
            snapshot.EffectiveFromUtc,
            revision.SupersedesRevisionId?.Value,
            revision.ApprovedBy,
            revision.ApprovalReason,
            revision.ApprovedAtUtc,
            snapshot.Modules.Select(module => new IssueEntitlementSnapshotModuleResult(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray(),
            snapshot.AllowedNamedUsers,
            snapshot.AllowedConcurrentUsers,
            snapshot.FeatureLimits.Select(limit => new IssueEntitlementSnapshotFeatureLimitResult(
                limit.ModuleCode.Value,
                limit.FeatureCode.Value,
                limit.LimitValue,
                limit.Unit)).ToArray());
    }

    private sealed record EntitlementSnapshotIssuedCloudPayload(
        string EventVersion,
        Guid EntitlementSnapshotId,
        Guid ClientId,
        Guid ContractId,
        long ContractRevisionNumber,
        Guid ProductCatalogRevisionId,
        long ProductCatalogRevisionNumber,
        Guid ClientAccessRevisionId,
        long EntitlementVersion,
        Guid SourceInvoiceId,
        string SourceInvoiceNumber,
        string Status,
        DateOnly PaidUntil,
        DateOnly GraceUntil,
        DateOnly OfflineValidUntil,
        int AllowedDevices,
        int AllowedBranches,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset EffectiveFromUtc,
        IReadOnlyCollection<EntitlementSnapshotIssuedCloudPayloadModule> Modules,
        int? AllowedNamedUsers,
        int? AllowedConcurrentUsers,
        IReadOnlyCollection<EntitlementSnapshotIssuedCloudPayloadFeatureLimit> FeatureLimits);

    private sealed record EntitlementSnapshotIssuedCloudPayloadModule(
        string ModuleCode,
        bool IsEnabled);

    private sealed record EntitlementSnapshotIssuedCloudPayloadFeatureLimit(
        string ModuleCode,
        string FeatureCode,
        long LimitValue,
        string Unit);
}

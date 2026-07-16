using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.CommandCenter.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientWorkQueueReader : IClientWorkQueueReader
{
    private readonly InMemoryClientRepository _clients;
    private readonly IClientDeploymentRepository _deployments;
    private readonly IInvoiceRepository _invoices;
    private readonly IEntitlementSnapshotRepository _entitlements;
    private readonly ICloudOutboxMessageRepository _outbox;

    public InMemoryClientWorkQueueReader(
        InMemoryClientRepository clients,
        IClientDeploymentRepository deployments,
        IInvoiceRepository invoices,
        IEntitlementSnapshotRepository entitlements,
        ICloudOutboxMessageRepository outbox)
    {
        _clients = clients;
        _deployments = deployments;
        _invoices = invoices;
        _entitlements = entitlements;
        _outbox = outbox;
    }

    public async Task<ClientWorkQueueReadPage> ReadPageAsync(
        ClientWorkQueueReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = new List<ClientWorkQueueReadItem>();

        foreach (var client in _clients.Snapshot())
        {
            var clientId = client.Id;
            var deployments = await _deployments.ListByClientIdAsync(clientId, cancellationToken);
            var invoices = await _invoices.ListForClientAsync(
                clientId,
                cancellationToken: cancellationToken);
            var entitlement = await _entitlements.GetLatestForClientAsync(clientId, cancellationToken);
            var outbox = await _outbox.SummarizeAsync(
                status: null,
                messageType: null,
                clientId,
                DateTimeOffset.UtcNow,
                int.MaxValue,
                cancellationToken);
            var item = CreateItem(client, deployments.Count, invoices, entitlement is not null, outbox);
            var sortValue = request.Sort switch
            {
                ClientWorkQueueSort.Client => item.Name.ToLowerInvariant(),
                ClientWorkQueueSort.Action => item.ActionLabel.ToLowerInvariant(),
                _ => item.Priority.ToString("D2")
            };
            item = item with { SortValue = sortValue };

            if (MatchesSearch(item, request.Search))
            {
                items.Add(item);
            }
        }

        var summary = new ClientWorkQueueReadSummary(
            items.LongCount(),
            CountLane(items, "setup"),
            CountLane(items, "billing"),
            CountLane(items, "payments"),
            CountLane(items, "access"),
            CountLane(items, "cloud"),
            CountLane(items, "overview"));
        var lane = request.Lane.ToString().ToLowerInvariant();
        var filteredItems = request.Lane == ClientWorkQueueLane.All
            ? items
            : items.Where(item => string.Equals(item.Tab, lane, StringComparison.Ordinal)).ToList();
        var filteredCount = filteredItems.LongCount();
        var orderedItems = filteredItems
            .OrderBy(item => item.SortValue, StringComparer.Ordinal)
            .ThenBy(item => item.Priority)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.ClientId)
            .AsEnumerable();

        if (request.AfterClientId.HasValue)
        {
            orderedItems = orderedItems.Where(item => IsAfterCursor(item, request));
        }

        return new ClientWorkQueueReadPage(
            orderedItems.Take(request.Take).ToArray(),
            filteredCount,
            summary);
    }

    private static ClientWorkQueueReadItem CreateItem(
        Client client,
        int deploymentCount,
        IReadOnlyCollection<Invoice> invoices,
        bool hasEntitlement,
        CloudOutboxMessageRegisterSummary outbox)
    {
        var openInvoice = invoices.FirstOrDefault(invoice =>
            invoice.BalanceDue.Amount > 0
            && invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid);
        var paidInvoice = hasEntitlement
            ? null
            : invoices.FirstOrDefault(invoice =>
                invoice.BalanceDue.Amount <= 0
                && invoice.TotalAmount.Amount > 0
                && invoice.Status == InvoiceStatus.Paid);
        var pendingMessageCount = outbox.PendingCount + outbox.FailedCount;

        if (client.Status != ClientStatus.Active)
        {
            return Item(client, "Activate client", $"{client.Status} master record", "setup", "warning", 0);
        }

        if (client.Contacts.Count == 0)
        {
            return Item(client, "Add contact", "No billing or support contact", "setup", "warning", 1);
        }

        if (deploymentCount == 0)
        {
            return Item(client, "Save deployment", "No local server profile", "setup", "warning", 2);
        }

        if (invoices.Count == 0)
        {
            return Item(client, "Draft invoice", "No invoice voucher yet", "billing", "warning", 3);
        }

        if (openInvoice is not null)
        {
            return Item(client, "Record receipt", $"{openInvoice.Number.Value} due", "payments", "warning", 4);
        }

        if (paidInvoice is not null)
        {
            return Item(client, "Issue access", $"{paidInvoice.Number.Value} is paid", "access", "warning", 5);
        }

        if (pendingMessageCount > 0)
        {
            var detail = outbox.FailedCount > 0
                ? $"{outbox.FailedCount} failed cloud update{(outbox.FailedCount == 1 ? string.Empty : "s")}"
                : $"{pendingMessageCount} pending cloud update{(pendingMessageCount == 1 ? string.Empty : "s")}";

            return Item(
                client,
                "Send to Cloud",
                detail,
                "cloud",
                outbox.FailedCount > 0 ? "warning" : "ready",
                6);
        }

        return Item(
            client,
            "Review next action",
            $"{client.Contacts.Count} contact{(client.Contacts.Count == 1 ? string.Empty : "s")}",
            "overview",
            "ready",
            7);
    }

    private static ClientWorkQueueReadItem Item(
        Client client,
        string actionLabel,
        string detail,
        string tab,
        string tone,
        int priority)
    {
        return new ClientWorkQueueReadItem(
            client.Id.Value,
            client.Code.Value,
            client.DisplayName,
            client.Status.ToString(),
            actionLabel,
            detail,
            tab,
            tone,
            priority,
            string.Empty);
    }

    private static bool MatchesSearch(ClientWorkQueueReadItem item, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return $"{item.Code} {item.Name} {item.Status} {item.ActionLabel} {item.Detail} {item.Tab}"
            .Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static long CountLane(IEnumerable<ClientWorkQueueReadItem> items, string lane)
    {
        return items.LongCount(item => string.Equals(item.Tab, lane, StringComparison.Ordinal));
    }

    private static bool IsAfterCursor(
        ClientWorkQueueReadItem item,
        ClientWorkQueueReadRequest request)
    {
        var comparison = string.Compare(
            item.SortValue,
            request.AfterSortValue,
            StringComparison.Ordinal);

        if (comparison == 0)
        {
            comparison = item.Priority.CompareTo(request.AfterPriority!.Value);
        }

        if (comparison == 0)
        {
            comparison = string.Compare(item.Code, request.AfterCode, StringComparison.Ordinal);
        }

        if (comparison == 0)
        {
            comparison = item.ClientId.CompareTo(request.AfterClientId!.Value);
        }

        return comparison > 0;
    }
}

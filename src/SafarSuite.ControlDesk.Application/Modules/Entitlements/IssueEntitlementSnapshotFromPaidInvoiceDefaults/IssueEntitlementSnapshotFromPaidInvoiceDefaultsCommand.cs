namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoiceDefaults;

public sealed record IssueEntitlementSnapshotFromPaidInvoiceDefaultsCommand(
    Guid InvoiceId,
    string ApprovedBy,
    string ApprovalReason,
    DateTimeOffset? EffectiveFromUtc = null);

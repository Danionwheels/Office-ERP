using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Clients;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.ControlCloud;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Entitlements;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

namespace SafarSuite.ConnectedAcceptance;

internal sealed record ConnectedAcceptanceEvidence(
    string FormatVersion,
    string RunId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    AcceptanceEndpointEvidence Endpoints,
    IReadOnlyList<string> PassedAssertions,
    AcceptanceAccountingEvidence Accounting,
    AcceptanceClientEvidence Client,
    CreateClientContractResponse Contract,
    AcceptanceBillingEvidence Billing,
    AcceptancePaymentEvidence Payment,
    AcceptanceEntitlementEvidence Entitlement,
    AcceptanceCloudPublishEvidence CloudPublishing,
    AcceptanceRuntimeEvidence Runtime);

internal sealed record AcceptanceEndpointEvidence(
    string ControlDeskBaseUrl,
    string ControlCloudBaseUrl,
    string LocalServerBaseUrl);

internal sealed record AcceptanceAccountingEvidence(
    BootstrapStandardChartOfAccountsResponse Bootstrap,
    Guid AccountsReceivableAccountId,
    Guid CashOrBankAccountId,
    Guid RevenueAccountId);

internal sealed record AcceptanceClientEvidence(
    CreateClientResponse Created,
    ClientDetailsResponse Activated,
    ClientAccountingProfileResponse AccountingProfile,
    ClientDeploymentResponse Deployment);

internal sealed record AcceptanceBillingEvidence(
    CreateChargeCodeResponse ChargeCode,
    CreateClientChargeRuleResponse ChargeRule,
    GenerateInvoiceDraftResponse Draft,
    IssueInvoiceResponse Issued,
    InvoiceDocumentResponse ReadBack);

internal sealed record AcceptancePaymentEvidence(
    RecordInvoicePaymentResponse Recorded,
    ApproveInvoicePaymentResponse Approved,
    InvoicePaymentDocumentResponse ReadBack);

internal sealed record AcceptanceEntitlementEvidence(
    IssueEntitlementSnapshotFromPaidInvoiceResponse Issued,
    EntitlementSnapshotResponse ReadBack);

internal sealed record AcceptanceCloudPublishEvidence(
    ListCloudOutboxMessagesResponse BeforePublish,
    PublishCloudOutboxMessagesResponse PublishResult,
    ListCloudOutboxMessagesResponse AfterPublish,
    AcceptanceDuplicateReplayEvidence DuplicateReplay);

internal sealed record AcceptanceDuplicateReplayEvidence(
    Guid SourceOutboxMessageId,
    string SourceMessageType,
    Guid ReceiptId,
    string CloudReference,
    string Status,
    string SignatureKeyId,
    string PayloadSha256,
    DateTimeOffset ReplayedAtUtc);

internal sealed record AcceptanceBootstrapPackageEvidence(
    string FormatVersion,
    Guid BootstrapPackageId,
    Guid SetupTokenId,
    Guid ClientId,
    string InstallationId,
    string DeploymentMode,
    string LocalServerVersion,
    DateTimeOffset SetupTokenExpiresAtUtc,
    DateTimeOffset GeneratedAtUtc,
    string BundleFileName,
    string BundleContentType,
    string BundleSha256,
    string SignatureAlgorithm,
    string SignatureKeyId,
    string SignaturePayloadSha256,
    LocalServerDeploymentProfileResponse DeploymentProfile);

internal sealed record AcceptanceLocalBootstrapImportResponse(
    Guid ClientId,
    string InstallationId,
    string BootstrapRegistrationStatus,
    LocalServerDeploymentProfileResponse DeploymentProfile,
    string CloudRegistrationStatus,
    DateTimeOffset RegisteredAtUtc,
    string SignatureKeyId,
    string PayloadSha256);

internal sealed record AcceptanceLocalEntitlementPullResponse(
    DateTimeOffset PulledAtUtc,
    Guid ClientId,
    string InstallationId,
    long EntitlementVersion,
    DateOnly PaidUntil,
    DateOnly OfflineValidUntil);

internal sealed record AcceptanceLocalHeartbeatResponse(
    Guid ClientId,
    string InstallationId,
    string HeartbeatStatus,
    string LicenseStatus,
    long? EntitlementVersion,
    DateTimeOffset ReceivedAtUtc,
    ControlCloudEntitlementStateValuesResponse? EntitlementState);

internal sealed record AcceptanceLocalImportAuditRecord(
    Guid AuditRecordId,
    string InstallationId,
    Guid? ClientId,
    string ImportSource,
    string ResultStatus,
    long? EntitlementVersion,
    Guid? BundleIssueId,
    string? FailureCode,
    string? Detail,
    string? PayloadSha256,
    string? SignatureKeyId,
    DateTimeOffset OccurredAtUtc);

internal sealed record AcceptanceRuntimeEvidence(
    AcceptanceBootstrapPackageEvidence BootstrapPackage,
    AcceptanceLocalBootstrapImportResponse BootstrapImport,
    AcceptanceLocalEntitlementPullResponse EntitlementPull,
    LocalServerModuleAccessResponse EnabledModuleAccess,
    LocalServerModuleAccessResponse DisabledModuleAccess,
    LocalServerEntitlementLimitsResponse Limits,
    AcceptanceLocalHeartbeatResponse HeartbeatSubmission,
    ControlCloudInstallationStatusResponse FinalInstallationStatus,
    ControlCloudAuditEventsResponse InstallationAudit,
    IReadOnlyCollection<AcceptanceLocalImportAuditRecord> LocalImportAudit);

internal sealed record ConnectedAcceptanceRunResult(
    string RunId,
    Guid ClientId,
    long EntitlementVersion,
    string ReconciliationState,
    string EvidencePath,
    string EvidenceSha256);

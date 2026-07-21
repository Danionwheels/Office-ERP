using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Auth;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Clients;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.ControlCloud;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Entitlements;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

namespace SafarSuite.ConnectedAcceptance;

internal sealed class ConnectedAcceptanceRunner
{
    private const string EvidenceFormatVersion = "safarsuite-connected-acceptance-v1";
    private const string CurrencyCode = "PKR";
    private const string EnabledModuleCode = "PAYROLL";
    private const string DisabledModuleCode = "TOUR";
    private const string FeatureCode = "MONTHLY_PAYSLIPS";
    private const long FeatureLimit = 500;
    private const decimal InvoiceAmount = 12_500m;
    private const int AllowedDevices = 12;
    private const int AllowedBranches = 4;
    private const int AllowedNamedUsers = 80;
    private const int AllowedConcurrentUsers = 25;

    private readonly AcceptanceAssertions _assertions = new();
    private readonly ConnectedAcceptanceOptions _options;

    public ConnectedAcceptanceRunner(ConnectedAcceptanceOptions options)
    {
        _options = options;
    }

    public async Task<ConnectedAcceptanceRunResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var runId = $"ca-{startedAtUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..35];
        var suffix = runId[^8..].ToUpperInvariant();
        var today = DateOnly.FromDateTime(startedAtUtc.UtcDateTime);
        var contractEndsOn = today.AddYears(1).AddDays(-1);
        var installationId = $"acceptance-{runId}";
        var topologyId = $"acceptance-topology-{suffix.ToLowerInvariant()}";

        using var desk = new AcceptanceHttpClient(_options.ControlDeskBaseUrl);
        using var cloud = new AcceptanceHttpClient(_options.ControlCloudBaseUrl);
        using var local = new AcceptanceHttpClient(_options.LocalServerBaseUrl);

        await VerifyHealthAsync(desk, cloud, local, cancellationToken);

        var session = await desk.PostAsync<LocalOperatorSessionResponse>(
            "api/v1/auth/operator-sessions",
            new CreateLocalOperatorSessionRequest(
                _options.OperatorEmail,
                _options.OperatorPassword,
                ExpiresInMinutes: 60),
            cancellationToken);
        _assertions.NotBlank(session.AccessToken, "Control Desk issued an operator bearer token");
        _assertions.True(
            session.Scopes.Contains("control-desk:admin", StringComparer.Ordinal),
            "operator token carries the control-desk:admin scope");
        desk.UseBearerToken(session.AccessToken);

        var accounting = await CreateAccountingFoundationAsync(desk, cancellationToken);
        var client = await CreateClientAsync(
            desk,
            suffix,
            installationId,
            topologyId,
            accounting.AccountsReceivableAccountId,
            cancellationToken);
        var contract = await CreateContractAsync(
            desk,
            client.Created.ClientId,
            suffix,
            today,
            contractEndsOn,
            cancellationToken);
        var billing = await BillClientAsync(
            desk,
            client.Created.ClientId,
            contract.ContractId,
            suffix,
            today,
            contractEndsOn,
            accounting,
            cancellationToken);
        var payment = await PayInvoiceAsync(
            desk,
            billing.Draft.InvoiceId,
            suffix,
            today,
            accounting,
            cancellationToken);
        var entitlement = await IssueEntitlementAsync(
            desk,
            client.Created.ClientId,
            contract,
            billing.Draft.InvoiceId,
            cancellationToken);
        var cloudPublishing = await PublishAndReplayAsync(
            desk,
            cloud,
            client.Created.ClientId,
            cancellationToken);
        var runtime = await BootstrapAndReconcileAsync(
            desk,
            local,
            client.Created.ClientId,
            installationId,
            topologyId,
            entitlement.Issued,
            today,
            cancellationToken);

        var completedAtUtc = DateTimeOffset.UtcNow;
        var evidence = new ConnectedAcceptanceEvidence(
            EvidenceFormatVersion,
            runId,
            startedAtUtc,
            completedAtUtc,
            new AcceptanceEndpointEvidence(
                _options.ControlDeskBaseUrl.ToString(),
                _options.ControlCloudBaseUrl.ToString(),
                _options.LocalServerBaseUrl.ToString()),
            _assertions.Passed,
            accounting,
            client,
            contract,
            billing,
            payment,
            entitlement,
            cloudPublishing,
            runtime);
        var evidenceSha256 = await WriteEvidenceAsync(evidence, cancellationToken);

        return new ConnectedAcceptanceRunResult(
            runId,
            client.Created.ClientId,
            entitlement.Issued.EntitlementVersion,
            runtime.FinalInstallationStatus.Reconciliation!.State,
            Path.GetFullPath(_options.EvidencePath),
            evidenceSha256);
    }

    private async Task VerifyHealthAsync(
        AcceptanceHttpClient desk,
        AcceptanceHttpClient cloud,
        AcceptanceHttpClient local,
        CancellationToken cancellationToken)
    {
        var deskHealth = await desk.GetElementAsync("health", cancellationToken);
        var cloudHealth = await cloud.GetElementAsync("health", cancellationToken);
        var localHealth = await local.GetElementAsync("health", cancellationToken);

        _assertions.Equal("Healthy", ReadString(deskHealth, "status"), "Control Desk health is Healthy");
        _assertions.Equal("Healthy", ReadString(cloudHealth, "status"), "Control Cloud health is Healthy");
        _assertions.Equal("Healthy", ReadString(localHealth, "status"), "Local Server health is Healthy");
    }

    private async Task<AcceptanceAccountingEvidence> CreateAccountingFoundationAsync(
        AcceptanceHttpClient desk,
        CancellationToken cancellationToken)
    {
        var bootstrap = await desk.PostAsync<BootstrapStandardChartOfAccountsResponse>(
            "api/v1/accounting/accounting-setup/standard-chart-of-accounts",
            new BootstrapStandardChartOfAccountsRequest("MAIN"),
            cancellationToken);
        var receivable = FindAccount(bootstrap, "ClientReceivable");
        var cash = FindAccount(bootstrap, "CashBank");
        var revenue = FindAccount(bootstrap, "SubscriptionRevenue");

        _assertions.True(bootstrap.Accounts.Count >= 20, "standard chart of accounts was created");
        _assertions.True(receivable.IsPostingAccount, "client receivable account is postable");
        _assertions.True(cash.IsPostingAccount, "cash or bank account is postable");
        _assertions.True(revenue.IsPostingAccount, "subscription revenue account is postable");

        return new AcceptanceAccountingEvidence(
            bootstrap,
            receivable.LedgerAccountId,
            cash.LedgerAccountId,
            revenue.LedgerAccountId);
    }

    private async Task<AcceptanceClientEvidence> CreateClientAsync(
        AcceptanceHttpClient desk,
        string suffix,
        string installationId,
        string topologyId,
        Guid receivableAccountId,
        CancellationToken cancellationToken)
    {
        var created = await desk.PostAsync<CreateClientResponse>(
            "api/v1/clients/",
            new CreateClientRequest(
                $"ACC{suffix}",
                $"Connected Acceptance Client {suffix}",
                $"Acceptance {suffix}"),
            cancellationToken);
        var activated = await desk.PostAsync<ClientDetailsResponse>(
            $"api/v1/clients/{created.ClientId:D}/activate",
            cancellationToken: cancellationToken);
        var accountingProfile = await desk.PutAsync<ClientAccountingProfileResponse>(
            $"api/v1/clients/{created.ClientId:D}/accounting-profile",
            new ConfigureClientAccountingProfileRequest(
                receivableAccountId,
                CurrencyCode),
            cancellationToken);
        var deployment = await desk.PutAsync<ClientDeploymentResponse>(
            $"api/v1/clients/{created.ClientId:D}/deployments/{Uri.EscapeDataString(installationId)}",
            new ConfigureClientDeploymentRequest(
                "Connected acceptance HQ",
                ControlCloudBootstrapModes.OnlineBootstrap,
                SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
                "acceptance-hq",
                SafarSuiteDeploymentSiteRoles.Hq,
                ParentSiteId: null,
                BranchCode: "HQ",
                SyncTopologyId: topologyId,
                LocalServerVersion: "acceptance-1.0.0",
                SafarSuiteAppVersion: "acceptance-app-1.0.0",
                IsPrimary: true),
            cancellationToken);

        _assertions.NotEmpty(created.ClientId, "client ID was allocated");
        _assertions.Equal("Active", activated.Status, "client is Active");
        _assertions.True(activated.ActivatedAtUtc.HasValue, "client activation timestamp was retained");
        _assertions.Equal(created.ClientId, accountingProfile.ClientId, "accounting profile belongs to the client");
        _assertions.Equal(installationId, deployment.InstallationId, "deployment uses the intended installation ID");
        _assertions.Equal(
            SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
            deployment.ClientDeploymentMode,
            "deployment is CloudSyncMultiBranch");

        return new AcceptanceClientEvidence(created, activated, accountingProfile, deployment);
    }

    private async Task<CreateClientContractResponse> CreateContractAsync(
        AcceptanceHttpClient desk,
        Guid clientId,
        string suffix,
        DateOnly startsOn,
        DateOnly endsOn,
        CancellationToken cancellationToken)
    {
        var response = await desk.PostAsync<CreateClientContractResponse>(
            "api/v1/contracts/client-contracts",
            new CreateClientContractRequest(
                clientId,
                $"ACC-CON-{suffix}",
                startsOn,
                endsOn,
                InvoiceAmount,
                CurrencyCode,
                "Monthly",
                Math.Min(startsOn.Day, 28),
                AllowedDevices,
                AllowedBranches,
                "P0 connected acceptance proof",
                [
                    new ClientContractModuleRequest(EnabledModuleCode, true),
                    new ClientContractModuleRequest(DisabledModuleCode, false)
                ],
                AllowedNamedUsers,
                AllowedConcurrentUsers,
                [
                    new ClientContractFeatureLimitRequest(
                        EnabledModuleCode,
                        FeatureCode,
                        FeatureLimit,
                        "Count")
                ]),
            cancellationToken);

        _assertions.NotEmpty(response.ContractId, "contract ID was allocated");
        _assertions.Equal("Active", response.Status, "contract revision is Active");
        _assertions.Equal(1L, response.RevisionNumber, "contract starts at immutable revision 1");
        _assertions.NotEmpty(response.ProductCatalogRevisionId, "product catalog revision ID was retained");
        _assertions.True(response.ProductCatalogRevisionNumber > 0, "product catalog revision number was retained");
        _assertions.True(
            response.Modules.Any(module => module.ModuleCode == EnabledModuleCode && module.IsEnabled),
            "contract enables PAYROLL");
        _assertions.True(
            response.Modules.Any(module => module.ModuleCode == DisabledModuleCode && !module.IsEnabled),
            "contract disables TOUR");
        _assertions.True(
            (response.FeatureLimits ?? []).Any(limit =>
                limit.ModuleCode == EnabledModuleCode
                && limit.FeatureCode == FeatureCode
                && limit.LimitValue == FeatureLimit),
            "contract retains the PAYROLL feature limit");

        return response;
    }

    private async Task<AcceptanceBillingEvidence> BillClientAsync(
        AcceptanceHttpClient desk,
        Guid clientId,
        Guid contractId,
        string suffix,
        DateOnly issueDate,
        DateOnly contractEndsOn,
        AcceptanceAccountingEvidence accounting,
        CancellationToken cancellationToken)
    {
        var chargeCode = await desk.PostAsync<CreateChargeCodeResponse>(
            "api/v1/billing/charge-codes",
            new CreateChargeCodeRequest(
                $"ACC-{suffix}",
                "Connected acceptance subscription",
                "One-cycle connected acceptance charge",
                InvoiceAmount,
                CurrencyCode,
                accounting.RevenueAccountId),
            cancellationToken);
        var chargeRule = await desk.PostAsync<CreateClientChargeRuleResponse>(
            "api/v1/billing/client-charge-rules",
            new CreateClientChargeRuleRequest(
                clientId,
                contractId,
                chargeCode.ChargeCodeId,
                EnabledModuleCode,
                "Connected acceptance subscription",
                InvoiceAmount,
                CurrencyCode,
                1m,
                0m,
                "Monthly",
                Math.Min(issueDate.Day, 28),
                issueDate,
                contractEndsOn),
            cancellationToken);
        var draft = await desk.PostAsync<GenerateInvoiceDraftResponse>(
            "api/v1/billing/invoice-drafts",
            new GenerateInvoiceDraftRequest(
                clientId,
                contractId,
                $"ACC-INV-{suffix}",
                issueDate,
                issueDate.AddDays(7),
                issueDate,
                CurrencyCode),
            cancellationToken);
        var issued = await desk.PostAsync<IssueInvoiceResponse>(
            $"api/v1/billing/invoices/{draft.InvoiceId:D}/issue",
            new IssueInvoiceRequest(
                accounting.AccountsReceivableAccountId,
                issueDate),
            cancellationToken);
        var readBack = await desk.GetAsync<InvoiceDocumentResponse>(
            $"api/v1/billing/invoices/{draft.InvoiceId:D}",
            cancellationToken);

        _assertions.NotEmpty(chargeCode.ChargeCodeId, "charge code ID was allocated");
        _assertions.NotEmpty(chargeRule.ClientChargeRuleId, "client charge rule ID was allocated");
        _assertions.Equal(InvoiceAmount, draft.TotalAmount, "invoice draft total matches contract price");
        _assertions.Equal("Issued", issued.InvoiceStatus, "invoice is Issued");
        _assertions.NotEmpty(issued.JournalEntryId, "invoice journal entry ID was allocated");
        _assertions.Equal("Posted", issued.JournalEntryStatus, "invoice journal is Posted");
        _assertions.Equal(CurrencyCode, issued.CurrencyCode, "invoice journal currency is PKR");
        _assertions.Balanced(issued.TotalDebit, issued.TotalCredit, "invoice journal is balanced");
        _assertions.Equal(InvoiceAmount, issued.TotalDebit, "invoice journal debit equals the invoice total");
        _assertions.True(
            issued.JournalLines.Any(line =>
                line.LedgerAccountId == accounting.AccountsReceivableAccountId
                && line.Debit == InvoiceAmount
                && line.Credit == 0m),
            "invoice journal debits the configured client receivable account");
        _assertions.True(
            issued.JournalLines.Any(line =>
                line.LedgerAccountId == accounting.RevenueAccountId
                && line.Credit == InvoiceAmount
                && line.Debit == 0m),
            "invoice journal credits the configured subscription revenue account");
        _assertions.Equal(issued.JournalEntryId, readBack.IssuedInvoice?.JournalEntryId, "issued invoice reads back with the same journal ID");

        return new AcceptanceBillingEvidence(chargeCode, chargeRule, draft, issued, readBack);
    }

    private async Task<AcceptancePaymentEvidence> PayInvoiceAsync(
        AcceptanceHttpClient desk,
        Guid invoiceId,
        string suffix,
        DateOnly paymentDate,
        AcceptanceAccountingEvidence accounting,
        CancellationToken cancellationToken)
    {
        var recorded = await desk.PostAsync<RecordInvoicePaymentResponse>(
            "api/v1/payments/invoice-payments",
            new RecordInvoicePaymentRequest(
                invoiceId,
                "BankTransfer",
                $"ACC-PAY-{suffix}",
                InvoiceAmount,
                CurrencyCode,
                paymentDate,
                accounting.CashOrBankAccountId,
                accounting.AccountsReceivableAccountId,
                paymentDate),
            cancellationToken);

        _assertions.Equal("PendingReview", recorded.PaymentStatus, "bank transfer enters PendingReview");
        _assertions.True(!recorded.JournalEntryId.HasValue, "pending bank transfer has no journal posting");

        var approved = await desk.PostAsync<ApproveInvoicePaymentResponse>(
            $"api/v1/payments/invoice-payments/{recorded.PaymentId:D}/approve",
            new ApproveInvoicePaymentRequest(
                accounting.CashOrBankAccountId,
                accounting.AccountsReceivableAccountId,
                paymentDate,
                "Bank transfer verified for P0 connected acceptance"),
            cancellationToken);
        var readBack = await desk.GetAsync<InvoicePaymentDocumentResponse>(
            $"api/v1/payments/invoice-payments/{recorded.PaymentId:D}",
            cancellationToken);

        _assertions.Equal("Approved", approved.PaymentStatus, "payment is Approved");
        _assertions.Equal("Paid", approved.InvoiceStatus, "invoice is Paid");
        _assertions.Equal(0m, approved.BalanceDue, "paid invoice balance is zero");
        _assertions.NotEmpty(approved.JournalEntryId, "payment journal entry ID was allocated");
        _assertions.Equal("Posted", approved.JournalEntryStatus, "payment journal is Posted");
        _assertions.Equal(CurrencyCode, approved.CurrencyCode, "payment journal currency is PKR");
        _assertions.Balanced(approved.TotalDebit, approved.TotalCredit, "payment journal is balanced");
        _assertions.Equal(InvoiceAmount, approved.TotalDebit, "payment journal debit equals the payment amount");
        _assertions.True(
            approved.JournalLines.Any(line =>
                line.LedgerAccountId == accounting.CashOrBankAccountId
                && line.Debit == InvoiceAmount
                && line.Credit == 0m),
            "payment journal debits the configured cash or bank account");
        _assertions.True(
            approved.JournalLines.Any(line =>
                line.LedgerAccountId == accounting.AccountsReceivableAccountId
                && line.Credit == InvoiceAmount
                && line.Debit == 0m),
            "payment journal credits the configured client receivable account");
        _assertions.Equal(approved.JournalEntryId, readBack.Payment.JournalEntryId, "payment reads back with the same journal ID");

        return new AcceptancePaymentEvidence(recorded, approved, readBack);
    }

    private async Task<AcceptanceEntitlementEvidence> IssueEntitlementAsync(
        AcceptanceHttpClient desk,
        Guid clientId,
        CreateClientContractResponse contract,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var issued = await desk.PostAsync<IssueEntitlementSnapshotFromPaidInvoiceResponse>(
            "api/v1/entitlements/snapshots/from-paid-invoice/defaults",
            new IssueEntitlementSnapshotFromPaidInvoiceDefaultsRequest(
                invoiceId,
                "Paid invoice authorizes P0 connected acceptance"),
            cancellationToken);
        var readBack = await desk.GetAsync<EntitlementSnapshotResponse>(
            $"api/v1/entitlements/clients/{clientId:D}/latest-snapshot",
            cancellationToken);

        _assertions.NotEmpty(issued.EntitlementSnapshotId, "entitlement snapshot ID was allocated");
        _assertions.NotEmpty(issued.ClientAccessRevisionId, "client access revision ID was allocated");
        _assertions.True(issued.EntitlementVersion > 0, "global entitlement version was allocated");
        _assertions.Equal(contract.ContractId, issued.ContractId, "entitlement retains contract provenance");
        _assertions.Equal(contract.RevisionNumber, issued.ContractRevisionNumber, "entitlement retains contract revision");
        _assertions.Equal(contract.ProductCatalogRevisionId, issued.ProductCatalogRevisionId, "entitlement retains catalog revision ID");
        _assertions.Equal(contract.ProductCatalogRevisionNumber, issued.ProductCatalogRevisionNumber, "entitlement retains catalog revision number");
        _assertions.Equal(AllowedDevices, issued.AllowedDevices, "entitlement retains device allowance");
        _assertions.Equal(AllowedBranches, issued.AllowedBranches, "entitlement retains branch allowance");
        _assertions.Equal(AllowedNamedUsers, issued.AllowedNamedUsers, "entitlement retains named-user allowance");
        _assertions.Equal(AllowedConcurrentUsers, issued.AllowedConcurrentUsers, "entitlement retains concurrent-user allowance");
        _assertions.Equal(issued.EntitlementVersion, readBack.EntitlementVersion, "latest snapshot reads back at the issued version");

        return new AcceptanceEntitlementEvidence(issued, readBack);
    }

    private async Task<AcceptanceCloudPublishEvidence> PublishAndReplayAsync(
        AcceptanceHttpClient desk,
        AcceptanceHttpClient cloud,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var listPath = $"api/v1/control-cloud/outbox-messages?clientId={clientId:D}&take=100";
        var beforePublish = await desk.GetAsync<ListCloudOutboxMessagesResponse>(listPath, cancellationToken);
        var requiredMessageTypes = new[]
        {
            "InvoiceIssued",
            "PaymentRecorded",
            "ClientPaidStatusChanged",
            "EntitlementSnapshotIssued"
        };

        foreach (var messageType in requiredMessageTypes)
        {
            _assertions.True(
                beforePublish.Messages.Any(message => message.MessageType == messageType),
                $"office outbox contains {messageType}");
        }

        _assertions.True(
            beforePublish.Messages.All(message => message.Status == "Pending"),
            "target outbox messages are Pending before publish");

        var publish = await desk.PostAsync<PublishCloudOutboxMessagesResponse>(
            "api/v1/control-cloud/outbox-messages/publish?batchSize=100",
            cancellationToken: cancellationToken);
        var targetIds = beforePublish.Messages.Select(message => message.CloudOutboxMessageId).ToHashSet();
        var targetPublishResults = publish.Messages
            .Where(message => targetIds.Contains(message.CloudOutboxMessageId))
            .ToArray();

        _assertions.Equal(0, publish.FailedCount, "outbox publish has zero failures");
        _assertions.Equal(targetIds.Count, targetPublishResults.Length, "every target outbox message has a publish result");
        _assertions.True(targetPublishResults.All(message => message.Status == "Sent"), "every target outbox message is Sent");
        _assertions.True(targetPublishResults.All(message => !string.IsNullOrWhiteSpace(message.CloudReference)), "every target publish has a cloud reference");
        _assertions.True(targetPublishResults.All(message => !string.IsNullOrWhiteSpace(message.EnvelopeSignature)), "every target publish has an envelope signature");

        var afterPublish = await desk.GetAsync<ListCloudOutboxMessagesResponse>(listPath, cancellationToken);
        _assertions.True(afterPublish.Messages.All(message => message.Status == "Sent"), "office outbox reads back as Sent");
        _assertions.True(afterPublish.Messages.All(message => message.SentAtUtc.HasValue), "office outbox retains sent timestamps");

        var replaySource = beforePublish.Messages.Single(message => message.MessageType == "EntitlementSnapshotIssued");
        var replay = ControlCloudReplayEnvelopeFactory.Create(
            replaySource,
            _options.CloudSigningKeyId,
            _options.CloudSigningSecret,
            _options.CloudSourceEnvironment);
        var replayResult = await cloud.PostAsync<ControlCloudReceiveEnvelopeResponse>(
            "api/v1/control-desk/messages",
            replay.Envelope,
            cancellationToken);

        _assertions.Equal("Duplicate", replayResult.Status, "Control Cloud rejects the replay as Duplicate");
        _assertions.Equal(replaySource.CloudOutboxMessageId, replayResult.MessageId, "duplicate receipt retains the original message ID");
        _assertions.NotEmpty(replayResult.ReceiptId, "duplicate replay returns the retained receipt ID");
        _assertions.NotBlank(replayResult.CloudReference, "duplicate replay returns the retained cloud reference");

        return new AcceptanceCloudPublishEvidence(
            beforePublish,
            publish,
            afterPublish,
            new AcceptanceDuplicateReplayEvidence(
                replaySource.CloudOutboxMessageId,
                replaySource.MessageType,
                replayResult.ReceiptId,
                replayResult.CloudReference,
                replayResult.Status,
                replay.SigningKeyId,
                replay.PayloadSha256,
                replay.PreparedAtUtc));
    }

    private async Task<AcceptanceRuntimeEvidence> BootstrapAndReconcileAsync(
        AcceptanceHttpClient desk,
        AcceptanceHttpClient local,
        Guid clientId,
        string installationId,
        string topologyId,
        IssueEntitlementSnapshotFromPaidInvoiceResponse entitlement,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var bootstrap = await desk.PostAsync<LocalServerBootstrapPackageResponse>(
            $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/bootstrap-package",
            new CreateLocalServerBootstrapPackageRequest(
                ExpiresInHours: 24,
                CreatedBy: "connected-acceptance",
                DeploymentMode: ControlCloudBootstrapModes.OnlineBootstrap,
                LocalServerVersion: "acceptance-1.0.0",
                SafarSuiteAppVersion: "acceptance-app-1.0.0",
                ClientDeploymentMode: SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
                SiteId: "acceptance-hq",
                SiteRole: SafarSuiteDeploymentSiteRoles.Hq,
                ParentSiteId: null,
                BranchCode: "HQ",
                SyncTopologyId: topologyId),
            cancellationToken);
        var bootstrapEvidence = new AcceptanceBootstrapPackageEvidence(
            bootstrap.FormatVersion,
            bootstrap.BootstrapPackageId,
            bootstrap.SetupTokenId,
            bootstrap.ClientId,
            bootstrap.InstallationId,
            bootstrap.DeploymentMode,
            bootstrap.LocalServerVersion,
            bootstrap.SetupTokenExpiresAtUtc,
            bootstrap.GeneratedAtUtc,
            bootstrap.BundleFileName,
            bootstrap.BundleContentType,
            bootstrap.BundleSha256,
            bootstrap.SignedBundle.Signature.Algorithm,
            bootstrap.SignedBundle.Signature.KeyId,
            bootstrap.SignedBundle.Signature.PayloadSha256,
            bootstrap.DeploymentProfile);

        _assertions.NotEmpty(bootstrap.BootstrapPackageId, "bootstrap package ID was allocated");
        _assertions.NotEmpty(bootstrap.SetupTokenId, "one-time setup token ID was allocated");
        _assertions.NotBlank(bootstrap.BundleSha256, "bootstrap package SHA-256 was retained");
        _assertions.NotBlank(bootstrap.SignedBundle.Signature.Value, "bootstrap package is signed");

        var imported = await local.PostAsync<AcceptanceLocalBootstrapImportResponse>(
            $"api/v1/local-server/bootstrap-package/import?expectedInstallationId={Uri.EscapeDataString(installationId)}",
            bootstrap.SignedBundle,
            cancellationToken);
        _assertions.Equal(clientId, imported.ClientId, "Local Server bootstrap belongs to the client");
        _assertions.Equal(installationId, imported.InstallationId, "Local Server bootstrap belongs to the installation");
        _assertions.Equal("Registered", imported.BootstrapRegistrationStatus, "Local Server retained the verified bootstrap registration");
        _assertions.Equal("Active", imported.CloudRegistrationStatus, "Control Cloud activated the Local Server registration");

        var pulled = await local.PostAsync<AcceptanceLocalEntitlementPullResponse>(
            "api/v1/local-server/entitlement/pull",
            cancellationToken: cancellationToken);
        _assertions.Equal(entitlement.EntitlementVersion, pulled.EntitlementVersion, "Local Server pulled the issued entitlement version");
        _assertions.Equal(entitlement.PaidUntil, pulled.PaidUntil, "Local Server pulled the exact paid-until date");
        _assertions.Equal(entitlement.OfflineValidUntil, pulled.OfflineValidUntil, "Local Server pulled the exact offline-valid-until date");

        var query = $"asOfDate={today:yyyy-MM-dd}&requestedBy=connected-acceptance";
        var enabled = await local.GetAsync<LocalServerModuleAccessResponse>(
            $"api/v1/local-server/modules/{EnabledModuleCode}/access?{query}",
            cancellationToken);
        var disabled = await local.GetAsync<LocalServerModuleAccessResponse>(
            $"api/v1/local-server/modules/{DisabledModuleCode}/access?{query}",
            cancellationToken);
        var limits = await local.GetAsync<LocalServerEntitlementLimitsResponse>(
            "api/v1/local-server/limits",
            cancellationToken);

        _assertions.True(enabled.IsAllowed, "PAYROLL is allowed locally");
        _assertions.Equal("Active", enabled.AccessState, "PAYROLL local access state is Active");
        _assertions.True(!disabled.IsAllowed, "TOUR is denied locally");
        _assertions.Equal("ModuleDisabled", disabled.AccessState, "TOUR denial is deterministic ModuleDisabled");
        _assertions.Equal(entitlement.EntitlementVersion, limits.EntitlementVersion, "local limits use the issued entitlement version");
        _assertions.Equal(AllowedDevices, limits.AllowedDevices, "local device limit matches entitlement");
        _assertions.Equal(AllowedBranches, limits.AllowedBranches, "local branch limit matches entitlement");
        _assertions.Equal(AllowedNamedUsers, limits.AllowedNamedUsers, "local named-user limit matches entitlement");
        _assertions.Equal(AllowedConcurrentUsers, limits.AllowedConcurrentUsers, "local concurrent-user limit matches entitlement");
        _assertions.True(
            limits.FeatureLimits.Any(limit =>
                limit.ModuleCode == EnabledModuleCode
                && limit.FeatureCode == FeatureCode
                && limit.LimitValue == FeatureLimit),
            "local feature limit matches entitlement");

        var heartbeat = await local.PostAsync<AcceptanceLocalHeartbeatResponse>(
            "api/v1/local-server/heartbeat",
            cancellationToken: cancellationToken);
        _assertions.Equal("Received", heartbeat.HeartbeatStatus, "Control Cloud received the Local Server heartbeat");
        _assertions.Equal(entitlement.EntitlementVersion, heartbeat.EntitlementVersion, "heartbeat reports the issued entitlement version");
        _assertions.True(heartbeat.EntitlementState is not null, "heartbeat reports exact entitlement state values");

        var finalStatus = await WaitForInSyncAsync(
            desk,
            clientId,
            installationId,
            cancellationToken);
        AssertFinalStatus(finalStatus, entitlement);

        var audit = await desk.GetAsync<ControlCloudAuditEventsResponse>(
            $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/audit-events?take=100",
            cancellationToken);
        foreach (var eventType in new[]
                 {
                     "SetupTokenCreated",
                     "BootstrapPackageGenerated",
                     "LocalServerRegistrationAccepted"
                 })
        {
            _assertions.True(
                audit.Events.Any(auditEvent => auditEvent.EventType == eventType),
                $"Control Cloud audit contains {eventType}");
        }

        var importAudit = await ReadImportAuditAsync(installationId, entitlement, cancellationToken);

        return new AcceptanceRuntimeEvidence(
            bootstrapEvidence,
            imported,
            pulled,
            enabled,
            disabled,
            limits,
            heartbeat,
            finalStatus,
            audit,
            importAudit);
    }

    private async Task<ControlCloudInstallationStatusResponse> WaitForInSyncAsync(
        AcceptanceHttpClient desk,
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken)
    {
        var path = $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/status";
        ControlCloudInstallationStatusResponse? latest = null;

        for (var attempt = 0; attempt < 10; attempt += 1)
        {
            latest = await desk.GetAsync<ControlCloudInstallationStatusResponse>(path, cancellationToken);

            if (latest.EntitlementSync.State == "InSync"
                && latest.Reconciliation?.State == "InSync"
                && latest.Reconciliation.Differences.Count == 0)
            {
                return latest;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new ConnectedAcceptanceFailureException(
            $"Installation did not reach InSync. Last sync state: {latest?.EntitlementSync.State ?? "missing"}; " +
            $"reconciliation: {latest?.Reconciliation?.State ?? "missing"}.");
    }

    private void AssertFinalStatus(
        ControlCloudInstallationStatusResponse status,
        IssueEntitlementSnapshotFromPaidInvoiceResponse entitlement)
    {
        _assertions.Equal("Active", status.InstallationStatus, "Control Cloud installation is Active");
        _assertions.Equal(entitlement.EntitlementVersion, status.EntitlementSync.DesiredVersion, "desired entitlement version matches issued version");
        _assertions.Equal(entitlement.EntitlementVersion, status.EntitlementSync.SignedVersion, "signed entitlement version matches issued version");
        _assertions.Equal(entitlement.EntitlementVersion, status.EntitlementSync.ObservedVersion, "observed entitlement version matches issued version");
        _assertions.Equal("InSync", status.EntitlementSync.State, "entitlement version sync is InSync");
        _assertions.True(status.LatestEntitlement is not null, "Control Cloud retains the signed entitlement issue");
        _assertions.NotEmpty(status.LatestEntitlement!.BundleIssueId, "signed bundle issue ID was retained");
        _assertions.True(status.LatestHeartbeat is not null, "Control Cloud retains the latest heartbeat");
        _assertions.NotEmpty(status.LatestHeartbeat!.HeartbeatId, "heartbeat ID was retained");
        _assertions.True(status.Reconciliation is not null, "Control Cloud produced exact-value reconciliation");
        _assertions.Equal("InSync", status.Reconciliation!.State, "exact-value reconciliation is InSync");
        _assertions.Equal(0, status.Reconciliation.Differences.Count, "exact-value reconciliation has zero differences");
        _assertions.True(status.Reconciliation.Desired is not null, "reconciliation contains desired state");
        _assertions.True(status.Reconciliation.Delivered is not null, "reconciliation contains delivered state");
        _assertions.True(status.Reconciliation.Observed is not null, "reconciliation contains observed state");

        AssertStateEquals(status.Reconciliation.Desired!, status.Reconciliation.Delivered!, "desired and delivered");
        AssertStateEquals(status.Reconciliation.Desired!, status.Reconciliation.Observed!, "desired and observed");
        _assertions.Equal(
            status.Reconciliation.Delivered!.WarningStartsAt,
            status.Reconciliation.Observed!.WarningStartsAt,
            "delivered and observed derived warning dates match");
        _assertions.Equal(
            status.LatestEntitlement.WarningStartsAt,
            status.Reconciliation.Delivered.WarningStartsAt,
            "signed bundle and delivered warning dates match");
    }

    private void AssertStateEquals(
        ControlCloudEntitlementStateValuesResponse expected,
        ControlCloudEntitlementStateValuesResponse actual,
        string label)
    {
        _assertions.Equal(expected.EntitlementVersion, actual.EntitlementVersion, $"{label} versions match");
        _assertions.Equal(expected.EffectiveFromUtc, actual.EffectiveFromUtc, $"{label} effective timestamps match");
        _assertions.Equal(expected.Status, actual.Status, $"{label} statuses match");
        _assertions.Equal(expected.PaidUntil, actual.PaidUntil, $"{label} paid-until dates match");
        _assertions.Equal(expected.GraceUntil, actual.GraceUntil, $"{label} grace dates match");
        _assertions.Equal(expected.OfflineValidUntil, actual.OfflineValidUntil, $"{label} offline-valid dates match");
        _assertions.Equal(expected.AllowedDevices, actual.AllowedDevices, $"{label} device limits match");
        _assertions.Equal(expected.AllowedBranches, actual.AllowedBranches, $"{label} branch limits match");
        _assertions.Equal(expected.AllowedNamedUsers, actual.AllowedNamedUsers, $"{label} named-user limits match");
        _assertions.Equal(expected.AllowedConcurrentUsers, actual.AllowedConcurrentUsers, $"{label} concurrent-user limits match");
        _assertions.Equal(
            CanonicalModules(expected.Modules),
            CanonicalModules(actual.Modules),
            $"{label} module values match");
        _assertions.Equal(
            CanonicalLimits(expected.FeatureLimits),
            CanonicalLimits(actual.FeatureLimits),
            $"{label} feature-limit values match");
    }

    private async Task<IReadOnlyCollection<AcceptanceLocalImportAuditRecord>> ReadImportAuditAsync(
        string installationId,
        IssueEntitlementSnapshotFromPaidInvoiceResponse entitlement,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.LocalImportAuditPath))
        {
            throw new ConnectedAcceptanceFailureException(
                "--local-import-audit-path is required so the accepted local import can be retained as evidence.");
        }

        var fullPath = Path.GetFullPath(_options.LocalImportAuditPath);

        if (!File.Exists(fullPath))
        {
            throw new ConnectedAcceptanceFailureException(
                $"Local import audit file was not found at {fullPath}.");
        }

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var records = await JsonSerializer.DeserializeAsync<List<AcceptanceLocalImportAuditRecord>>(
            stream,
            AcceptanceHttpClient.JsonOptions,
            cancellationToken) ?? [];
        var targetRecords = records
            .Where(record => record.InstallationId == installationId)
            .OrderBy(record => record.OccurredAtUtc)
            .ToArray();

        _assertions.True(
            targetRecords.Any(record =>
                record.ImportSource == "ControlCloudPull"
                && record.ResultStatus == "Accepted"
                && record.EntitlementVersion == entitlement.EntitlementVersion),
            "local import audit retains the accepted Control Cloud pull");

        return targetRecords;
    }

    private async Task<string> WriteEvidenceAsync(
        ConnectedAcceptanceEvidence evidence,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(_options.EvidencePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var jsonOptions = new JsonSerializerOptions(AcceptanceHttpClient.JsonOptions)
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(evidence, jsonOptions);
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var evidenceBytes = utf8.GetBytes(json);
        await File.WriteAllBytesAsync(fullPath, evidenceBytes, cancellationToken);
        var sha256 = Convert.ToHexString(SHA256.HashData(evidenceBytes))
            .ToLowerInvariant();
        await File.WriteAllBytesAsync(
            fullPath + ".sha256",
            utf8.GetBytes(sha256 + Environment.NewLine),
            cancellationToken);

        return sha256;
    }

    private static BootstrapStandardChartOfAccountsItemResponse FindAccount(
        BootstrapStandardChartOfAccountsResponse response,
        string role)
    {
        return response.Accounts.Single(account => account.Role == role);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string CanonicalModules(
        IReadOnlyCollection<ControlCloudEntitlementStateModuleResponse> modules)
    {
        return string.Join(
            ";",
            modules
                .OrderBy(module => module.ModuleCode, StringComparer.Ordinal)
                .Select(module => $"{module.ModuleCode}:{module.IsEnabled}"));
    }

    private static string CanonicalLimits(
        IReadOnlyCollection<ControlCloudEntitlementStateFeatureLimitResponse> limits)
    {
        return string.Join(
            ";",
            limits
                .OrderBy(limit => limit.ModuleCode, StringComparer.Ordinal)
                .ThenBy(limit => limit.FeatureCode, StringComparer.Ordinal)
                .Select(limit => $"{limit.ModuleCode}:{limit.FeatureCode}:{limit.LimitValue}:{limit.Unit}"));
    }
}

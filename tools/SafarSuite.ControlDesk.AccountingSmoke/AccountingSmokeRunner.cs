using System.Numerics;
using System.Text.Json;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CloseAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureDefaultAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureVoucherNumberingRule;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseJournalPreview;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListVoucherNumberingRules;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetBalanceSheet;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntrySourceDocument;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountCodeRangeValidation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetProfitAndLossStatement;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetRevenueSummary;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PostOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewChartOfAccountsImportText;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewJournalVoucherNumber;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;
using SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetAccountsReceivableAging;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetCreditNoteDocument;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetInvoiceDocument;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListOutstandingInvoices;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.Financials;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetClientRefundDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetInvoicePaymentDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;
using SafarSuite.ControlDesk.Application.Modules.Payments.ListPaymentReceiptsReport;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;
using SafarSuite.ControlDesk.Infrastructure.System;
using Microsoft.Extensions.Options;

namespace SafarSuite.ControlDesk.AccountingSmoke;

internal sealed class AccountingSmokeRunner
{
    private const string CurrencyCode = "PKR";
    private const string SigningKeyId = "local-dev";
    private const string SigningSecret = "local-development-signing-secret-change-before-cloud";
    private static readonly Guid ProductCatalogRevisionId =
        Guid.Parse("9c1da88b-c763-4bb0-8dda-2d95fe63ec8f");

    private readonly SmokeHarness _harness;
    private readonly SmokeOptions _options;
    private readonly string _runId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");

    public AccountingSmokeRunner(SmokeHarness harness, SmokeOptions options)
    {
        _harness = harness;
        _options = options;
    }

    public string RunId => _runId;

    public int PublishedCloudMessageCount { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var businessDate = new DateOnly(2026, 7, 1);
        var accounts = await CreateLedgerAccountsAsync(cancellationToken);
        await AssertLedgerAccountReconciliationAsync(accounts, cancellationToken);
        await AssertAccountCodeRangeValidationAsync(cancellationToken);
        await AssertLedgerAccountGuardrailsAsync(accounts, cancellationToken);
        await AssertPaymentPostingAccountGuardsAsync(accounts, cancellationToken);
        await AssertChartOfAccountsImportPreviewAsync(accounts, cancellationToken);
        await AssertPostingPeriodGuardAsync(cancellationToken);
        await AssertVoucherAndOpeningBalancePreviewAsync(accounts, businessDate, cancellationToken);
        var client = await CreateClientAsync(cancellationToken);
        var contract = await CreateContractAsync(client.ClientId, businessDate, cancellationToken);
        SmokeAssertions.Equal(1L, contract.RevisionNumber, "initial contract revision");
        SmokeAssertions.True(contract.SupersedesContractId is null, "initial contract revision must be the chain root");
        SmokeAssertions.Equal(ProductCatalogRevisionId, contract.ProductCatalogRevisionId, "initial contract catalog revision id");
        SmokeAssertions.Equal(1L, contract.ProductCatalogRevisionNumber, "initial contract catalog revision number");
        SmokeAssertions.Equal(40, contract.AllowedNamedUsers ?? -1, "initial contract named-user allowance");
        SmokeAssertions.Equal(12, contract.AllowedConcurrentUsers ?? -1, "initial contract concurrent-user allowance");
        var contractFeatureLimit = contract.FeatureLimits?.SingleOrDefault();
        SmokeAssertions.True(contractFeatureLimit is not null, "initial contract should retain its feature limit.");
        SmokeAssertions.Equal("BILLING", contractFeatureLimit!.ModuleCode, "initial contract feature-limit module");
        SmokeAssertions.Equal("MONTHLY_INVOICES", contractFeatureLimit.FeatureCode, "initial contract feature-limit code");
        SmokeAssertions.Equal(2500L, contractFeatureLimit.LimitValue, "initial contract feature-limit value");
        SmokeAssertions.Equal("COUNT", contractFeatureLimit.Unit, "initial contract feature-limit unit");
        var chargeCode = await CreateBillingSetupAsync(
            client.ClientId,
            contract.ContractId,
            accounts.RevenueAccountId,
            accounts.TaxAccountId,
            businessDate,
            cancellationToken);

        await ConfigureAccountingProfileAsync(client.ClientId, accounts.AccountsReceivableAccountId, cancellationToken);

        var firstInvoice = await DraftInvoiceAsync(
            client.ClientId,
            contract.ContractId,
            Document("INV-001"),
            businessDate,
            cancellationToken);
        SmokeAssertions.Money(110m, firstInvoice.TotalAmount, "first invoice total");
        SmokeAssertions.Equal(2, firstInvoice.Lines.Count, "first invoice line count");

        var firstIssue = await IssueInvoiceAsync(
            firstInvoice.InvoiceId,
            accounts.AccountsReceivableAccountId,
            businessDate,
            cancellationToken);
        AssertBalanced(firstIssue.TotalDebit, firstIssue.TotalCredit, "first invoice journal");
        AssertJournalLineAccountMetadata(
            firstIssue.JournalLines.Select(line => (
                line.LedgerAccountCode,
                line.LedgerAccountName,
                line.LedgerAccountType,
                line.LedgerAccountNormalBalance,
                line.LedgerAccountLevel,
                line.IsPostingAccount,
                line.LedgerAccountStatus)),
            "first invoice journal");
        SmokeAssertions.Equal("Issued", firstIssue.InvoiceStatus, "first invoice issue status");

        var payment = await RecordPaymentAsync(
            firstInvoice.InvoiceId,
            accounts.CashOrBankAccountId,
            accounts.AccountsReceivableAccountId,
            businessDate,
            cancellationToken);
        AssertBalanced(payment.TotalDebit, payment.TotalCredit, "payment journal");
        AssertJournalLineAccountMetadata(
            payment.JournalLines.Select(line => (
                line.LedgerAccountCode,
                line.LedgerAccountName,
                line.LedgerAccountType,
                line.LedgerAccountNormalBalance,
                line.LedgerAccountLevel,
                line.IsPostingAccount,
                line.LedgerAccountStatus)),
            "payment journal");
        SmokeAssertions.Equal("Paid", payment.InvoiceStatus, "paid invoice status");
        SmokeAssertions.Money(0m, payment.BalanceDue, "paid invoice balance");

        await AssertEntitlementVersioningAsync(
            client.ClientId,
            firstInvoice.InvoiceId,
            businessDate,
            cancellationToken);

        var creditNote = await IssueCreditNoteAsync(firstInvoice.InvoiceId, businessDate, cancellationToken);
        AssertBalanced(creditNote.TotalDebit, creditNote.TotalCredit, "credit note journal");
        AssertJournalLineAccountMetadata(
            creditNote.JournalLines.Select(line => (
                line.LedgerAccountCode,
                line.LedgerAccountName,
                line.LedgerAccountType,
                line.LedgerAccountNormalBalance,
                line.LedgerAccountLevel,
                line.IsPostingAccount,
                line.LedgerAccountStatus)),
            "credit note journal");
        SmokeAssertions.Money(110m, creditNote.Amount, "credit note amount");

        var refund = await IssueRefundAsync(
            client.ClientId,
            accounts.CashOrBankAccountId,
            accounts.AccountsReceivableAccountId,
            businessDate,
            cancellationToken);
        AssertBalanced(refund.TotalDebit, refund.TotalCredit, "client refund journal");
        AssertJournalLineAccountMetadata(
            refund.JournalLines.Select(line => (
                line.LedgerAccountCode,
                line.LedgerAccountName,
                line.LedgerAccountType,
                line.LedgerAccountNormalBalance,
                line.LedgerAccountLevel,
                line.IsPostingAccount,
                line.LedgerAccountStatus)),
            "client refund journal");
        SmokeAssertions.Money(-110m, refund.ClientBalanceBefore, "refund balance before");
        SmokeAssertions.Money(-70m, refund.ClientBalanceAfter, "refund balance after");

        await AssertSourceDocumentReadbackAsync(
            firstInvoice,
            firstIssue,
            payment,
            creditNote,
            refund,
            cancellationToken);

        var secondInvoice = await DraftInvoiceAsync(
            client.ClientId,
            contract.ContractId,
            Document("INV-002"),
            businessDate.AddDays(1),
            cancellationToken);
        _ = chargeCode;

        var secondIssue = await IssueInvoiceAsync(
            secondInvoice.InvoiceId,
            accounts.AccountsReceivableAccountId,
            businessDate.AddDays(1),
            cancellationToken);
        AssertBalanced(secondIssue.TotalDebit, secondIssue.TotalCredit, "second invoice journal");

        var creditApplication = await ApplyCreditAsync(
            client.ClientId,
            secondInvoice.InvoiceId,
            businessDate.AddDays(1),
            cancellationToken);
        SmokeAssertions.Equal("PartiallyPaid", creditApplication.InvoiceStatus, "settled invoice status");
        SmokeAssertions.Money(110m, creditApplication.InvoiceBalanceBefore, "settlement invoice balance before");
        SmokeAssertions.Money(50m, creditApplication.InvoiceBalanceAfter, "settlement invoice balance after");
        SmokeAssertions.Money(70m, creditApplication.AvailableCreditBefore, "settlement available credit before");
        SmokeAssertions.Money(10m, creditApplication.AvailableCreditAfter, "settlement available credit after");
        SmokeAssertions.Money(40m, creditApplication.ClientBalanceBefore, "settlement client balance before");
        SmokeAssertions.Money(40m, creditApplication.ClientBalanceAfter, "settlement client balance after");

        await AssertGeneralLedgerReportsAsync(accounts, businessDate, cancellationToken);
        await AssertFinalStatementAsync(client.ClientId, cancellationToken);
        await AssertCrossClientReportsAsync(client.ClientId, businessDate, cancellationToken);
        await AssertContractRevisioningAsync(client.ClientId, contract, businessDate, cancellationToken);
        await AssertJournalAndOutboxShapeAsync(client.ClientId, cancellationToken);
        await AssertPeriodCloseBalanceSheetHandoffAsync(accounts, businessDate, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.CloudReceiverUrl))
        {
            PublishedCloudMessageCount = await PublishOutboxToCloudAsync(client.ClientId, cancellationToken);
        }
    }

    private async Task<LedgerAccounts> CreateLedgerAccountsAsync(CancellationToken cancellationToken)
    {
        var assetHeader = await GetOrCreateReusableLedgerAccountAsync(
            "AssetHeader",
            "Asset header",
            "Header",
            cancellationToken);
        var assetTotal = await GetOrCreateReusableLedgerAccountAsync(
            "AssetTotal",
            "Asset total",
            "T",
            cancellationToken);
        var accountsReceivableControl = await GetOrCreateReusableLedgerAccountAsync(
            "ReceivableControl",
            "Accounts receivable control",
            null,
            cancellationToken);
        var accountsReceivableSuggestion = await SuggestLedgerAccountCodeAsync(
            "ClientReceivable",
            accountsReceivableControl.LedgerAccountId,
            cancellationToken);
        SmokeAssertions.Equal(9, accountsReceivableSuggestion.SuggestedCode.Length, "receivable setup suggestion length");
        SmokeAssertions.Equal(
            accountsReceivableControl.LedgerAccountId,
            accountsReceivableSuggestion.ParentAccountId ?? Guid.Empty,
            "receivable suggestion parent account");
        SmokeAssertions.Equal(
            accountsReceivableControl.Code,
            accountsReceivableSuggestion.ParentAccountCode ?? "",
            "receivable suggestion parent code");
        SmokeAssertions.Equal("Header", assetHeader.Level, "asset header level");
        SmokeAssertions.Equal("Total", assetTotal.Level, "asset total level");

        var nestedAssetHeaderSuggestion = await SuggestLedgerAccountCodeAsync(
            "AssetHeader",
            assetHeader.LedgerAccountId,
            cancellationToken);
        var nestedAssetHeader = SmokeAssertions.RequireSuccess(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nestedAssetHeaderSuggestion.SuggestedCode,
                    "Nested asset header",
                    nestedAssetHeaderSuggestion.Type,
                    nestedAssetHeaderSuggestion.NormalBalance,
                    assetHeader.LedgerAccountId,
                    nestedAssetHeaderSuggestion.IsPostingAccount,
                    "Header"),
                cancellationToken),
            "create nested asset header account");
        SmokeAssertions.Equal(
            assetHeader.LedgerAccountId,
            nestedAssetHeader.ParentAccountId ?? Guid.Empty,
            "nested asset header parent account");
        SmokeAssertions.Equal("Header", nestedAssetHeader.Level, "nested asset header level");

        var nestedAssetGrandchildSuggestion = await SuggestLedgerAccountCodeAsync(
            "AssetHeader",
            nestedAssetHeader.LedgerAccountId,
            cancellationToken);
        var nestedAssetGrandchild = SmokeAssertions.RequireSuccess(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nestedAssetGrandchildSuggestion.SuggestedCode,
                    "Nested asset grandchild",
                    nestedAssetGrandchildSuggestion.Type,
                    nestedAssetGrandchildSuggestion.NormalBalance,
                    nestedAssetHeader.LedgerAccountId,
                    nestedAssetGrandchildSuggestion.IsPostingAccount,
                    "Header"),
                cancellationToken),
            "create nested asset grandchild account");
        SmokeAssertions.Equal(
            nestedAssetHeader.LedgerAccountId,
            nestedAssetGrandchild.ParentAccountId ?? Guid.Empty,
            "nested asset grandchild parent account");
        SmokeAssertions.Equal("Header", nestedAssetGrandchild.Level, "nested asset grandchild level");
        var cashBankControl = await GetOrCreateReusableLedgerAccountAsync(
            "CashBankControl",
            "Cash and bank control",
            null,
            cancellationToken);

        var accountsReceivable = SmokeAssertions.RequireSuccess(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    accountsReceivableSuggestion.SuggestedCode,
                    "Accounts receivable",
                    accountsReceivableSuggestion.Type,
                    accountsReceivableSuggestion.NormalBalance,
                    null,
                    accountsReceivableSuggestion.IsPostingAccount),
                cancellationToken),
            "create accounts receivable account");
        SmokeAssertions.Equal(
            accountsReceivableControl.LedgerAccountId,
            accountsReceivable.ParentAccountId ?? Guid.Empty,
            "receivable parent account");
        SmokeAssertions.Equal("Control", accountsReceivableControl.Level, "receivable control level");
        SmokeAssertions.Equal("Subsidiary", accountsReceivable.Level, "receivable subsidiary level");
        SmokeAssertions.Equal("Control", cashBankControl.Level, "cash bank control level");

        var cashBankSuggestion = await SuggestLedgerAccountCodeAsync(
            "CashBank",
            cashBankControl.LedgerAccountId,
            cancellationToken);
        var cashOrBank = SmokeAssertions.RequireSuccess(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    cashBankSuggestion.SuggestedCode,
                    "Cash and bank",
                    cashBankSuggestion.Type,
                    cashBankSuggestion.NormalBalance,
                    cashBankControl.LedgerAccountId,
                    cashBankSuggestion.IsPostingAccount,
                    "Subsidiary"),
                cancellationToken),
            "create cash bank subsidiary account");
        SmokeAssertions.Equal(
            cashBankControl.LedgerAccountId,
            cashOrBank.ParentAccountId ?? Guid.Empty,
            "cash bank subsidiary parent account");
        SmokeAssertions.Equal("Subsidiary", cashOrBank.Level, "cash bank subsidiary level");
        var revenue = await GetOrCreateReusableLedgerAccountAsync(
            "SubscriptionRevenue",
            "Subscription revenue",
            null,
            cancellationToken);
        var tax = await GetOrCreateReusableLedgerAccountAsync(
            "TaxPayable",
            "Tax payable",
            null,
            cancellationToken);
        var retainedEarnings = await GetOrCreateReusableLedgerAccountAsync(
            "RetainedEarnings",
            "Retained earnings",
            null,
            cancellationToken);
        var incomeSummary = await GetOrCreateReusableLedgerAccountAsync(
            "IncomeSummary",
            "Income summary",
            null,
            cancellationToken);
        var roundingAdjustment = await GetOrCreateReusableLedgerAccountAsync(
            "RoundingAdjustment",
            "Rounding adjustment",
            null,
            cancellationToken);

        var nextAccountsReceivableSuggestion = await SuggestLedgerAccountCodeAsync(
            "ClientReceivable",
            accountsReceivableControl.LedgerAccountId,
            cancellationToken);
        SmokeAssertions.Equal(
            BigInteger.Parse(accountsReceivableSuggestion.SuggestedCode) + BigInteger.One,
            BigInteger.Parse(nextAccountsReceivableSuggestion.SuggestedCode),
            "next receivable setup suggestion");

        return new LedgerAccounts(
            accountsReceivableControl.LedgerAccountId,
            accountsReceivable.LedgerAccountId,
            cashBankControl.LedgerAccountId,
            cashOrBank.LedgerAccountId,
            revenue.LedgerAccountId,
            tax.LedgerAccountId,
            retainedEarnings.LedgerAccountId,
            incomeSummary.LedgerAccountId,
            roundingAdjustment.LedgerAccountId,
            [
                assetHeader.LedgerAccountId,
                assetTotal.LedgerAccountId,
                nestedAssetHeader.LedgerAccountId,
                nestedAssetGrandchild.LedgerAccountId,
                cashBankControl.LedgerAccountId,
                accountsReceivableControl.LedgerAccountId,
                accountsReceivable.LedgerAccountId,
                cashOrBank.LedgerAccountId,
                revenue.LedgerAccountId,
                tax.LedgerAccountId,
                retainedEarnings.LedgerAccountId,
                incomeSummary.LedgerAccountId,
                roundingAdjustment.LedgerAccountId
            ]);
    }

    private async Task<CreateLedgerAccountResult> GetOrCreateReusableLedgerAccountAsync(
        string role,
        string name,
        string? level,
        CancellationToken cancellationToken)
    {
        var existing = await FindReusableLedgerAccountAsync(role, cancellationToken);

        if (existing is not null)
        {
            return ToCreateLedgerAccountResult(existing);
        }

        var suggestion = await SuggestLedgerAccountCodeAsync(role, cancellationToken);

        return SmokeAssertions.RequireSuccess(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    suggestion.SuggestedCode,
                    name,
                    suggestion.Type,
                    suggestion.NormalBalance,
                    null,
                    suggestion.IsPostingAccount,
                    level),
                cancellationToken),
            $"create {name} account");
    }

    private async Task<LedgerAccount?> FindReusableLedgerAccountAsync(
        string role,
        CancellationToken cancellationToken)
    {
        await _harness.AccountingSetupDefaults.EnsureSeededAsync(
            AccountingSetupDefaults.DefaultCompanyCode,
            cancellationToken);

        var range = await _harness.AccountCodeRanges.GetByCompanyAndRoleAsync(
            AccountingSetupDefaults.DefaultCompanyCode,
            role,
            cancellationToken);

        if (range is null || !range.IsActive)
        {
            return null;
        }

        var accounts = (await _harness.LedgerAccounts.ListAsync(cancellationToken: cancellationToken))
            .ToArray();
        var accountsById = accounts.ToDictionary(account => account.Id.Value);

        return accounts
            .Where(account => IsInsideRange(account.Code.Value, range)
                && IsCompatibleWithRange(account, range, accountsById))
            .OrderBy(account => account.Code.Value, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private async Task<SuggestLedgerAccountCodeResult> SuggestLedgerAccountCodeAsync(
        string role,
        CancellationToken cancellationToken)
    {
        return await SuggestLedgerAccountCodeAsync(role, null, cancellationToken);
    }

    private async Task<SuggestLedgerAccountCodeResult> SuggestLedgerAccountCodeAsync(
        string role,
        Guid? parentAccountId,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.SuggestLedgerAccountCode.HandleAsync(
                new SuggestLedgerAccountCodeQuery(role, ParentAccountId: parentAccountId),
                cancellationToken),
            $"suggest ledger account code for {role}");
    }

    private async Task AssertLedgerAccountReconciliationAsync(
        LedgerAccounts accounts,
        CancellationToken cancellationToken)
    {
        var reconciliation = SmokeAssertions.RequireSuccess(
            await _harness.GetLedgerAccountReconciliation.HandleAsync(
                new GetLedgerAccountReconciliationQuery(null),
                cancellationToken),
            "get ledger account reconciliation");

        if (_options.Provider == "inmemory")
        {
            SmokeAssertions.Equal(0, reconciliation.IssueCount, "ledger account reconciliation issue count");
            await AssertLedgerAccountRepairPlanAsync(accounts, expectedActionCount: 0, cancellationToken);

            return;
        }

        var smokeAccountIds = accounts.ReconciledLedgerAccountIds.ToHashSet();
        var smokeIssueCount = reconciliation.Items
            .Where(item => smokeAccountIds.Contains(item.LedgerAccountId))
            .Sum(item => item.Issues.Count);

        SmokeAssertions.Equal(0, smokeIssueCount, "smoke ledger account reconciliation issue count");
        await AssertLedgerAccountRepairPlanAsync(accounts, expectedActionCount: 0, cancellationToken);
    }

    private async Task AssertLedgerAccountRepairPlanAsync(
        LedgerAccounts accounts,
        int expectedActionCount,
        CancellationToken cancellationToken)
    {
        var repairPlan = SmokeAssertions.RequireSuccess(
            await _harness.GetLedgerAccountRepairPlan.HandleAsync(
                new GetLedgerAccountRepairPlanQuery(null),
                cancellationToken),
            "get ledger account repair plan");
        var accountIds = accounts.ReconciledLedgerAccountIds.ToHashSet();
        var actionCount = _options.Provider == "inmemory"
            ? repairPlan.ActionCount
            : repairPlan.Items
                .Where(item => accountIds.Contains(item.LedgerAccountId))
                .Sum(item => item.Actions.Count);

        SmokeAssertions.Equal(expectedActionCount, actionCount, "ledger account repair plan action count");
    }

    private async Task AssertAccountCodeRangeValidationAsync(CancellationToken cancellationToken)
    {
        var validation = SmokeAssertions.RequireSuccess(
            await _harness.GetAccountCodeRangeValidation.HandleAsync(
                new GetAccountCodeRangeValidationQuery(null),
                cancellationToken),
            "get account code range validation");

        if (_options.Provider == "inmemory")
        {
            SmokeAssertions.True(validation.IsValid, "default account code ranges should be valid");
            SmokeAssertions.Equal(0, validation.ErrorCount, "default account code range error count");
            SmokeAssertions.Equal(0, validation.WarningCount, "default account code range warning count");
        }

        await AssertPollutedAccountCodeRangeValidationAsync(cancellationToken);
    }

    private static async Task AssertPollutedAccountCodeRangeValidationAsync(CancellationToken cancellationToken)
    {
        var ranges = new InMemoryAccountCodeRangeRepository();
        var ledgerAccounts = new InMemoryLedgerAccountRepository();
        var defaults = new AccountingSetupDefaults(
            ranges,
            new NoOpUnitOfWork(),
            new GuidIdGenerator(),
            new SystemClock());

        await ranges.AddAsync(CreateSmokeRange(
            "SmokeAssetOverlap",
            "Smoke asset overlap",
            "100",
            "10050",
            "10060",
            5,
            LedgerAccountType.Asset,
            NormalBalance.Debit,
            isPostingAccount: false,
            parentCode: null), cancellationToken);
        await ranges.AddAsync(CreateSmokeRange(
            "SmokeMissingParent",
            "Smoke missing parent",
            "88900",
            "889000001",
            "889009999",
            9,
            LedgerAccountType.Expense,
            NormalBalance.Debit,
            isPostingAccount: true,
            parentCode: "88900"), cancellationToken);

        var handler = new GetAccountCodeRangeValidationHandler(
            ranges,
            ledgerAccounts,
            defaults);
        var validation = SmokeAssertions.RequireSuccess(
            await handler.HandleAsync(new GetAccountCodeRangeValidationQuery(null), cancellationToken),
            "get polluted account code range validation");

        SmokeAssertions.True(!validation.IsValid, "polluted account code ranges should be invalid");
        SmokeAssertions.True(
            validation.Issues.Any(issue => issue.Code == "OverlappingRange"),
            "polluted account code ranges should detect overlap");
        SmokeAssertions.True(
            validation.Issues.Any(issue => issue.Code == "ParentCodeNotCovered"),
            "polluted account code ranges should detect missing parent range");
    }

    private static AccountCodeRange CreateSmokeRange(
        string role,
        string displayName,
        string searchPrefix,
        string rangeStart,
        string rangeEnd,
        int codeLength,
        LedgerAccountType accountType,
        NormalBalance normalBalance,
        bool isPostingAccount,
        string? parentCode)
    {
        return AccountCodeRange.Create(
            AccountCodeRangeId.Create(Guid.NewGuid()),
            AccountingSetupDefaults.DefaultCompanyCode,
            role,
            displayName,
            searchPrefix,
            rangeStart,
            rangeEnd,
            codeLength,
            accountType,
            normalBalance,
            isPostingAccount,
            parentCode,
            isActive: true,
            DateTimeOffset.UtcNow);
    }

    private async Task AssertLedgerAccountGuardrailsAsync(
        LedgerAccounts accounts,
        CancellationToken cancellationToken)
    {
        SmokeAssertions.RequireFailure(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    $"LEGACY-{RunId}",
                    "Legacy loose code should be rejected",
                    "Asset",
                    "Debit",
                    null,
                    true,
                    "Detail"),
                cancellationToken),
            "reject non-numeric ledger account code",
            nameof(CreateLedgerAccountCommand.Code),
            "numeric");

        SmokeAssertions.RequireFailure(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    "999",
                    "Outside setup range should be rejected",
                    "Asset",
                    "Debit",
                    null,
                    true,
                    "Detail"),
                cancellationToken),
            "reject outside-range ledger account code",
            nameof(CreateLedgerAccountCommand.Code),
            "outside the active accounting setup ranges");

        var nextAssetHeader = await SuggestLedgerAccountCodeAsync("AssetHeader", cancellationToken);
        SmokeAssertions.RequireFailure(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nextAssetHeader.SuggestedCode,
                    "Posting header should be rejected",
                    nextAssetHeader.Type,
                    nextAssetHeader.NormalBalance,
                    null,
                    true,
                    "Header"),
                cancellationToken),
            "reject posting header account",
            nameof(CreateLedgerAccountCommand.IsPostingAccount),
            "Posting flag must be False");

        var nextReceivable = await SuggestLedgerAccountCodeAsync("ClientReceivable", cancellationToken);
        var nestedReceivable = SmokeAssertions.RequireSuccess(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nextReceivable.SuggestedCode,
                    "Nested receivable descendant should be allowed",
                    nextReceivable.Type,
                    nextReceivable.NormalBalance,
                    accounts.AccountsReceivableAccountId,
                    nextReceivable.IsPostingAccount,
                    "Subsidiary"),
                cancellationToken),
            "allow subsidiary under same-range descendant parent");
        SmokeAssertions.Equal(
            accounts.AccountsReceivableAccountId,
            nestedReceivable.ParentAccountId ?? Guid.Empty,
            "nested receivable descendant parent account");

        nextReceivable = await SuggestLedgerAccountCodeAsync(
            "ClientReceivable",
            accounts.AccountsReceivableControlId,
            cancellationToken);

        SmokeAssertions.RequireFailure(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nextReceivable.SuggestedCode,
                    "Subsidiary under wrong control should be rejected",
                    nextReceivable.Type,
                    nextReceivable.NormalBalance,
                    accounts.CashOrBankControlId,
                    nextReceivable.IsPostingAccount,
                    "Subsidiary"),
                cancellationToken),
            "reject explicit child range under wrong control",
            nameof(CreateLedgerAccountCommand.ParentAccountId),
            "must be 15100");

        var nextCashBank = await SuggestLedgerAccountCodeAsync(
            "CashBank",
            accounts.CashOrBankControlId,
            cancellationToken);
        var nestedCashBank = SmokeAssertions.RequireSuccess(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nextCashBank.SuggestedCode,
                    "Nested cash bank descendant should be allowed",
                    nextCashBank.Type,
                    nextCashBank.NormalBalance,
                    accounts.CashOrBankAccountId,
                    nextCashBank.IsPostingAccount,
                    "Subsidiary"),
                cancellationToken),
            "allow cash bank under same-range descendant parent");
        SmokeAssertions.Equal(
            accounts.CashOrBankAccountId,
            nestedCashBank.ParentAccountId ?? Guid.Empty,
            "nested cash bank descendant parent account");

        nextCashBank = await SuggestLedgerAccountCodeAsync(
            "CashBank",
            accounts.CashOrBankControlId,
            cancellationToken);

        SmokeAssertions.RequireFailure(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nextCashBank.SuggestedCode,
                    "Cash bank under receivable control should be rejected",
                    nextCashBank.Type,
                    nextCashBank.NormalBalance,
                    accounts.AccountsReceivableControlId,
                    nextCashBank.IsPostingAccount,
                    "Subsidiary"),
                cancellationToken),
            "reject open posting range under unrelated control",
            nameof(CreateLedgerAccountCommand.ParentAccountId),
            "cannot own codes from range");
    }

    private async Task AssertPaymentPostingAccountGuardsAsync(
        LedgerAccounts accounts,
        CancellationToken cancellationToken)
    {
        var cashOrBankAccount = await _harness.LedgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(accounts.CashOrBankAccountId),
            cancellationToken);

        if (cashOrBankAccount is null)
        {
            throw new SmokeFailureException("cash or bank account should exist for posting account guard smoke.");
        }

        var postingService = new PaymentPostingService(
            _harness.LedgerAccounts,
            new GuidIdGenerator(),
            new SystemClock());

        cashOrBankAccount.Deactivate();

        try
        {
            SmokeAssertions.RequireFailure(
                await postingService.ValidateAssetPostingAccountAsync(
                    LedgerAccountId.Create(accounts.CashOrBankAccountId),
                    nameof(RecordInvoicePaymentCommand.CashOrBankAccountId),
                    "Cash or bank account",
                    cancellationToken),
                "reject inactive payment posting account",
                nameof(RecordInvoicePaymentCommand.CashOrBankAccountId),
                "active");
        }
        finally
        {
            cashOrBankAccount.Activate();
        }
    }

    private static async Task AssertPostingPeriodGuardAsync(CancellationToken cancellationToken)
    {
        const string Target = "PostingDate";
        var periods = new InMemoryAccountingPeriodRepository();
        var guard = new AccountingPeriodPostingGuard(periods);
        var julyPostingDate = new DateOnly(2026, 7, 15);

        SmokeAssertions.True(
            await guard.ValidateOpenPeriodAsync(julyPostingDate, Target, cancellationToken: cancellationToken) is null,
            "posting guard should allow posting before MAIN periods are configured.");

        var julyPeriod = AccountingPeriod.Create(
            AccountingPeriodId.Create(Guid.NewGuid()),
            AccountingSetupDefaults.DefaultCompanyCode,
            "July 2026",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
        await periods.AddAsync(julyPeriod, cancellationToken);

        SmokeAssertions.True(
            await guard.ValidateOpenPeriodAsync(julyPostingDate, Target, cancellationToken: cancellationToken) is null,
            "posting guard should allow dates in an open MAIN period.");

        var missingPeriodError = await guard.ValidateOpenPeriodAsync(
            new DateOnly(2026, 8, 1),
            Target,
            cancellationToken: cancellationToken);

        if (missingPeriodError is null)
        {
            throw new SmokeFailureException("posting guard should block dates outside configured MAIN periods.");
        }

        SmokeAssertions.Equal("validation", missingPeriodError.Code, "missing posting period error code");
        SmokeAssertions.Equal(Target, missingPeriodError.Target ?? "", "missing posting period error target");
        SmokeAssertions.True(
            missingPeriodError.Message.Contains(
                "No MAIN accounting period contains posting date 2026-08-01",
                StringComparison.OrdinalIgnoreCase),
            "missing posting period error should name the MAIN calendar.");

        julyPeriod.Close(
            new DateTimeOffset(2026, 7, 31, 23, 59, 0, TimeSpan.Zero),
            AccountingPeriodCloseArtifact.Create(
                new DateTimeOffset(2026, 7, 31, 23, 59, 0, TimeSpan.Zero),
                "smoke",
                1,
                0,
                1,
                0,
                0,
                "{}"));

        var closedPeriodError = await guard.ValidateOpenPeriodAsync(
            julyPostingDate,
            Target,
            cancellationToken: cancellationToken);

        if (closedPeriodError is null)
        {
            throw new SmokeFailureException("posting guard should block dates in closed MAIN periods.");
        }

        SmokeAssertions.Equal("conflict", closedPeriodError.Code, "closed posting period error code");
        SmokeAssertions.Equal(Target, closedPeriodError.Target ?? "", "closed posting period error target");
        SmokeAssertions.True(
            closedPeriodError.Message.Contains(
                "Accounting period July 2026 (2026-07-01 to 2026-07-31) is closed for MAIN",
                StringComparison.OrdinalIgnoreCase),
            "closed posting period error should name the closed MAIN period.");
    }

    private async Task AssertChartOfAccountsImportPreviewAsync(
        LedgerAccounts accounts,
        CancellationToken cancellationToken)
    {
        var receivableAccount = await RequireLedgerAccountAsync(
            accounts.AccountsReceivableAccountId,
            cancellationToken);
        var receivableControlCode = receivableAccount.Code.Value[..5];
        var receivableControl = await _harness.LedgerAccounts.GetByCodeAsync(
            LedgerAccountCode.Create(receivableControlCode),
            cancellationToken)
            ?? throw new SmokeFailureException("receivable control account was not found for COA import preview.");
        var cashBankControl = await RequireLedgerAccountAsync(
            accounts.CashOrBankControlId,
            cancellationToken);
        var importReceivableSuggestion = await SuggestLedgerAccountCodeAsync(
            "ClientReceivable",
            accounts.AccountsReceivableControlId,
            cancellationToken);
        var importCashBankSuggestion = await SuggestLedgerAccountCodeAsync(
            "CashBank",
            accounts.CashOrBankControlId,
            cancellationToken);
        var missingParentReceivableCode = NextNumericCode(importReceivableSuggestion.SuggestedCode);

        var preview = SmokeAssertions.RequireSuccess(
            await _harness.PreviewChartOfAccountsImportText.HandleAsync(
                new PreviewChartOfAccountsImportTextCommand(
                    null,
                    string.Join(
                        Environment.NewLine,
                        [
                            "Acc Type,Account Code,Parent Code,Account Name,CUR",
                            $"Control,{receivableControl.Code.Value},,{receivableControl.Name},PKR",
                            $"Subsidiary,{receivableAccount.Code.Value},{receivableControl.Code.Value},Imported receivable rename,PKR",
                            $"Subsidiary,{importReceivableSuggestion.SuggestedCode},{receivableControl.Code.Value},Imported client receivable,PKR",
                            $"Control,{cashBankControl.Code.Value},,{cashBankControl.Name},PKR",
                            $"Subsidiary,{importCashBankSuggestion.SuggestedCode},{cashBankControl.Code.Value},Imported bank account,PKR",
                            $"Subsidiary,{missingParentReceivableCode},99999,Missing parent receivable,PKR"
                        ]),
                    "comma"),
                cancellationToken),
            "preview chart of accounts import text");

        SmokeAssertions.Equal("MAIN", preview.CompanyCode, "COA import preview company");
        SmokeAssertions.Equal("Comma", preview.Format, "COA import preview format");
        SmokeAssertions.Equal(6, preview.ParsedLineCount, "COA import preview parsed line count");
        SmokeAssertions.Equal(1, preview.IgnoredLineCount, "COA import preview ignored header count");
        SmokeAssertions.Equal(2, preview.InsertCount, "COA import preview insert count");
        SmokeAssertions.Equal(1, preview.UpdateCount, "COA import preview update count");
        SmokeAssertions.Equal(2, preview.NoChangeCount, "COA import preview no-change count");
        SmokeAssertions.Equal(1, preview.RejectCount, "COA import preview reject count");
        SmokeAssertions.True(!preview.CanImport, "COA import preview should block rejected rows.");

        var noChangeRow = preview.Rows.Single(row => row.Code == receivableControl.Code.Value);
        SmokeAssertions.Equal("NoChange", noChangeRow.Action, "existing control COA preview action");
        SmokeAssertions.Equal(
            receivableControl.Id.Value,
            noChangeRow.ExistingLedgerAccountId ?? Guid.Empty,
            "existing control COA preview id");
        SmokeAssertions.Equal("Control", noChangeRow.ResolvedLevel, "existing control COA preview level");

        var updateRow = preview.Rows.Single(row => row.Code == receivableAccount.Code.Value);
        SmokeAssertions.Equal("Update", updateRow.Action, "existing subsidiary COA preview update action");
        SmokeAssertions.Equal(
            receivableAccount.Id.Value,
            updateRow.ExistingLedgerAccountId ?? Guid.Empty,
            "existing subsidiary COA preview id");

        var childRow = preview.Rows.Single(row => row.Code == importReceivableSuggestion.SuggestedCode);
        SmokeAssertions.Equal("Insert", childRow.Action, "new subsidiary COA preview action");
        SmokeAssertions.Equal(
            receivableControl.Id.Value,
            childRow.ParentAccountId ?? Guid.Empty,
            "new subsidiary COA preview parent");
        SmokeAssertions.Equal("Existing", childRow.ParentSource ?? "", "new subsidiary COA preview parent source");
        SmokeAssertions.Equal("Subsidiary", childRow.ResolvedLevel, "new subsidiary COA preview level");

        var importParentRow = preview.Rows.Single(row => row.Code == cashBankControl.Code.Value);
        SmokeAssertions.Equal("NoChange", importParentRow.Action, "existing cash control COA preview action");
        SmokeAssertions.Equal("Control", importParentRow.ResolvedLevel, "existing cash control COA preview level");

        var importChildRow = preview.Rows.Single(row => row.Code == importCashBankSuggestion.SuggestedCode);
        SmokeAssertions.Equal("Insert", importChildRow.Action, "existing-parent child COA preview action");
        SmokeAssertions.Equal("Existing", importChildRow.ParentSource ?? "", "existing-parent child COA preview parent source");
        SmokeAssertions.Equal("Subsidiary", importChildRow.ResolvedLevel, "existing-parent child COA preview level");

        var missingParentRow = preview.Rows.Single(row => row.Code == missingParentReceivableCode);
        SmokeAssertions.Equal("Reject", missingParentRow.Action, "missing parent COA preview action");
        SmokeAssertions.True(
            missingParentRow.Issues.Any(issue => issue.Code == "MissingParent"),
            "missing parent COA preview should report missing parent.");
    }

    private async Task AssertVoucherAndOpeningBalancePreviewAsync(
        LedgerAccounts accounts,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var manualVoucher = SmokeAssertions.RequireSuccess(
            await _harness.PreviewJournalVoucherNumber.HandleAsync(
                new PreviewJournalVoucherNumberQuery("Manual", businessDate),
                cancellationToken),
            "preview manual voucher number");

        SmokeAssertions.Equal("MJ", manualVoucher.Prefix, "manual voucher prefix");
        SmokeAssertions.Equal(2026, manualVoucher.SequenceYear, "manual voucher year");
        SmokeAssertions.Equal(1, manualVoucher.NextSequence, "manual voucher next sequence");
        SmokeAssertions.Equal(4, manualVoucher.NumberPaddingWidth, "manual voucher padding width");
        SmokeAssertions.Equal("MJ-2026-0001", manualVoucher.Reference, "manual voucher reference");

        var defaultVoucherRules = SmokeAssertions.RequireSuccess(
            await _harness.ListVoucherNumberingRules.HandleAsync(
                new ListVoucherNumberingRulesQuery(),
                cancellationToken),
            "list default voucher numbering rules");

        SmokeAssertions.True(
            defaultVoucherRules.Rules.Any(rule =>
                rule.SourceType == "Manual"
                && rule.Prefix == "MJ"
                && rule.NumberPaddingWidth == 4
                && rule.IsActive
                && !rule.IsConfigured),
            "default manual voucher numbering rule should be listed.");

        var configuredManualRule = SmokeAssertions.RequireSuccess(
            await _harness.ConfigureVoucherNumberingRule.HandleAsync(
                new ConfigureVoucherNumberingRuleCommand(
                    null,
                    "Manual",
                    "JV",
                    5,
                    true),
                cancellationToken),
            "configure manual voucher numbering rule");

        SmokeAssertions.Equal("JV", configuredManualRule.Prefix, "configured manual voucher prefix");
        SmokeAssertions.Equal(5, configuredManualRule.NumberPaddingWidth, "configured manual voucher padding");
        SmokeAssertions.True(configuredManualRule.IsConfigured, "configured manual voucher rule flag");

        var configuredManualVoucher = SmokeAssertions.RequireSuccess(
            await _harness.PreviewJournalVoucherNumber.HandleAsync(
                new PreviewJournalVoucherNumberQuery("Manual", businessDate),
                cancellationToken),
            "preview configured manual voucher number");

        SmokeAssertions.Equal("JV", configuredManualVoucher.Prefix, "configured manual voucher preview prefix");
        SmokeAssertions.Equal(5, configuredManualVoucher.NumberPaddingWidth, "configured manual voucher preview padding");
        SmokeAssertions.Equal("JV-2026-00001", configuredManualVoucher.Reference, "configured manual voucher reference");

        var cashAccount = await RequireLedgerAccountAsync(accounts.CashOrBankAccountId, cancellationToken);
        var retainedEarningsAccount = await RequireLedgerAccountAsync(
            accounts.RetainedEarningsAccountId,
            cancellationToken);
        var profileFromDate = new DateOnly(businessDate.Year, 1, 1);
        var profileToDate = new DateOnly(businessDate.Year, 12, 31);

        var openingPreview = SmokeAssertions.RequireSuccess(
            await _harness.PreviewOpeningBalanceImport.HandleAsync(
                new PreviewOpeningBalanceImportCommand(
                    businessDate,
                    CurrencyCode,
                    null,
                    "Legacy opening balance dry-run",
                    profileFromDate,
                    profileToDate,
                    "Open",
                    true,
                    accounts.RetainedEarningsAccountId,
                    [
                        new PreviewOpeningBalanceImportLineCommand(
                            cashAccount.Code.Value,
                            500m,
                            0m,
                            "Legacy cash opening"),
                        new PreviewOpeningBalanceImportLineCommand(
                            retainedEarningsAccount.Code.Value,
                            0m,
                            500m,
                            "Legacy equity opening")
                    ]),
                cancellationToken),
            "preview opening balance import");

        SmokeAssertions.True(openingPreview.CanPost, "opening balance preview should be postable.");
        SmokeAssertions.Equal("OB-2026-0001", openingPreview.SourceReference, "opening balance preview reference");
        SmokeAssertions.Money(500m, openingPreview.TotalDebit, "opening balance preview debit");
        SmokeAssertions.Money(500m, openingPreview.TotalCredit, "opening balance preview credit");
        SmokeAssertions.Money(0m, openingPreview.Difference, "opening balance preview difference");
        SmokeAssertions.Equal(2, openingPreview.ValidLineCount, "opening balance preview valid lines");
        SmokeAssertions.Equal(0, openingPreview.InvalidLineCount, "opening balance preview invalid lines");

        SmokeAssertions.RequireSuccess(
            await _harness.ConfigureVoucherNumberingRule.HandleAsync(
                new ConfigureVoucherNumberingRuleCommand(
                    null,
                    "OpeningBalance",
                    "OPEN",
                    5,
                    true),
                cancellationToken),
            "configure opening balance voucher numbering rule");

        var configuredOpeningPreview = SmokeAssertions.RequireSuccess(
            await _harness.PreviewOpeningBalanceImport.HandleAsync(
                new PreviewOpeningBalanceImportCommand(
                    businessDate,
                    CurrencyCode,
                    null,
                    "Configured opening balance dry-run",
                    profileFromDate,
                    profileToDate,
                    "Open",
                    true,
                    accounts.RetainedEarningsAccountId,
                    [
                        new PreviewOpeningBalanceImportLineCommand(
                            cashAccount.Code.Value,
                            50m,
                            0m,
                            "Configured cash opening"),
                        new PreviewOpeningBalanceImportLineCommand(
                            retainedEarningsAccount.Code.Value,
                            0m,
                            50m,
                            "Configured equity opening")
                    ]),
                cancellationToken),
            "preview configured opening balance import");

        SmokeAssertions.Equal(
            "OPEN-2026-00001",
            configuredOpeningPreview.SourceReference,
            "configured opening balance preview reference");

        var textPreview = SmokeAssertions.RequireSuccess(
            await _harness.PreviewOpeningBalanceImportText.HandleAsync(
                new PreviewOpeningBalanceImportTextCommand(
                    businessDate,
                    CurrencyCode,
                    null,
                    "Legacy opening balance text dry-run",
                    profileFromDate,
                    profileToDate,
                    "Open",
                    true,
                    accounts.RetainedEarningsAccountId,
                    string.Join(
                        Environment.NewLine,
                        [
                            "accountCode,debit,credit,description",
                            $"{cashAccount.Code.Value},125,0,CSV cash opening",
                            $"{retainedEarningsAccount.Code.Value},0,125,CSV equity opening"
                        ]),
                    "comma"),
                cancellationToken),
            "preview opening balance import text");

        SmokeAssertions.Equal("Comma / Standard", textPreview.Format, "opening balance text format");
        SmokeAssertions.Equal(2, textPreview.ParsedLineCount, "opening balance text parsed line count");
        SmokeAssertions.Equal(1, textPreview.IgnoredLineCount, "opening balance text ignored header count");
        SmokeAssertions.Equal(0, textPreview.ParseIssues.Count, "opening balance text parse issue count");
        SmokeAssertions.True(textPreview.Preview.CanPost, "opening balance text preview should be postable.");
        SmokeAssertions.Money(125m, textPreview.Preview.TotalDebit, "opening balance text debit");
        SmokeAssertions.Money(125m, textPreview.Preview.TotalCredit, "opening balance text credit");

        var blockedTextPreview = SmokeAssertions.RequireSuccess(
            await _harness.PreviewOpeningBalanceImportText.HandleAsync(
                new PreviewOpeningBalanceImportTextCommand(
                    businessDate,
                    CurrencyCode,
                    null,
                    null,
                    profileFromDate,
                    profileToDate,
                    "Open",
                    true,
                    accounts.RetainedEarningsAccountId,
                    string.Join(
                        Environment.NewLine,
                        [
                            "accountCode|debit|credit|description",
                            $"{cashAccount.Code.Value}|not-a-number|0|Bad debit",
                            $"{retainedEarningsAccount.Code.Value}|0|125|CSV equity opening"
                        ]),
                    "pipe"),
                cancellationToken),
            "preview blocked opening balance import text");

        SmokeAssertions.Equal("Pipe / Standard", blockedTextPreview.Format, "blocked opening balance text format");
        SmokeAssertions.Equal(1, blockedTextPreview.ParseIssues.Count, "blocked opening balance text parse issue count");
        SmokeAssertions.True(!blockedTextPreview.Preview.CanPost, "blocked opening balance text preview should not post.");
        SmokeAssertions.True(
            blockedTextPreview.Preview.Blockers.Any(blocker =>
                blocker.Contains("text issue", StringComparison.OrdinalIgnoreCase)),
            "blocked opening balance text preview should report parse blockers.");

        var blockedPreview = SmokeAssertions.RequireSuccess(
            await _harness.PreviewOpeningBalanceImport.HandleAsync(
                new PreviewOpeningBalanceImportCommand(
                    businessDate,
                    CurrencyCode,
                    null,
                    null,
                    profileFromDate,
                    profileToDate,
                    "Open",
                    true,
                    accounts.RetainedEarningsAccountId,
                    [
                        new PreviewOpeningBalanceImportLineCommand(
                            "NO-SUCH-ACCOUNT",
                            25m,
                            0m,
                            "Missing account")
                    ]),
                cancellationToken),
            "preview blocked opening balance import");

        SmokeAssertions.True(!blockedPreview.CanPost, "blocked opening balance preview should not be postable.");
        SmokeAssertions.Equal(1, blockedPreview.InvalidLineCount, "blocked opening balance invalid lines");
        SmokeAssertions.True(
            blockedPreview.Blockers.Any(blocker =>
                blocker.Contains("validation issues", StringComparison.OrdinalIgnoreCase)),
            "blocked opening balance preview should report row validation issues.");

        var futureOpeningDate = new DateOnly(2030, 1, 1);
        var futureProfileFromDate = new DateOnly(futureOpeningDate.Year, 1, 1);
        var futureProfileToDate = new DateOnly(futureOpeningDate.Year, 12, 31);
        var futureOpeningReference = "OB-FUTURE-2030-0001";
        var postedOpening = SmokeAssertions.RequireSuccess(
            await _harness.PostOpeningBalanceImport.HandleAsync(
                new PostOpeningBalanceImportCommand(
                    futureOpeningDate,
                    CurrencyCode,
                    futureOpeningReference,
                    "Future-dated opening balance posting smoke",
                    futureProfileFromDate,
                    futureProfileToDate,
                    "Open",
                    true,
                    accounts.RetainedEarningsAccountId,
                    [
                        new PostOpeningBalanceImportLineCommand(
                            cashAccount.Code.Value,
                            600m,
                            0m,
                            "Opening cash post"),
                        new PostOpeningBalanceImportLineCommand(
                            retainedEarningsAccount.Code.Value,
                            0m,
                            600m,
                            "Opening equity post")
                    ]),
                cancellationToken),
            "post opening balance import");

        SmokeAssertions.Equal("OpeningBalance", postedOpening.SourceType, "posted opening balance source type");
        SmokeAssertions.Equal(futureOpeningReference, postedOpening.SourceReference ?? "", "posted opening balance reference");
        SmokeAssertions.Equal(futureOpeningDate, postedOpening.EntryDate, "posted opening balance date");
        SmokeAssertions.Money(600m, postedOpening.TotalDebit, "posted opening balance debit");
        SmokeAssertions.Money(600m, postedOpening.TotalCredit, "posted opening balance credit");

        SmokeAssertions.RequireFailure(
            await _harness.PostOpeningBalanceImport.HandleAsync(
                new PostOpeningBalanceImportCommand(
                    futureOpeningDate,
                    CurrencyCode,
                    futureOpeningReference,
                    "Duplicate opening balance posting smoke",
                    futureProfileFromDate,
                    futureProfileToDate,
                    "Open",
                    true,
                    accounts.RetainedEarningsAccountId,
                    [
                        new PostOpeningBalanceImportLineCommand(
                            cashAccount.Code.Value,
                            600m,
                            0m,
                            "Duplicate opening cash post"),
                        new PostOpeningBalanceImportLineCommand(
                            retainedEarningsAccount.Code.Value,
                            0m,
                            600m,
                            "Duplicate opening equity post")
                    ]),
                cancellationToken),
            "reject duplicate opening balance import",
            nameof(PostOpeningBalanceImportCommand.SourceReference),
            "already exists");
    }

    private static bool IsInsideRange(string code, AccountCodeRange range)
    {
        return code.All(char.IsDigit)
            && code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0;
    }

    private static string NextNumericCode(string code)
    {
        return (BigInteger.Parse(code) + BigInteger.One)
            .ToString()
            .PadLeft(code.Length, '0');
    }

    private static bool IsCompatibleWithRange(
        LedgerAccount account,
        AccountCodeRange range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        if (account.Status != LedgerAccountStatus.Active
            || account.Type != range.AccountType
            || account.NormalBalance != range.NormalBalance
            || account.IsPostingAccount != range.IsPostingAccount)
        {
            return false;
        }

        var expectedLevel = DetermineExpectedLevel(range, account);

        if (account.Level != expectedLevel)
        {
            return false;
        }

        if (expectedLevel is LedgerAccountLevel.Header
            or LedgerAccountLevel.Total
            or LedgerAccountLevel.Master
            or LedgerAccountLevel.Control)
        {
            return !account.ParentAccountId.HasValue;
        }

        if (expectedLevel != LedgerAccountLevel.Subsidiary)
        {
            return true;
        }

        if (!account.ParentAccountId.HasValue
            || !accountsById.TryGetValue(account.ParentAccountId.Value.Value, out var parentAccount))
        {
            return false;
        }

        return parentAccount.Level == LedgerAccountLevel.Control
            && (string.IsNullOrWhiteSpace(range.ParentCode)
                || string.Equals(parentAccount.Code.Value, range.ParentCode, StringComparison.Ordinal));
    }

    private static LedgerAccountLevel DetermineExpectedLevel(
        AccountCodeRange range,
        LedgerAccount account)
    {
        if (HasRangeIntent(range, "Header"))
        {
            return LedgerAccountLevel.Header;
        }

        if (HasRangeIntent(range, "Total"))
        {
            return LedgerAccountLevel.Total;
        }

        if (HasRangeIntent(range, "Control"))
        {
            return LedgerAccountLevel.Control;
        }

        if (HasRangeIntent(range, "Master"))
        {
            return LedgerAccountLevel.Master;
        }

        if (!string.IsNullOrWhiteSpace(range.ParentCode))
        {
            return LedgerAccountLevel.Subsidiary;
        }

        return account.IsPostingAccount
            ? LedgerAccountLevel.Detail
            : LedgerAccountLevel.Master;
    }

    private static bool HasRangeIntent(AccountCodeRange range, string intent)
    {
        return range.Role.Contains(intent, StringComparison.OrdinalIgnoreCase)
            || range.DisplayName.Contains(intent, StringComparison.OrdinalIgnoreCase);
    }

    private static CreateLedgerAccountResult ToCreateLedgerAccountResult(LedgerAccount account)
    {
        return new CreateLedgerAccountResult(
            account.Id.Value,
            account.Code.Value,
            account.Name,
            account.Type.ToString(),
            account.NormalBalance.ToString(),
            account.Level.ToString(),
            account.ParentAccountId?.Value,
            account.IsPostingAccount,
            account.Status.ToString());
    }

    private async Task<CreateClientResult> CreateClientAsync(CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.CreateClient.HandleAsync(
                new CreateClientCommand(Code("CL"), $"Smoke Test Client {_runId} Pvt Ltd", "Smoke Client"),
                cancellationToken),
            "create client");
    }

    private async Task ConfigureAccountingProfileAsync(
        Guid clientId,
        Guid accountsReceivableAccountId,
        CancellationToken cancellationToken)
    {
        _ = SmokeAssertions.RequireSuccess(
            await _harness.ConfigureClientAccountingProfile.HandleAsync(
                new ConfigureClientAccountingProfileCommand(
                    clientId,
                    accountsReceivableAccountId,
                    CurrencyCode,
                    $"cloud-smoke-client-{_runId}"),
                cancellationToken),
            "configure accounting profile");
    }

    private async Task<CreateClientContractResult> CreateContractAsync(
        Guid clientId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.CreateClientContract.HandleAsync(
                new CreateClientContractCommand(
                    clientId,
                    Document("CON-001"),
                    businessDate,
                    businessDate.AddYears(1),
                    100m,
                    CurrencyCode,
                    "Monthly",
                    1,
                    25,
                    3,
                    "Accounting smoke",
                    "Initial commercial terms approved for the accounting proof.",
                    [new CreateClientContractModuleCommand("BILLING", true)],
                    AllowedNamedUsers: 40,
                    AllowedConcurrentUsers: 12,
                    FeatureLimits:
                    [
                        new CreateClientContractFeatureLimitCommand(
                            "BILLING",
                            "MONTHLY_INVOICES",
                            2500,
                            "COUNT")
                    ]),
                cancellationToken),
            "create client contract");
    }

    private async Task<CreateChargeCodeResult> CreateBillingSetupAsync(
        Guid clientId,
        Guid contractId,
        Guid revenueAccountId,
        Guid taxAccountId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var chargeCode = SmokeAssertions.RequireSuccess(
            await _harness.CreateChargeCode.HandleAsync(
                new CreateChargeCodeCommand(
                    Code("SUB"),
                    "Control Desk subscription",
                    "Monthly SafarSuite Control Desk subscription",
                    100m,
                    CurrencyCode,
                    revenueAccountId,
                    taxAccountId),
                cancellationToken),
            "create charge code");

        _ = SmokeAssertions.RequireSuccess(
            await _harness.CreateClientChargeRule.HandleAsync(
                new CreateClientChargeRuleCommand(
                    clientId,
                    contractId,
                    chargeCode.ChargeCodeId,
                    null,
                    null,
                    100m,
                    CurrencyCode,
                    1m,
                    10m,
                    "Monthly",
                    1,
                    businessDate,
                    businessDate.AddYears(1)),
                cancellationToken),
            "create client charge rule");

        return chargeCode;
    }

    private async Task AssertContractRevisioningAsync(
        Guid clientId,
        CreateClientContractResult initialContract,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var persistedInitial = await _harness.Contracts.GetByIdAsync(
            ContractId.Create(initialContract.ContractId),
            cancellationToken);
        SmokeAssertions.True(persistedInitial is not null, "initial contract revision should be persisted.");

        var immutable = false;

        try
        {
            persistedInitial!.SetDeviceAllowance(DeviceAllowance.Create(99));
        }
        catch (InvalidOperationException)
        {
            immutable = true;
        }

        SmokeAssertions.True(immutable, "activated commercial terms should be immutable.");

        var replacement = SmokeAssertions.RequireSuccess(
            await _harness.ReplaceActiveClientContract.HandleAsync(
                new ReplaceActiveClientContractCommand(
                    clientId,
                    Document("CON-002"),
                    businessDate.AddMonths(6),
                    businessDate.AddYears(1).AddMonths(6),
                    125m,
                    CurrencyCode,
                    "Monthly",
                    1,
                    30,
                    4,
                    "Accounting smoke",
                    "Commercial limits revised for the contract lineage proof.",
                    [new ReplaceActiveClientContractModuleCommand("BILLING", true)]),
                cancellationToken),
            "replace active client contract");

        SmokeAssertions.Equal(2L, replacement.ActiveContract.RevisionNumber, "replacement contract revision");
        SmokeAssertions.Equal(
            initialContract.ContractId,
            replacement.ActiveContract.SupersedesContractId ?? Guid.Empty,
            "replacement contract predecessor");
        SmokeAssertions.Equal(
            "Suspended",
            replacement.SuspendedContract?.Status ?? "",
            "superseded contract status");

        var history = await _harness.Contracts.ListForClientAsync(ClientId.Create(clientId), cancellationToken);
        SmokeAssertions.Equal(2, history.Count, "contract revision history count");
        SmokeAssertions.Equal(2L, history.First().RevisionNumber, "latest contract history revision");
    }

    private async Task<GenerateInvoiceDraftResult> DraftInvoiceAsync(
        Guid clientId,
        Guid contractId,
        string invoiceNumber,
        DateOnly issueDate,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.GenerateInvoiceDraft.HandleAsync(
                new GenerateInvoiceDraftCommand(
                    clientId,
                    contractId,
                    invoiceNumber,
                    issueDate,
                    issueDate.AddDays(14),
                    issueDate,
                    CurrencyCode),
                cancellationToken),
            $"draft invoice {invoiceNumber}");
    }

    private async Task<IssueInvoiceResult> IssueInvoiceAsync(
        Guid invoiceId,
        Guid accountsReceivableAccountId,
        DateOnly postingDate,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.IssueInvoice.HandleAsync(
                new IssueInvoiceCommand(invoiceId, accountsReceivableAccountId, postingDate),
                cancellationToken),
            "issue invoice");
    }

    private async Task<RecordInvoicePaymentResult> RecordPaymentAsync(
        Guid invoiceId,
        Guid cashOrBankAccountId,
        Guid accountsReceivableAccountId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.RecordInvoicePayment.HandleAsync(
                new RecordInvoicePaymentCommand(
                    invoiceId,
                    "ManualCash",
                    Document("PAY-001"),
                    110m,
                    CurrencyCode,
                    businessDate,
                    cashOrBankAccountId,
                    accountsReceivableAccountId,
                    businessDate),
                cancellationToken),
            "record invoice payment");
    }

    private async Task<IssueCreditNoteResult> IssueCreditNoteAsync(
        Guid invoiceId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.IssueCreditNote.HandleAsync(
                new IssueCreditNoteCommand(
                    invoiceId,
                    Document("CN-001"),
                    businessDate,
                    "Smoke full correction"),
                cancellationToken),
            "issue credit note");
    }

    private async Task<IssueClientRefundResult> IssueRefundAsync(
        Guid clientId,
        Guid cashOrBankAccountId,
        Guid accountsReceivableAccountId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.IssueClientRefund.HandleAsync(
                new IssueClientRefundCommand(
                    clientId,
                    "ManualCash",
                    Document("REF-001"),
                    40m,
                    CurrencyCode,
                    businessDate,
                    cashOrBankAccountId,
                    accountsReceivableAccountId,
                    businessDate,
                    "Smoke partial refund"),
                cancellationToken),
            "issue client refund");
    }

    private async Task<ApplyClientCreditResult> ApplyCreditAsync(
        Guid clientId,
        Guid invoiceId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        return SmokeAssertions.RequireSuccess(
            await _harness.ApplyClientCredit.HandleAsync(
                new ApplyClientCreditCommand(
                    clientId,
                    invoiceId,
                    Document("SET-001"),
                    60m,
                    CurrencyCode,
                    businessDate,
                    "Smoke client credit settlement"),
                cancellationToken),
            "apply client credit");
    }

    private async Task AssertFinalStatementAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var statement = SmokeAssertions.RequireSuccess(
            await _harness.GetClientFinancialSummary.HandleAsync(
                new GetClientFinancialSummaryQuery(clientId),
                cancellationToken),
            "get client financial summary");

        var summary = statement.CurrencySummaries.Single(summary => summary.CurrencyCode == CurrencyCode);
        SmokeAssertions.Money(220m, summary.TotalInvoiced, "statement total invoiced");
        SmokeAssertions.Money(110m, summary.TotalPaid, "statement total paid");
        SmokeAssertions.Money(10m, summary.AvailableCredit, "statement available credit");
        SmokeAssertions.Money(40m, summary.BalanceDue, "statement balance due");
        SmokeAssertions.Equal(2L, summary.InvoiceCount, "statement invoice count");
        SmokeAssertions.Equal(1L, summary.OpenInvoiceCount, "statement open invoice count");

        var activity = SmokeAssertions.RequireSuccess(
            await _harness.ListClientFinancialActivity.HandleAsync(
                new ListClientFinancialActivityQuery(clientId, Take: 100),
                cancellationToken),
            "list client financial activity");

        SmokeAssertions.True(
            activity.Lines.Any(line => line.DocumentType == "Credit note" && line.Credit == 110m),
            "statement should include the credit note line.");
        SmokeAssertions.True(
            activity.Lines.Any(line => line.DocumentType == "Client refund" && line.Debit == 40m),
            "statement should include the client refund line.");
        SmokeAssertions.True(
            activity.Lines.Any(line =>
                line.DocumentType == "Applied credit"
                && line.Debit == 60m
                && line.Credit == 60m
                && line.JournalEntryId is null),
            "statement should include a zero-net applied credit line without a journal.");

        var journalPage = SmokeAssertions.RequireSuccess(
            await _harness.ListClientJournalPostings.HandleAsync(
                new ListClientJournalPostingsQuery(clientId, Take: 100),
                cancellationToken),
            "list client journal postings");
        SmokeAssertions.Equal(5L, journalPage.FilteredCount, "statement journal posting count");
        SmokeAssertions.True(
            journalPage.JournalPostings.All(posting => posting.LineCount >= 2),
            "statement journal summaries should expose bounded line counts.");

        var journalRegisterPage = SmokeAssertions.RequireSuccess(
            await _harness.ListJournalEntries.HandleAsync(
                new ListJournalEntriesQuery(Take: 2),
                cancellationToken),
            "list first global journal page");
        SmokeAssertions.True(journalRegisterPage.FilteredCount >= 5, "global journal count");
        SmokeAssertions.True(journalRegisterPage.HasMore, "global journal register should expose continuation.");
        SmokeAssertions.True(
            !string.IsNullOrWhiteSpace(journalRegisterPage.NextCursor),
            "global journal continuation cursor");
        var nextJournalRegisterPage = SmokeAssertions.RequireSuccess(
            await _harness.ListJournalEntries.HandleAsync(
                new ListJournalEntriesQuery(Take: 2, Cursor: journalRegisterPage.NextCursor),
                cancellationToken),
            "list second global journal page");
        SmokeAssertions.True(
            !journalRegisterPage.Entries.Select(entry => entry.JournalEntryId)
                .Intersect(nextJournalRegisterPage.Entries.Select(entry => entry.JournalEntryId))
                .Any(),
            "global journal pages should not overlap.");
        var staleJournalCursor = await _harness.ListJournalEntries.HandleAsync(
            new ListJournalEntriesQuery(
                SourceType: JournalSourceType.BillingInvoice.ToString(),
                Take: 2,
                Cursor: journalRegisterPage.NextCursor),
            cancellationToken);
        SmokeAssertions.True(staleJournalCursor.IsFailure, "journal cursor should be filter-bound.");

        var invoicePage = SmokeAssertions.RequireSuccess(
            await _harness.ListClientInvoices.HandleAsync(
                new ListClientInvoicesQuery(clientId, Take: 1),
                cancellationToken),
            "list first client invoice page");
        SmokeAssertions.Equal(2L, invoicePage.FilteredCount, "invoice register count");
        SmokeAssertions.True(invoicePage.HasMore, "invoice register should expose continuation.");
        var nextInvoicePage = SmokeAssertions.RequireSuccess(
            await _harness.ListClientInvoices.HandleAsync(
                new ListClientInvoicesQuery(clientId, Take: 1, Cursor: invoicePage.NextCursor),
                cancellationToken),
            "list second client invoice page");
        SmokeAssertions.True(
            invoicePage.Invoices.Single().InvoiceId != nextInvoicePage.Invoices.Single().InvoiceId,
            "invoice pages should not overlap.");
        SmokeAssertions.True(!nextInvoicePage.HasMore, "second invoice page should terminate.");
        var staleInvoiceCursor = await _harness.ListClientInvoices.HandleAsync(
            new ListClientInvoicesQuery(
                clientId,
                Search: "different-query",
                Take: 1,
                Cursor: invoicePage.NextCursor),
            cancellationToken);
        SmokeAssertions.True(staleInvoiceCursor.IsFailure, "invoice cursor should be query-bound.");

        var paymentPage = SmokeAssertions.RequireSuccess(
            await _harness.ListClientPayments.HandleAsync(
                new ListClientPaymentsQuery(clientId, Take: 1),
                cancellationToken),
            "list first client payment page");
        SmokeAssertions.Equal(1L, paymentPage.FilteredCount, "payment register count");
        SmokeAssertions.True(!paymentPage.HasMore, "single-payment register should terminate.");
    }

    private async Task AssertCrossClientReportsAsync(
        Guid clientId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var asOfDate = _harness.Clock.Today;
        var aging = SmokeAssertions.RequireSuccess(
            await _harness.GetAccountsReceivableAging.HandleAsync(
                new GetAccountsReceivableAgingQuery(asOfDate, CurrencyCode),
                cancellationToken),
            "get cross-client receivable aging");
        var clientAging = aging.Clients.Single(client => client.ClientId == clientId);
        var bucketTotal = clientAging.CurrentAmount
            + clientAging.Days1To30Amount
            + clientAging.Days31To60Amount
            + clientAging.Days61To90Amount
            + clientAging.DaysOver90Amount;

        SmokeAssertions.Money(50m, clientAging.TotalOutstanding, "client aging outstanding total");
        SmokeAssertions.Money(clientAging.TotalOutstanding, bucketTotal, "client aging bucket reconciliation");
        SmokeAssertions.Equal(1L, clientAging.InvoiceCount, "client aging invoice count");
        var agingCurrency = aging.Currencies.Single(currency => currency.CurrencyCode == CurrencyCode);
        SmokeAssertions.True(
            agingCurrency.TotalOutstanding >= clientAging.TotalOutstanding,
            "currency aging total should include the smoke client's outstanding amount.");

        var historicalAging = await _harness.GetAccountsReceivableAging.HandleAsync(
            new GetAccountsReceivableAgingQuery(asOfDate.AddDays(-1), CurrencyCode),
            cancellationToken);
        SmokeAssertions.RequireFailure(
            historicalAging,
            "reject historical aging from mutable operational invoice balances");

        var outstanding = SmokeAssertions.RequireSuccess(
            await _harness.ListOutstandingInvoices.HandleAsync(
                new ListOutstandingInvoicesQuery(
                    clientId,
                    businessDate,
                    asOfDate,
                    null,
                    null,
                    "All",
                    CurrencyCode,
                    100,
                    null),
                cancellationToken),
            "list cross-client outstanding invoices");
        var outstandingInvoice = outstanding.Invoices.Single();
        SmokeAssertions.Equal(clientId, outstandingInvoice.ClientId, "outstanding invoice client");
        SmokeAssertions.Equal("PartiallyPaid", outstandingInvoice.Status, "outstanding invoice status");
        SmokeAssertions.Money(110m, outstandingInvoice.TotalAmount, "outstanding invoice total");
        SmokeAssertions.Money(60m, outstandingInvoice.AmountPaid, "outstanding invoice settled amount");
        SmokeAssertions.Money(50m, outstandingInvoice.BalanceDue, "outstanding invoice residual");
        SmokeAssertions.Equal(1L, outstanding.FilteredCount, "outstanding invoice filtered count");

        var invalidOutstandingCursor = await _harness.ListOutstandingInvoices.HandleAsync(
            new ListOutstandingInvoicesQuery(
                clientId,
                businessDate,
                asOfDate,
                null,
                null,
                "All",
                CurrencyCode,
                100,
                "not-a-report-cursor"),
            cancellationToken);
        SmokeAssertions.RequireFailure(invalidOutstandingCursor, "reject malformed outstanding invoice cursor");

        var receipts = SmokeAssertions.RequireSuccess(
            await _harness.ListPaymentReceiptsReport.HandleAsync(
                new ListPaymentReceiptsReportQuery(
                    clientId,
                    businessDate,
                    asOfDate,
                    null,
                    null,
                    CurrencyCode,
                    100,
                    null),
                cancellationToken),
            "list cross-client payment receipts");
        var receipt = receipts.Payments.Single();
        SmokeAssertions.Equal(clientId, receipt.ClientId, "receipt client");
        SmokeAssertions.Equal("Approved", receipt.Status, "receipt status");
        SmokeAssertions.Equal("ManualCash", receipt.Method, "receipt method");
        SmokeAssertions.Money(110m, receipt.Amount, "receipt amount");
        SmokeAssertions.True(receipt.JournalEntryId.HasValue, "receipt should link to its journal entry.");
        SmokeAssertions.Equal(1L, receipts.FilteredCount, "receipt filtered count");
    }

    private async Task AssertGeneralLedgerReportsAsync(
        LedgerAccounts accounts,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var reportDate = businessDate.AddDays(1);
        var trialBalance = SmokeAssertions.RequireSuccess(
            await _harness.GetTrialBalance.HandleAsync(
                new GetTrialBalanceQuery(reportDate, reportDate, CurrencyCode),
                cancellationToken),
            "get trial balance");

        SmokeAssertions.Equal(reportDate, trialBalance.FromDate ?? DateOnly.MinValue, "trial balance from date");
        SmokeAssertions.Equal(reportDate, trialBalance.AsOfDate, "trial balance as-of date");
        SmokeAssertions.Money(110m, trialBalance.TotalDebit, "trial balance closing debit total");
        SmokeAssertions.Money(110m, trialBalance.TotalCredit, "trial balance closing credit total");
        SmokeAssertions.Money(110m, trialBalance.TotalPeriodDebit, "trial balance period debit total");
        SmokeAssertions.Money(110m, trialBalance.TotalPeriodCredit, "trial balance period credit total");
        SmokeAssertions.Money(0m, trialBalance.Difference, "trial balance difference");

        var receivableLine = FindTrialBalanceLine(
            trialBalance,
            accounts.AccountsReceivableAccountId,
            "trial balance receivable account");
        SmokeAssertions.Money(-70m, receivableLine.OpeningBalance, "receivable opening balance");
        SmokeAssertions.Money(110m, receivableLine.PeriodDebit, "receivable period debit");
        SmokeAssertions.Money(0m, receivableLine.PeriodCredit, "receivable period credit");
        SmokeAssertions.Money(40m, receivableLine.DebitBalance, "receivable closing debit balance");
        SmokeAssertions.Money(0m, receivableLine.CreditBalance, "receivable closing credit balance");
        SmokeAssertions.Money(40m, receivableLine.NetBalance, "receivable net balance");
        SmokeAssertions.Equal(1, receivableLine.ActivityCount, "receivable period activity count");

        var receivableActivity = SmokeAssertions.RequireSuccess(
            await _harness.GetLedgerAccountActivity.HandleAsync(
                new GetLedgerAccountActivityQuery(
                    accounts.AccountsReceivableAccountId,
                    reportDate,
                    reportDate,
                    CurrencyCode),
                cancellationToken),
            "get receivable account activity");

        SmokeAssertions.Money(-70m, receivableActivity.OpeningBalance, "receivable activity opening balance");
        SmokeAssertions.Money(110m, receivableActivity.PeriodDebit, "receivable activity period debit");
        SmokeAssertions.Money(0m, receivableActivity.PeriodCredit, "receivable activity period credit");
        SmokeAssertions.Money(40m, receivableActivity.EndingBalance, "receivable activity ending balance");
        SmokeAssertions.Equal(1, receivableActivity.Lines.Count, "receivable activity line count");

        var activityLine = receivableActivity.Lines.Single();
        SmokeAssertions.Equal(reportDate, activityLine.EntryDate, "receivable activity entry date");
        SmokeAssertions.Equal("BillingInvoice", activityLine.SourceType, "receivable activity source type");
        SmokeAssertions.Money(110m, activityLine.Debit, "receivable activity line debit");
        SmokeAssertions.Money(0m, activityLine.Credit, "receivable activity line credit");
        SmokeAssertions.Money(40m, activityLine.RunningBalance, "receivable activity line running balance");

        var profitAndLoss = SmokeAssertions.RequireSuccess(
            await _harness.GetProfitAndLossStatement.HandleAsync(
                new GetProfitAndLossStatementQuery(reportDate, reportDate, CurrencyCode),
                cancellationToken),
            "get profit and loss statement");

        SmokeAssertions.Equal(reportDate, profitAndLoss.FromDate ?? DateOnly.MinValue, "profit and loss from date");
        SmokeAssertions.Equal(reportDate, profitAndLoss.ToDate, "profit and loss to date");
        SmokeAssertions.Money(100m, profitAndLoss.TotalRevenue, "profit and loss total revenue");
        SmokeAssertions.Money(0m, profitAndLoss.TotalExpense, "profit and loss total expense");
        SmokeAssertions.Money(100m, profitAndLoss.NetIncome, "profit and loss net income");

        var revenueSection = FindProfitAndLossSection(
            profitAndLoss,
            "Revenue",
            "profit and loss revenue section");
        var revenueLine = revenueSection.Lines.SingleOrDefault(line => line.LedgerAccountId == accounts.RevenueAccountId)
            ?? throw new SmokeFailureException("profit and loss revenue account was not included.");
        SmokeAssertions.Money(100m, revenueLine.Amount, "profit and loss revenue line amount");
        SmokeAssertions.Money(0m, revenueLine.Debit, "profit and loss revenue line debit");
        SmokeAssertions.Money(100m, revenueLine.Credit, "profit and loss revenue line credit");

        var revenueSummary = SmokeAssertions.RequireSuccess(
            await _harness.GetRevenueSummary.HandleAsync(
                new GetRevenueSummaryQuery(reportDate, reportDate, "monthly", CurrencyCode),
                cancellationToken),
            "get monthly revenue summary");

        SmokeAssertions.Equal("Monthly", revenueSummary.Period, "revenue summary period");
        SmokeAssertions.Money(100m, revenueSummary.TotalRevenue, "revenue summary total");
        var revenuePeriod = revenueSummary.Periods.Single();
        SmokeAssertions.Equal(reportDate, revenuePeriod.PeriodStart, "revenue summary period start");
        SmokeAssertions.Equal(reportDate, revenuePeriod.PeriodEnd, "revenue summary period end");
        SmokeAssertions.Money(0m, revenuePeriod.Debit, "revenue summary debit");
        SmokeAssertions.Money(100m, revenuePeriod.Credit, "revenue summary credit");
        SmokeAssertions.Money(100m, revenuePeriod.Revenue, "revenue summary net revenue");
        SmokeAssertions.Equal(1, revenuePeriod.ActivityCount, "revenue summary activity count");

        SmokeAssertions.RequireFailure(
            await _harness.GetRevenueSummary.HandleAsync(
                new GetRevenueSummaryQuery(DateOnly.MinValue, reportDate, "monthly", CurrencyCode),
                cancellationToken),
            "reject oversized revenue summary window",
            nameof(GetRevenueSummaryQuery.FromDate),
            "10 years");
        SmokeAssertions.RequireFailure(
            await _harness.GetRevenueSummary.HandleAsync(
                new GetRevenueSummaryQuery(reportDate, DateOnly.MaxValue, "monthly", CurrencyCode),
                cancellationToken),
            "reject future revenue summary date",
            nameof(GetRevenueSummaryQuery.ToDate),
            "after today");
        SmokeAssertions.RequireFailure(
            await _harness.GetRevenueSummary.HandleAsync(
                new GetRevenueSummaryQuery(reportDate, reportDate, "monthly", "@@@"),
                cancellationToken),
            "reject invalid revenue summary currency",
            nameof(GetRevenueSummaryQuery.CurrencyCode),
            "ASCII letters");

        var balanceSheet = SmokeAssertions.RequireSuccess(
            await _harness.GetBalanceSheet.HandleAsync(
                new GetBalanceSheetQuery(reportDate, CurrencyCode),
                cancellationToken),
            "get balance sheet");

        SmokeAssertions.Equal(reportDate, balanceSheet.AsOfDate, "balance sheet as-of date");
        SmokeAssertions.Money(110m, balanceSheet.TotalAssets, "balance sheet total assets");
        SmokeAssertions.Money(10m, balanceSheet.TotalLiabilities, "balance sheet total liabilities");
        SmokeAssertions.Money(100m, balanceSheet.TotalEquity, "balance sheet total equity");
        SmokeAssertions.Money(110m, balanceSheet.TotalLiabilitiesAndEquity, "balance sheet liabilities and equity");
        SmokeAssertions.Money(0m, balanceSheet.Difference, "balance sheet difference");

        var assetSection = FindBalanceSheetSection(
            balanceSheet,
            "Asset",
            "balance sheet asset section");
        var receivableBalanceSheetLine = assetSection.Lines.SingleOrDefault(
                line => line.LedgerAccountId == accounts.AccountsReceivableAccountId)
            ?? throw new SmokeFailureException("balance sheet receivable account was not included.");
        var cashBalanceSheetLine = assetSection.Lines.SingleOrDefault(
                line => line.LedgerAccountId == accounts.CashOrBankAccountId)
            ?? throw new SmokeFailureException("balance sheet cash account was not included.");
        SmokeAssertions.Money(40m, receivableBalanceSheetLine.Amount, "balance sheet receivable amount");
        SmokeAssertions.Money(70m, cashBalanceSheetLine.Amount, "balance sheet cash amount");

        var liabilitySection = FindBalanceSheetSection(
            balanceSheet,
            "Liability",
            "balance sheet liability section");
        var taxBalanceSheetLine = liabilitySection.Lines.SingleOrDefault(
                line => line.LedgerAccountId == accounts.TaxAccountId)
            ?? throw new SmokeFailureException("balance sheet tax payable account was not included.");
        SmokeAssertions.Money(10m, taxBalanceSheetLine.Amount, "balance sheet tax payable amount");

        var equitySection = FindBalanceSheetSection(
            balanceSheet,
            "Equity",
            "balance sheet equity section");
        var currentEarningsLine = equitySection.Lines.SingleOrDefault(line => line.IsSystemLine)
            ?? throw new SmokeFailureException("balance sheet current earnings line was not included.");
        if (currentEarningsLine.LedgerAccountId is not null)
        {
            throw new SmokeFailureException("balance sheet current earnings line should not have a ledger id.");
        }

        SmokeAssertions.Equal("Current earnings", currentEarningsLine.Name, "balance sheet current earnings name");
        SmokeAssertions.Money(100m, currentEarningsLine.Amount, "balance sheet current earnings amount");
    }

    private async Task AssertPeriodCloseBalanceSheetHandoffAsync(
        LedgerAccounts accounts,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var periodStart = businessDate;
        var periodEnd = businessDate.AddDays(10);
        var period = SmokeAssertions.RequireSuccess(
            await _harness.CreateAccountingPeriod.HandleAsync(
                new CreateAccountingPeriodCommand(
                    AccountingSetupDefaults.DefaultCompanyCode,
                    $"Smoke July {RunId}",
                    periodStart,
                    periodEnd),
                cancellationToken),
            "create smoke accounting period");

        var controls = SmokeAssertions.RequireSuccess(
            await _harness.ConfigureDefaultAccountingControlSettings.HandleAsync(
                new ConfigureDefaultAccountingControlSettingsCommand(
                    AccountingSetupDefaults.DefaultCompanyCode),
                cancellationToken),
            "configure default accounting controls for close");

        SmokeAssertions.True(controls.IsConfigured, "accounting controls should be configured for close.");
        SmokeAssertions.Equal(
            accounts.RetainedEarningsAccountId,
            controls.RetainedEarningsAccountId ?? Guid.Empty,
            "default retained earnings control account");
        SmokeAssertions.Equal(
            accounts.IncomeSummaryAccountId,
            controls.IncomeSummaryAccountId ?? Guid.Empty,
            "default income summary control account");
        SmokeAssertions.Equal(
            accounts.RoundingAdjustmentAccountId,
            controls.RoundingAccountId ?? Guid.Empty,
            "default rounding control account");

        var closePreview = SmokeAssertions.RequireSuccess(
            await _harness.GetAccountingPeriodCloseJournalPreview.HandleAsync(
                new GetAccountingPeriodCloseJournalPreviewQuery(period.AccountingPeriodId),
                cancellationToken),
            "get accounting period close preview");

        SmokeAssertions.True(closePreview.CanGenerate, "close preview should be generatable.");
        SmokeAssertions.Money(100m, closePreview.NetIncome, "close preview net income");
        SmokeAssertions.Money(200m, closePreview.TotalDebit, "close preview total debit");
        SmokeAssertions.Money(200m, closePreview.TotalCredit, "close preview total credit");
        SmokeAssertions.Equal(2, closePreview.Entries.Count, "close preview entry count");

        var closedPeriod = SmokeAssertions.RequireSuccess(
            await _harness.CloseAccountingPeriod.HandleAsync(
                new CloseAccountingPeriodCommand(period.AccountingPeriodId, "accounting-smoke"),
                cancellationToken),
            "close smoke accounting period");

        SmokeAssertions.Equal("Closed", closedPeriod.Status, "closed period status");
        SmokeAssertions.Equal(2, closedPeriod.CloseArtifact?.CloseJournalEntries.Count ?? 0, "close artifact journal count");
        await AssertCloseArtifactJournalReadbackAsync(
            closedPeriod.CloseArtifact,
            closePreview,
            cancellationToken);

        var postCloseProfitAndLoss = SmokeAssertions.RequireSuccess(
            await _harness.GetProfitAndLossStatement.HandleAsync(
                new GetProfitAndLossStatementQuery(periodStart, periodEnd, CurrencyCode),
                cancellationToken),
            "get post-close profit and loss");

        SmokeAssertions.Money(0m, postCloseProfitAndLoss.TotalRevenue, "post-close profit and loss revenue");
        SmokeAssertions.Money(0m, postCloseProfitAndLoss.TotalExpense, "post-close profit and loss expense");
        SmokeAssertions.Money(0m, postCloseProfitAndLoss.NetIncome, "post-close profit and loss net income");

        var postCloseRevenueSummary = SmokeAssertions.RequireSuccess(
            await _harness.GetRevenueSummary.HandleAsync(
                new GetRevenueSummaryQuery(periodStart, periodEnd, "monthly", CurrencyCode),
                cancellationToken),
            "get post-close revenue summary");
        SmokeAssertions.Money(
            100m,
            postCloseRevenueSummary.TotalRevenue,
            "revenue summary excludes period-close transfers");

        var postCloseBalanceSheet = SmokeAssertions.RequireSuccess(
            await _harness.GetBalanceSheet.HandleAsync(
                new GetBalanceSheetQuery(periodEnd, CurrencyCode),
                cancellationToken),
            "get post-close balance sheet");

        SmokeAssertions.Money(110m, postCloseBalanceSheet.TotalAssets, "post-close balance sheet total assets");
        SmokeAssertions.Money(10m, postCloseBalanceSheet.TotalLiabilities, "post-close balance sheet total liabilities");
        SmokeAssertions.Money(100m, postCloseBalanceSheet.TotalEquity, "post-close balance sheet total equity");
        SmokeAssertions.Money(110m, postCloseBalanceSheet.TotalLiabilitiesAndEquity, "post-close balance sheet liabilities and equity");
        SmokeAssertions.Money(0m, postCloseBalanceSheet.Difference, "post-close balance sheet difference");

        var equitySection = FindBalanceSheetSection(
            postCloseBalanceSheet,
            "Equity",
            "post-close balance sheet equity section");
        var retainedEarningsLine = equitySection.Lines.SingleOrDefault(
                line => line.LedgerAccountId == accounts.RetainedEarningsAccountId)
            ?? throw new SmokeFailureException("post-close retained earnings account was not included.");

        SmokeAssertions.Money(100m, retainedEarningsLine.Amount, "post-close retained earnings amount");

        if (equitySection.Lines.Any(line => line.IsSystemLine))
        {
            throw new SmokeFailureException("post-close balance sheet should not include current earnings.");
        }
    }

    private async Task AssertCloseArtifactJournalReadbackAsync(
        AccountingPeriodCloseArtifactResult? artifact,
        GetAccountingPeriodCloseJournalPreviewResult closePreview,
        CancellationToken cancellationToken)
    {
        if (artifact is null)
        {
            throw new SmokeFailureException("closed period should include a close artifact.");
        }

        SmokeAssertions.Equal("accounting-smoke", artifact.GeneratedBy, "close artifact generated by");
        SmokeAssertions.Equal(0, artifact.BlockedCheckCount, "close artifact blocked check count");
        SmokeAssertions.Equal(1, artifact.CurrencyCount, "close artifact currency count");
        SmokeAssertions.Equal(2, artifact.CloseJournalEntries.Count, "close artifact journal readback count");

        var previewEntriesByReference = closePreview.Entries
            .Where(entry => entry.Lines.Count > 0)
            .ToDictionary(entry => entry.SourceReference, StringComparer.OrdinalIgnoreCase);

        foreach (var artifactJournal in artifact.CloseJournalEntries)
        {
            var postedJournal = await _harness.JournalEntries.GetByIdAsync(
                    JournalEntryId.Create(artifactJournal.JournalEntryId),
                    cancellationToken)
                ?? throw new SmokeFailureException(
                    $"close artifact journal {artifactJournal.JournalEntryId} could not be opened.");

            SmokeAssertions.Equal(
                JournalSourceType.PeriodClose.ToString(),
                postedJournal.SourceType.ToString(),
                "close artifact journal source type");
            SmokeAssertions.Equal(
                JournalEntryStatus.Posted.ToString(),
                postedJournal.Status.ToString(),
                "close artifact journal status");
            SmokeAssertions.Equal(
                artifactJournal.SourceReference,
                postedJournal.SourceReference ?? "",
                "close artifact journal source reference");
            SmokeAssertions.Equal(
                artifactJournal.Memo,
                postedJournal.Memo ?? "",
                "close artifact journal memo");
            SmokeAssertions.Equal(artifactJournal.EntryDate, postedJournal.EntryDate, "close artifact journal entry date");
            SmokeAssertions.Equal(artifactJournal.CurrencyCode, postedJournal.CurrencyCode, "close artifact journal currency");
            SmokeAssertions.Money(artifactJournal.TotalDebit, postedJournal.TotalDebit.Amount, "close artifact journal debit");
            SmokeAssertions.Money(artifactJournal.TotalCredit, postedJournal.TotalCredit.Amount, "close artifact journal credit");
            SmokeAssertions.Money(postedJournal.TotalDebit.Amount, postedJournal.TotalCredit.Amount, "close artifact posted journal balance");

            if (!previewEntriesByReference.TryGetValue(artifactJournal.SourceReference, out var previewEntry))
            {
                throw new SmokeFailureException(
                    $"close artifact journal {artifactJournal.SourceReference} was not present in the preview.");
            }

            SmokeAssertions.Equal(
                previewEntry.Lines.Count,
                postedJournal.Lines.Count,
                "close artifact journal line count");
            SmokeAssertions.Money(previewEntry.TotalDebit, artifactJournal.TotalDebit, "close artifact preview debit");
            SmokeAssertions.Money(previewEntry.TotalCredit, artifactJournal.TotalCredit, "close artifact preview credit");
        }
    }

    private async Task AssertSourceDocumentReadbackAsync(
        GenerateInvoiceDraftResult invoice,
        IssueInvoiceResult issuedInvoice,
        RecordInvoicePaymentResult payment,
        IssueCreditNoteResult creditNote,
        IssueClientRefundResult refund,
        CancellationToken cancellationToken)
    {
        var invoiceSource = await ResolveSourceDocumentAsync(
            issuedInvoice.JournalEntryId,
            "Invoice",
            cancellationToken);
        SmokeAssertions.Equal(invoice.InvoiceId, invoiceSource.DocumentId ?? Guid.Empty, "invoice source document id");
        SmokeAssertions.Equal("billing", invoiceSource.DashboardModule ?? "", "invoice source dashboard module");

        var invoiceDocument = SmokeAssertions.RequireSuccess(
            await _harness.GetInvoiceDocument.HandleAsync(
                new GetInvoiceDocumentQuery(invoiceSource.DocumentId ?? Guid.Empty),
                cancellationToken),
            "get invoice source document");
        SmokeAssertions.Equal(invoice.InvoiceId, invoiceDocument.Invoice.InvoiceId, "invoice document invoice id");
        SmokeAssertions.Equal(
            issuedInvoice.JournalEntryId,
            invoiceDocument.IssuedInvoice?.JournalEntryId ?? Guid.Empty,
            "invoice document issue journal id");
        SmokeAssertions.Equal(
            creditNote.CreditNoteId,
            invoiceDocument.CreditNote?.CreditNoteId ?? Guid.Empty,
            "invoice document credit note id");
        SmokeAssertions.True(invoiceDocument.VoidedInvoice is null, "invoice document should not include a void journal.");

        var paymentJournalEntryId = payment.JournalEntryId
            ?? throw new SmokeFailureException("payment journal entry id should be present.");
        var paymentSource = await ResolveSourceDocumentAsync(
            paymentJournalEntryId,
            "Payment",
            cancellationToken);
        SmokeAssertions.Equal(payment.PaymentId, paymentSource.DocumentId ?? Guid.Empty, "payment source document id");
        SmokeAssertions.Equal("payments", paymentSource.DashboardModule ?? "", "payment source dashboard module");

        var paymentDocument = SmokeAssertions.RequireSuccess(
            await _harness.GetInvoicePaymentDocument.HandleAsync(
                new GetInvoicePaymentDocumentQuery(paymentSource.DocumentId ?? Guid.Empty),
                cancellationToken),
            "get payment source document");
        SmokeAssertions.Equal(invoice.InvoiceId, paymentDocument.Invoice.InvoiceId, "payment document invoice id");
        SmokeAssertions.Equal(payment.PaymentId, paymentDocument.Payment.PaymentId, "payment document payment id");
        SmokeAssertions.Equal(
            payment.JournalEntryId ?? Guid.Empty,
            paymentDocument.Payment.JournalEntryId ?? Guid.Empty,
            "payment document journal id");
        SmokeAssertions.True(paymentDocument.Reversal is null, "payment document should not include a reversal.");

        var creditNoteSource = await ResolveSourceDocumentAsync(
            creditNote.JournalEntryId,
            "CreditNote",
            cancellationToken);
        SmokeAssertions.Equal(
            creditNote.CreditNoteId,
            creditNoteSource.DocumentId ?? Guid.Empty,
            "credit note source document id");

        var creditNoteDocument = SmokeAssertions.RequireSuccess(
            await _harness.GetCreditNoteDocument.HandleAsync(
                new GetCreditNoteDocumentQuery(creditNoteSource.DocumentId ?? Guid.Empty),
                cancellationToken),
            "get credit note source document");
        SmokeAssertions.Equal(invoice.InvoiceId, creditNoteDocument.Invoice.InvoiceId, "credit note invoice id");
        SmokeAssertions.Equal(
            creditNote.CreditNoteId,
            creditNoteDocument.CreditNote.CreditNoteId,
            "credit note document id");
        SmokeAssertions.Equal(
            creditNote.JournalEntryId,
            creditNoteDocument.CreditNote.JournalEntryId,
            "credit note journal id");

        var refundSource = await ResolveSourceDocumentAsync(
            refund.JournalEntryId,
            "ClientRefund",
            cancellationToken);
        SmokeAssertions.Equal(refund.RefundId, refundSource.DocumentId ?? Guid.Empty, "refund source document id");
        SmokeAssertions.Equal("refund", refundSource.DashboardStep ?? "", "refund source dashboard step");

        var refundDocument = SmokeAssertions.RequireSuccess(
            await _harness.GetClientRefundDocument.HandleAsync(
                new GetClientRefundDocumentQuery(refundSource.DocumentId ?? Guid.Empty),
                cancellationToken),
            "get refund source document");
        SmokeAssertions.Equal(refund.RefundId, refundDocument.Refund.RefundId, "refund document id");
        SmokeAssertions.Equal(refund.JournalEntryId, refundDocument.Refund.JournalEntryId, "refund journal id");
        SmokeAssertions.Money(refund.ClientBalanceBefore, refundDocument.Refund.ClientBalanceBefore, "refund readback balance before");
        SmokeAssertions.Money(refund.ClientBalanceAfter, refundDocument.Refund.ClientBalanceAfter, "refund readback balance after");
    }

    private async Task<JournalEntrySourceDocumentResult> ResolveSourceDocumentAsync(
        Guid journalEntryId,
        string expectedDocumentKind,
        CancellationToken cancellationToken)
    {
        var sourceDocument = SmokeAssertions.RequireSuccess(
            await _harness.GetJournalEntrySourceDocument.HandleAsync(
                new GetJournalEntrySourceDocumentQuery(journalEntryId),
                cancellationToken),
            $"resolve {expectedDocumentKind} source document");

        SmokeAssertions.True(sourceDocument.IsResolved, $"{expectedDocumentKind} source document should resolve.");
        SmokeAssertions.Equal(
            expectedDocumentKind,
            sourceDocument.DocumentKind ?? "",
            $"{expectedDocumentKind} source document kind");

        return sourceDocument;
    }

    private async Task AssertEntitlementVersioningAsync(
        Guid clientId,
        Guid invoiceId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var command = new IssueEntitlementSnapshotFromPaidInvoiceCommand(
            invoiceId,
            businessDate.AddMonths(1),
            businessDate.AddMonths(1).AddDays(7),
            businessDate.AddMonths(1).AddDays(14),
            AllowedDevices: 10,
            AllowedBranches: 2,
            ApprovedBy: "Accounting smoke operator",
            ApprovalReason: "Paid invoice and active contract verified by the accounting smoke.",
            Modules:
            [
                new IssueEntitlementSnapshotModuleCommand("Accounting", IsEnabled: true),
                new IssueEntitlementSnapshotModuleCommand("Reports", IsEnabled: true)
            ],
            AllowedNamedUsers: 40,
            AllowedConcurrentUsers: 12,
            FeatureLimits:
            [
                new IssueEntitlementSnapshotFeatureLimitCommand(
                    "Accounting",
                    "MONTHLY_POSTINGS",
                    5000,
                    "COUNT")
            ]);

        var first = SmokeAssertions.RequireSuccess(
            await _harness.IssueEntitlementSnapshot.HandleAsync(command, cancellationToken),
            "issue first versioned entitlement");
        var second = SmokeAssertions.RequireSuccess(
            await _harness.IssueEntitlementSnapshot.HandleAsync(command, cancellationToken),
            "issue second versioned entitlement");

        SmokeAssertions.True(first.EntitlementVersion > 0, "first entitlement version should be positive.");
        SmokeAssertions.True(
            second.EntitlementVersion > first.EntitlementVersion,
            "second entitlement version should be greater than the first.");
        SmokeAssertions.Equal(1L, first.ContractRevisionNumber, "first entitlement contract revision");
        SmokeAssertions.Equal(1L, second.ContractRevisionNumber, "second entitlement contract revision");
        SmokeAssertions.Equal(ProductCatalogRevisionId, first.ProductCatalogRevisionId, "first entitlement catalog revision id");
        SmokeAssertions.Equal(1L, first.ProductCatalogRevisionNumber, "first entitlement catalog revision number");
        SmokeAssertions.Equal(ProductCatalogRevisionId, second.ProductCatalogRevisionId, "second entitlement catalog revision id");
        SmokeAssertions.Equal(40, first.AllowedNamedUsers ?? -1, "first entitlement named-user allowance");
        SmokeAssertions.Equal(12, first.AllowedConcurrentUsers ?? -1, "first entitlement concurrent-user allowance");
        SmokeAssertions.Equal(first.ApprovedAtUtc, first.EffectiveFromUtc, "immediate entitlement effective-from time");
        var firstFeatureLimit = first.FeatureLimits?.SingleOrDefault();
        SmokeAssertions.True(firstFeatureLimit is not null, "first entitlement should retain its feature limit.");
        SmokeAssertions.Equal("ACCOUNTING", firstFeatureLimit!.ModuleCode, "first entitlement feature-limit module");
        SmokeAssertions.Equal("MONTHLY_POSTINGS", firstFeatureLimit.FeatureCode, "first entitlement feature-limit code");
        SmokeAssertions.Equal(5000L, firstFeatureLimit.LimitValue, "first entitlement feature-limit value");
        SmokeAssertions.Equal("COUNT", firstFeatureLimit.Unit, "first entitlement feature-limit unit");
        SmokeAssertions.True(
            first.ClientAccessRevisionId != Guid.Empty,
            "first client access revision id should be populated.");
        SmokeAssertions.True(
            second.ClientAccessRevisionId != first.ClientAccessRevisionId,
            "each entitlement issue should create a distinct client access revision.");
        SmokeAssertions.True(
            first.SupersedesClientAccessRevisionId is null,
            "first client access revision should be the chain root.");
        SmokeAssertions.Equal(
            first.ClientAccessRevisionId,
            second.SupersedesClientAccessRevisionId ?? Guid.Empty,
            "second client access revision predecessor");
        SmokeAssertions.Equal(
            "Accounting smoke operator",
            second.ApprovedBy,
            "client access revision approver");

        var latest = SmokeAssertions.RequireSuccess(
            await _harness.GetLatestEntitlementSnapshot.HandleAsync(
                new GetLatestEntitlementSnapshotQuery(clientId),
                cancellationToken),
            "read latest versioned entitlement");

        SmokeAssertions.Equal(
            second.EntitlementVersion,
            latest.EntitlementVersion,
            "latest entitlement version");
        SmokeAssertions.Equal(
            second.ClientAccessRevisionId,
            latest.ClientAccessRevisionId,
            "latest client access revision id");
        SmokeAssertions.Equal(1L, latest.ContractRevisionNumber, "latest entitlement contract revision");
        SmokeAssertions.Equal(ProductCatalogRevisionId, latest.ProductCatalogRevisionId, "latest entitlement catalog revision id");
        SmokeAssertions.Equal(1L, latest.ProductCatalogRevisionNumber, "latest entitlement catalog revision number");
        SmokeAssertions.Equal(40, latest.AllowedNamedUsers ?? -1, "latest entitlement named-user allowance");
        SmokeAssertions.Equal(12, latest.AllowedConcurrentUsers ?? -1, "latest entitlement concurrent-user allowance");
        SmokeAssertions.Equal(1, latest.FeatureLimits?.Count ?? 0, "latest entitlement feature-limit count");

        var latestRevision = await _harness.ClientAccessRevisions.GetLatestForClientAsync(
            ClientId.Create(clientId),
            cancellationToken);

        SmokeAssertions.True(latestRevision is not null, "latest client access revision should exist.");
        SmokeAssertions.Equal(
            second.ClientAccessRevisionId,
            latestRevision!.Id.Value,
            "persisted latest client access revision id");
        SmokeAssertions.Equal(invoiceId, latestRevision.SourceInvoiceId?.Value ?? Guid.Empty, "revision source invoice id");
        SmokeAssertions.Equal(1L, latestRevision.ContractRevisionNumber, "persisted access contract revision");
        SmokeAssertions.Equal(ProductCatalogRevisionId, latestRevision.ProductCatalogRevisionId.Value, "persisted access catalog revision id");
        SmokeAssertions.Equal(1L, latestRevision.ProductCatalogRevisionNumber, "persisted access catalog revision number");
        SmokeAssertions.Equal(2, latestRevision.Modules.Count, "revision module count");
        SmokeAssertions.Equal(40, latestRevision.AllowedNamedUsers ?? -1, "persisted access named-user allowance");
        SmokeAssertions.Equal(12, latestRevision.AllowedConcurrentUsers ?? -1, "persisted access concurrent-user allowance");
        var persistedFeatureLimit = latestRevision.FeatureLimits.SingleOrDefault();
        SmokeAssertions.True(persistedFeatureLimit is not null, "persisted access revision should retain its feature limit.");
        SmokeAssertions.Equal("ACCOUNTING", persistedFeatureLimit!.ModuleCode.Value, "persisted feature-limit module");
        SmokeAssertions.Equal("MONTHLY_POSTINGS", persistedFeatureLimit.FeatureCode.Value, "persisted feature-limit code");
        SmokeAssertions.Equal(5000L, persistedFeatureLimit.LimitValue, "persisted feature-limit value");
        SmokeAssertions.Equal("COUNT", persistedFeatureLimit.Unit, "persisted feature-limit unit");

        var entitlementMessages = (await _harness.CloudOutboxMessages.ListPageAsync(
                CloudOutboxMessageStatus.Pending,
                "EntitlementSnapshotIssued",
                ClientId.Create(clientId),
                beforeOccurredAtUtc: null,
                beforeMessageId: null,
                take: 100,
                cancellationToken))
            .Where(message => message.PayloadJson.Contains(_runId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        SmokeAssertions.Equal(2, entitlementMessages.Length, "versioned entitlement outbox message count");

        var publishedVersions = entitlementMessages
            .Select(message =>
            {
                using var payload = JsonDocument.Parse(message.PayloadJson);
                return payload.RootElement.GetProperty("entitlementVersion").GetInt64();
            })
            .ToHashSet();

        SmokeAssertions.True(
            publishedVersions.SetEquals([first.EntitlementVersion, second.EntitlementVersion]),
            "outbox should preserve both Office-issued entitlement versions.");

        var publishedRevisionIds = entitlementMessages
            .Select(message =>
            {
                using var payload = JsonDocument.Parse(message.PayloadJson);
                return payload.RootElement.GetProperty("clientAccessRevisionId").GetGuid();
            })
            .ToHashSet();

        SmokeAssertions.True(
            publishedRevisionIds.SetEquals([first.ClientAccessRevisionId, second.ClientAccessRevisionId]),
            "outbox should preserve both approved client access revision ids.");

        SmokeAssertions.True(
            entitlementMessages.All(message =>
            {
                using var payload = JsonDocument.Parse(message.PayloadJson);
                var root = payload.RootElement;
                var featureLimits = root.GetProperty("featureLimits");
                var featureLimit = featureLimits[0];

                return root.GetProperty("eventVersion").GetString() == "6"
                    && root.GetProperty("effectiveFromUtc").GetDateTimeOffset()
                        == root.GetProperty("issuedAtUtc").GetDateTimeOffset()
                    && root.GetProperty("contractRevisionNumber").GetInt64() == 1L
                    && root.GetProperty("productCatalogRevisionId").GetGuid() == ProductCatalogRevisionId
                    && root.GetProperty("productCatalogRevisionNumber").GetInt64() == 1L
                    && root.GetProperty("allowedNamedUsers").GetInt32() == 40
                    && root.GetProperty("allowedConcurrentUsers").GetInt32() == 12
                    && featureLimits.GetArrayLength() == 1
                    && featureLimit.GetProperty("moduleCode").GetString() == "ACCOUNTING"
                    && featureLimit.GetProperty("featureCode").GetString() == "MONTHLY_POSTINGS"
                    && featureLimit.GetProperty("limitValue").GetInt64() == 5000L
                    && featureLimit.GetProperty("unit").GetString() == "COUNT";
            }),
            "outbox should publish event v6 with exact revisions, effective time, and desired-access limits.");
    }

    private async Task AssertJournalAndOutboxShapeAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var journalEntries = (await _harness.JournalEntries.ListAsync(cancellationToken: cancellationToken))
            .Where(entry => entry.SourceReference?.Contains(_runId, StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();
        SmokeAssertions.Equal(5, journalEntries.Length, "journal entry count");
        SmokeAssertions.True(
            journalEntries.All(entry => entry.TotalDebit.Amount == entry.TotalCredit.Amount),
            "every journal entry should be balanced.");
        SmokeAssertions.Equal(
            2,
            journalEntries.Count(entry => entry.SourceType == JournalSourceType.BillingInvoice),
            "billing invoice journal count");
        SmokeAssertions.Equal(
            1,
            journalEntries.Count(entry => entry.SourceType == JournalSourceType.PaymentReceipt),
            "payment receipt journal count");
        SmokeAssertions.Equal(
            1,
            journalEntries.Count(entry => entry.SourceType == JournalSourceType.BillingCreditNote),
            "credit note journal count");
        SmokeAssertions.Equal(
            1,
            journalEntries.Count(entry => entry.SourceType == JournalSourceType.ClientRefund),
            "client refund journal count");

        var outboxMessages = (await _harness.CloudOutboxMessages.ListPageAsync(
            CloudOutboxMessageStatus.Pending,
            messageType: null,
            ClientId.Create(clientId),
            beforeOccurredAtUtc: null,
            beforeMessageId: null,
            take: 100,
            cancellationToken))
            .Where(message => message.PayloadJson.Contains(_runId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        SmokeAssertions.Equal(9, outboxMessages.Length, "pending outbox message count");
        SmokeAssertions.True(
            outboxMessages.Any(message => message.MessageType == "ClientCreditApplied"),
            "outbox should contain ClientCreditApplied.");
        SmokeAssertions.True(
            outboxMessages.All(message => message.Status == CloudOutboxMessageStatus.Pending),
            "all smoke outbox messages should be pending.");
        SmokeAssertions.True(
            outboxMessages.All(message => message.ClientId?.Value == clientId),
            "all smoke outbox messages should persist the owning client id.");

        var publishPolicy = new ConfiguredCloudOutboxPublishPolicy(
            Options.Create(new ControlCloudPublisherOptions
            {
                MaximumAttemptCount = 5
            }));
        var listHandler = new ListCloudOutboxMessagesHandler(
            _harness.CloudOutboxMessages,
            publishPolicy,
            _harness.Clock);
        var firstPage = SmokeAssertions.RequireSuccess(
            await listHandler.HandleAsync(
                new ListCloudOutboxMessagesQuery("Pending", null, clientId, 4, null),
                cancellationToken),
            "list first client outbox page");

        SmokeAssertions.Equal(4, firstPage.Messages.Count, "first outbox page row count");
        SmokeAssertions.True(firstPage.HasMore, "first outbox page should have a continuation.");
        SmokeAssertions.True(
            !string.IsNullOrWhiteSpace(firstPage.NextCursor),
            "first outbox page should return a cursor.");
        SmokeAssertions.Equal(9L, firstPage.Summary.TotalCount, "client outbox summary total");
        SmokeAssertions.Equal(9L, firstPage.Summary.PendingCount, "client outbox summary pending");
        SmokeAssertions.Equal(0L, firstPage.Summary.FailedCount, "client outbox summary failed");
        SmokeAssertions.Equal(9L, firstPage.Summary.ReadyForPublishingCount, "client outbox summary ready");
        SmokeAssertions.Equal(0L, firstPage.Summary.TotalAttemptCount, "client outbox summary attempts");

        var secondPage = SmokeAssertions.RequireSuccess(
            await listHandler.HandleAsync(
                new ListCloudOutboxMessagesQuery("Pending", null, clientId, 4, firstPage.NextCursor),
                cancellationToken),
            "list second client outbox page");
        var thirdPage = SmokeAssertions.RequireSuccess(
            await listHandler.HandleAsync(
                new ListCloudOutboxMessagesQuery("Pending", null, clientId, 4, secondPage.NextCursor),
                cancellationToken),
            "list third client outbox page");

        SmokeAssertions.Equal(4, secondPage.Messages.Count, "second outbox page row count");
        SmokeAssertions.True(secondPage.HasMore, "second outbox page should have a continuation.");
        SmokeAssertions.Equal(1, thirdPage.Messages.Count, "third outbox page row count");
        SmokeAssertions.True(!thirdPage.HasMore, "third outbox page should be terminal.");
        SmokeAssertions.True(thirdPage.NextCursor is null, "terminal outbox page should not return a cursor.");

        var pagedMessageIds = firstPage.Messages
            .Concat(secondPage.Messages)
            .Concat(thirdPage.Messages)
            .Select(message => message.CloudOutboxMessageId)
            .ToArray();
        SmokeAssertions.Equal(9, pagedMessageIds.Length, "paged outbox row count");
        SmokeAssertions.Equal(9, pagedMessageIds.Distinct().Count(), "paged outbox distinct row count");
        SmokeAssertions.True(
            firstPage.Messages
                .Concat(secondPage.Messages)
                .Concat(thirdPage.Messages)
                .All(message => message.ClientId == clientId),
            "every paged outbox row should belong to the requested client.");

        SmokeAssertions.RequireFailure(
            await listHandler.HandleAsync(
                new ListCloudOutboxMessagesQuery(null, null, clientId, 4, "not-a-valid-cursor"),
                cancellationToken),
            "reject malformed client outbox cursor",
            nameof(ListCloudOutboxMessagesQuery.Cursor),
            "invalid");
    }

    private async Task<int> PublishOutboxToCloudAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var endpointUrl = _options.CloudReceiverUrl?.Trim();

        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out _))
        {
            throw new SmokeFailureException("--cloud-receiver-url must be an absolute URL.");
        }

        var publisherOptions = Options.Create(new ControlCloudPublisherOptions
        {
            Mode = "Http",
            SourceSystem = "SafarSuite.ControlDesk",
            Environment = "Smoke",
            SigningKeyId = SigningKeyId,
            SigningSecret = SigningSecret,
            EndpointUrl = endpointUrl,
            MaximumAttemptCount = 5,
            RetryDelaySeconds = 60
        });
        using var httpClient = new HttpClient();
        var publisher = new HttpControlCloudOutboxPublisher(
            httpClient,
            new ControlCloudEnvelopeBuilder(publisherOptions, _harness.Clock),
            publisherOptions,
            new SmokePublisherAvailability());
        var pendingMessages = (await _harness.CloudOutboxMessages.ListPageAsync(
            CloudOutboxMessageStatus.Pending,
            messageType: null,
            ClientId.Create(clientId),
            beforeOccurredAtUtc: null,
            beforeMessageId: null,
            take: 100,
            cancellationToken))
            .Where(message => message.PayloadJson.Contains(_runId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        SmokeAssertions.Equal(9, pendingMessages.Length, "cloud publish pending smoke outbox count");

        foreach (var message in pendingMessages)
        {
            var publishResult = await publisher.PublishAsync(message, cancellationToken);

            if (!publishResult.IsSuccess)
            {
                throw new SmokeFailureException(
                    $"cloud publish failed for {message.MessageType}: {publishResult.FailureReason}");
            }

            SmokeAssertions.True(
                !string.IsNullOrWhiteSpace(publishResult.CloudReference),
                "cloud publish should return a cloud reference.");
            SmokeAssertions.True(
                !string.IsNullOrWhiteSpace(publishResult.EnvelopeSignature),
                "cloud publish should include the envelope signature.");

            var trackedMessage = await _harness.CloudOutboxMessages.GetByIdAsync(
                message.Id,
                cancellationToken)
                ?? throw new SmokeFailureException($"published outbox message {message.Id.Value} could not be reloaded.");

            trackedMessage.MarkSent(_harness.Clock.UtcNow);
            await _harness.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        var sentMessages = (await _harness.CloudOutboxMessages.ListPageAsync(
            CloudOutboxMessageStatus.Sent,
            messageType: null,
            ClientId.Create(clientId),
            beforeOccurredAtUtc: null,
            beforeMessageId: null,
            take: 100,
            cancellationToken))
            .Where(message => message.PayloadJson.Contains(_runId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        SmokeAssertions.Equal(9, sentMessages.Length, "cloud publish sent smoke outbox count");

        return sentMessages.Length;
    }

    private static void AssertBalanced(decimal totalDebit, decimal totalCredit, string label)
    {
        SmokeAssertions.Money(totalDebit, totalCredit, label);
    }

    private static void AssertJournalLineAccountMetadata(
        IEnumerable<(
            string? Code,
            string? Name,
            string? Type,
            string? NormalBalance,
            string? Level,
            bool? IsPostingAccount,
            string? Status)> lines,
        string label)
    {
        var journalLines = lines.ToArray();
        SmokeAssertions.True(journalLines.Length > 0, $"{label} should include journal lines.");
        SmokeAssertions.True(
            journalLines.All(line =>
                !string.IsNullOrWhiteSpace(line.Code)
                && !string.IsNullOrWhiteSpace(line.Name)
                && !string.IsNullOrWhiteSpace(line.Type)
                && !string.IsNullOrWhiteSpace(line.NormalBalance)
                && !string.IsNullOrWhiteSpace(line.Level)
                && line.IsPostingAccount == true
                && !string.IsNullOrWhiteSpace(line.Status)),
            $"{label} lines should expose COA metadata.");
    }

    private static TrialBalanceLineResult FindTrialBalanceLine(
        GetTrialBalanceResult trialBalance,
        Guid ledgerAccountId,
        string label)
    {
        return trialBalance.Lines.SingleOrDefault(line => line.LedgerAccountId == ledgerAccountId)
            ?? throw new SmokeFailureException($"{label} was not included.");
    }

    private static ProfitAndLossStatementSectionResult FindProfitAndLossSection(
        GetProfitAndLossStatementResult statement,
        string sectionType,
        string label)
    {
        return statement.Sections.SingleOrDefault(section =>
            string.Equals(section.Type, sectionType, StringComparison.OrdinalIgnoreCase))
            ?? throw new SmokeFailureException($"{label} was not included.");
    }

    private static BalanceSheetSectionResult FindBalanceSheetSection(
        GetBalanceSheetResult statement,
        string sectionType,
        string label)
    {
        return statement.Sections.SingleOrDefault(section =>
            string.Equals(section.Type, sectionType, StringComparison.OrdinalIgnoreCase))
            ?? throw new SmokeFailureException($"{label} was not included.");
    }

    private async Task<LedgerAccount> RequireLedgerAccountAsync(
        Guid ledgerAccountId,
        CancellationToken cancellationToken)
    {
        return await _harness.LedgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(ledgerAccountId),
            cancellationToken)
            ?? throw new SmokeFailureException($"ledger account {ledgerAccountId} was not found.");
    }

    private string Code(string prefix)
    {
        return $"{prefix}{_runId}";
    }

    private string Document(string suffix)
    {
        return $"SMK-{_runId}-{suffix}";
    }

    private sealed record LedgerAccounts(
        Guid AccountsReceivableControlId,
        Guid AccountsReceivableAccountId,
        Guid CashOrBankControlId,
        Guid CashOrBankAccountId,
        Guid RevenueAccountId,
        Guid TaxAccountId,
        Guid RetainedEarningsAccountId,
        Guid IncomeSummaryAccountId,
        Guid RoundingAdjustmentAccountId,
        IReadOnlyCollection<Guid> ReconciledLedgerAccountIds);

    private sealed class SmokePublisherAvailability : ICloudOutboxPublisherAvailability
    {
        public CloudOutboxPublisherAvailabilitySnapshot GetSnapshot() =>
            new(true, true, "SmokeReady");
    }
}

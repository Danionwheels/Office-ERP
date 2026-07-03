using System.Numerics;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;
using SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;
using SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.ControlCloud;
using Microsoft.Extensions.Options;

namespace SafarSuite.ControlDesk.AccountingSmoke;

internal sealed class AccountingSmokeRunner
{
    private const string CurrencyCode = "PKR";
    private const string SigningKeyId = "local-dev";
    private const string SigningSecret = "local-development-signing-secret-change-before-cloud";

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
        await AssertLedgerAccountGuardrailsAsync(accounts, cancellationToken);
        var client = await CreateClientAsync(cancellationToken);
        var contract = await CreateContractAsync(client.ClientId, businessDate, cancellationToken);
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
        SmokeAssertions.Equal("Issued", firstIssue.InvoiceStatus, "first invoice issue status");

        var payment = await RecordPaymentAsync(
            firstInvoice.InvoiceId,
            accounts.CashOrBankAccountId,
            accounts.AccountsReceivableAccountId,
            businessDate,
            cancellationToken);
        AssertBalanced(payment.TotalDebit, payment.TotalCredit, "payment journal");
        SmokeAssertions.Equal("Paid", payment.InvoiceStatus, "paid invoice status");
        SmokeAssertions.Money(0m, payment.BalanceDue, "paid invoice balance");

        var creditNote = await IssueCreditNoteAsync(firstInvoice.InvoiceId, businessDate, cancellationToken);
        AssertBalanced(creditNote.TotalDebit, creditNote.TotalCredit, "credit note journal");
        SmokeAssertions.Money(110m, creditNote.Amount, "credit note amount");

        var refund = await IssueRefundAsync(
            client.ClientId,
            accounts.CashOrBankAccountId,
            accounts.AccountsReceivableAccountId,
            businessDate,
            cancellationToken);
        AssertBalanced(refund.TotalDebit, refund.TotalCredit, "client refund journal");
        SmokeAssertions.Money(-110m, refund.ClientBalanceBefore, "refund balance before");
        SmokeAssertions.Money(-70m, refund.ClientBalanceAfter, "refund balance after");

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

        await AssertFinalStatementAsync(client.ClientId, cancellationToken);
        await AssertJournalAndOutboxShapeAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.CloudReceiverUrl))
        {
            PublishedCloudMessageCount = await PublishOutboxToCloudAsync(cancellationToken);
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
        var accountsReceivableSuggestion = await SuggestLedgerAccountCodeAsync("ClientReceivable", cancellationToken);
        SmokeAssertions.Equal(9, accountsReceivableSuggestion.SuggestedCode.Length, "receivable setup suggestion length");
        SmokeAssertions.Equal("Header", assetHeader.Level, "asset header level");
        SmokeAssertions.Equal("Total", assetTotal.Level, "asset total level");

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
        var cashOrBank = await GetOrCreateReusableLedgerAccountAsync(
            "CashBank",
            "Cash and bank",
            null,
            cancellationToken);
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

        var nextAccountsReceivableSuggestion = await SuggestLedgerAccountCodeAsync("ClientReceivable", cancellationToken);
        SmokeAssertions.Equal(
            BigInteger.Parse(accountsReceivableSuggestion.SuggestedCode) + BigInteger.One,
            BigInteger.Parse(nextAccountsReceivableSuggestion.SuggestedCode),
            "next receivable setup suggestion");

        return new LedgerAccounts(
            accountsReceivable.LedgerAccountId,
            cashOrBank.LedgerAccountId,
            revenue.LedgerAccountId,
            tax.LedgerAccountId,
            [
                assetHeader.LedgerAccountId,
                assetTotal.LedgerAccountId,
                accountsReceivableControl.LedgerAccountId,
                accountsReceivable.LedgerAccountId,
                cashOrBank.LedgerAccountId,
                revenue.LedgerAccountId,
                tax.LedgerAccountId
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
        return SmokeAssertions.RequireSuccess(
            await _harness.SuggestLedgerAccountCode.HandleAsync(
                new SuggestLedgerAccountCodeQuery(role),
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
        SmokeAssertions.RequireFailure(
            await _harness.CreateLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    nextReceivable.SuggestedCode,
                    "Subsidiary with wrong parent should be rejected",
                    nextReceivable.Type,
                    nextReceivable.NormalBalance,
                    accounts.AccountsReceivableAccountId,
                    nextReceivable.IsPostingAccount,
                    "Subsidiary"),
                cancellationToken),
            "reject subsidiary with non-control parent",
            nameof(CreateLedgerAccountCommand.ParentAccountId),
            "Control account");
    }

    private static bool IsInsideRange(string code, AccountCodeRange range)
    {
        return code.All(char.IsDigit)
            && code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0;
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
                    [new CreateClientContractModuleCommand("BILLING", true)]),
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
            await _harness.GetClientStatement.HandleAsync(
                new GetClientStatementQuery(clientId),
                cancellationToken),
            "get client statement");

        var summary = statement.CurrencySummaries.Single(summary => summary.CurrencyCode == CurrencyCode);
        SmokeAssertions.Money(220m, summary.TotalInvoiced, "statement total invoiced");
        SmokeAssertions.Money(110m, summary.TotalPaid, "statement total paid");
        SmokeAssertions.Money(10m, summary.AvailableCredit, "statement available credit");
        SmokeAssertions.Money(40m, summary.BalanceDue, "statement balance due");
        SmokeAssertions.Equal(2, summary.InvoiceCount, "statement invoice count");
        SmokeAssertions.Equal(1, summary.OpenInvoiceCount, "statement open invoice count");

        SmokeAssertions.True(
            statement.Lines.Any(line => line.DocumentType == "Credit note" && line.Credit == 110m),
            "statement should include the credit note line.");
        SmokeAssertions.True(
            statement.Lines.Any(line => line.DocumentType == "Client refund" && line.Debit == 40m),
            "statement should include the client refund line.");
        SmokeAssertions.True(
            statement.Lines.Any(line =>
                line.DocumentType == "Applied credit"
                && line.Debit == 60m
                && line.Credit == 60m
                && line.JournalEntryId is null),
            "statement should include a zero-net applied credit line without a journal.");
        SmokeAssertions.Equal(5, statement.JournalPostings.Count, "statement journal posting count");
    }

    private async Task AssertJournalAndOutboxShapeAsync(CancellationToken cancellationToken)
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

        var outboxMessages = (await _harness.CloudOutboxMessages.ListAsync(
            CloudOutboxMessageStatus.Pending,
            messageType: null,
            cancellationToken))
            .Where(message => message.PayloadJson.Contains(_runId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        SmokeAssertions.Equal(7, outboxMessages.Length, "pending outbox message count");
        SmokeAssertions.True(
            outboxMessages.Any(message => message.MessageType == "ClientCreditApplied"),
            "outbox should contain ClientCreditApplied.");
        SmokeAssertions.True(
            outboxMessages.All(message => message.Status == CloudOutboxMessageStatus.Pending),
            "all smoke outbox messages should be pending.");
    }

    private async Task<int> PublishOutboxToCloudAsync(CancellationToken cancellationToken)
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
            publisherOptions);
        var pendingMessages = (await _harness.CloudOutboxMessages.ListAsync(
            CloudOutboxMessageStatus.Pending,
            messageType: null,
            cancellationToken))
            .Where(message => message.PayloadJson.Contains(_runId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        SmokeAssertions.Equal(7, pendingMessages.Length, "cloud publish pending smoke outbox count");

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

        var sentMessages = (await _harness.CloudOutboxMessages.ListAsync(
            CloudOutboxMessageStatus.Sent,
            messageType: null,
            cancellationToken))
            .Where(message => message.PayloadJson.Contains(_runId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        SmokeAssertions.Equal(7, sentMessages.Length, "cloud publish sent smoke outbox count");

        return sentMessages.Length;
    }

    private static void AssertBalanced(decimal totalDebit, decimal totalCredit, string label)
    {
        SmokeAssertions.Money(totalDebit, totalCredit, label);
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
        Guid AccountsReceivableAccountId,
        Guid CashOrBankAccountId,
        Guid RevenueAccountId,
        Guid TaxAccountId,
        IReadOnlyCollection<Guid> ReconciledLedgerAccountIds);
}

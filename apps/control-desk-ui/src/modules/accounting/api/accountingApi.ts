import { apiRequest } from "../../../shared/api/httpClient";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  AccountCodeRangeValidation,
  AccountingControlSettings,
  AccountingControlSettingsInput,
  AccountingPeriod,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  AccountingPeriodFormInput,
  BalanceSheet,
  BalanceSheetFilters,
  ChartOfAccountsImportTextPreview,
  JournalEntryFilters,
  JournalEntryRegisterPage,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  JournalVoucherNumberPreview,
  LedgerAccountActivity,
  LedgerAccountActivityFilters,
  LedgerAccountCodeSuggestion,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  LedgerAccountReconciliation,
  LedgerAccountRepairPlan,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  OpeningBalanceImportInput,
  OpeningBalanceImportPreview,
  OpeningBalanceImportTextPreview,
  OpeningBalanceProfile,
  ProfitAndLossStatement,
  ProfitAndLossStatementFilters,
  StandardChartOfAccountsBootstrap,
  TrialBalance,
  TrialBalanceFilters,
  VoucherNumberingRule,
  VoucherNumberingRuleInput,
  VoidManualJournalEntryInput,
  VoidManualJournalEntryResult
} from "../types/accountingTypes";

type ListLedgerAccountsResponse = {
  companyCode: string;
  accounts: LedgerAccountSummary[];
};

type LedgerAccountReconciliationResponse = LedgerAccountReconciliation;

type LedgerAccountRepairPlanResponse = LedgerAccountRepairPlan;

type AccountCodeRangeValidationResponse = AccountCodeRangeValidation;

type ListAccountCodeRangesResponse = {
  companyCode: string;
  ranges: AccountCodeRange[];
};

type BootstrapStandardChartOfAccountsResponse = StandardChartOfAccountsBootstrap;

type ListAccountingPeriodsResponse = {
  companyCode: string;
  periods: AccountingPeriod[];
};

type ListVoucherNumberingRulesResponse = {
  companyCode: string;
  rules: VoucherNumberingRule[];
};

type LedgerAccountWriteResponse = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  level?: string | null;
  parentAccountId?: string | null;
  isPostingAccount: boolean;
  status: string;
  createdAtUtc?: string;
};

type PreviewChartOfAccountsImportTextResponse = ChartOfAccountsImportTextPreview;

export async function listLedgerAccounts(
  filters: LedgerAccountFilters
): Promise<LedgerAccountSummary[]> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", filters.companyCode);
  setQuery(query, "search", filters.search);
  setQuery(query, "type", filters.type);
  setQuery(query, "status", filters.status);
  setQuery(query, "role", filters.role);
  setQuery(query, "viewMode", filters.viewMode);

  if (filters.posting === "posting") {
    query.set("isPostingAccount", "true");
  }

  if (filters.posting === "control") {
    query.set("isPostingAccount", "false");
  }

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;
  const response = await apiRequest<ListLedgerAccountsResponse>(
    `/api/v1/accounting/ledger-accounts${suffix}`
  );

  return response.accounts;
}

export async function getLedgerAccountReconciliation(
  companyCode: string
): Promise<LedgerAccountReconciliation> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<LedgerAccountReconciliationResponse>(
    `/api/v1/accounting/ledger-accounts/reconciliation${suffix}`
  );
}

export async function getLedgerAccountRepairPlan(
  companyCode: string
): Promise<LedgerAccountRepairPlan> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<LedgerAccountRepairPlanResponse>(
    `/api/v1/accounting/ledger-accounts/repair-plan${suffix}`
  );
}

export async function createLedgerAccount(
  input: LedgerAccountEditorInput
): Promise<LedgerAccountWriteResponse> {
  return apiRequest<LedgerAccountWriteResponse>("/api/v1/accounting/ledger-accounts", {
    method: "POST",
    body: JSON.stringify({
      code: input.code,
      name: input.name,
      type: input.type,
      normalBalance: input.normalBalance,
      parentAccountId: optionalText(input.parentAccountId),
      isPostingAccount: input.isPostingAccount,
      level: optionalText(input.level)
    })
  });
}

export async function updateLedgerAccount(
  ledgerAccountId: string,
  input: LedgerAccountEditorInput
): Promise<LedgerAccountWriteResponse> {
  return apiRequest<LedgerAccountWriteResponse>(
    `/api/v1/accounting/ledger-accounts/${encodeURIComponent(ledgerAccountId)}`,
    {
      method: "PUT",
      body: JSON.stringify({
        name: input.name,
        isPostingAccount: input.isPostingAccount,
        status: input.status
      })
    }
  );
}

export async function suggestLedgerAccountCode(
  role: string,
  companyCode: string,
  parentAccountId?: string
): Promise<LedgerAccountCodeSuggestion> {
  const query = new URLSearchParams({ role });
  setQuery(query, "companyCode", companyCode);
  setQuery(query, "parentAccountId", parentAccountId);

  return apiRequest<LedgerAccountCodeSuggestion>(
    `/api/v1/accounting/ledger-accounts/suggest-code?${query.toString()}`
  );
}

export async function previewChartOfAccountsImportText(
  companyCode: string,
  importText: string,
  delimiter: string
): Promise<ChartOfAccountsImportTextPreview> {
  return apiRequest<PreviewChartOfAccountsImportTextResponse>(
    "/api/v1/accounting/ledger-accounts/import-preview",
    {
      method: "POST",
      body: JSON.stringify({
        companyCode: optionalText(companyCode),
        importText,
        delimiter: optionalText(delimiter)
      })
    }
  );
}

export async function listAccountCodeRanges(companyCode: string): Promise<AccountCodeRange[]> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;
  const response = await apiRequest<ListAccountCodeRangesResponse>(
    `/api/v1/accounting/accounting-setup/account-code-ranges${suffix}`
  );

  return response.ranges;
}

export async function getAccountCodeRangeValidation(
  companyCode: string
): Promise<AccountCodeRangeValidation> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<AccountCodeRangeValidationResponse>(
    `/api/v1/accounting/accounting-setup/account-code-ranges/validation${suffix}`
  );
}

export async function bootstrapStandardChartOfAccounts(
  companyCode: string
): Promise<StandardChartOfAccountsBootstrap> {
  return apiRequest<BootstrapStandardChartOfAccountsResponse>(
    "/api/v1/accounting/accounting-setup/standard-chart-of-accounts",
    {
      method: "POST",
      body: JSON.stringify({
        companyCode: optionalText(companyCode)
      })
    }
  );
}

export async function getAccountingControlSettings(
  companyCode: string
): Promise<AccountingControlSettings> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<AccountingControlSettings>(`/api/v1/accounting/accounting-controls${suffix}`);
}

export async function configureAccountingControlSettings(
  input: AccountingControlSettingsInput
): Promise<AccountingControlSettings> {
  return apiRequest<AccountingControlSettings>("/api/v1/accounting/accounting-controls", {
    method: "PUT",
    body: JSON.stringify({
      companyCode: optionalText(input.companyCode),
      baseCurrencyCode: input.baseCurrencyCode,
      retainedEarningsAccountId: optionalText(input.retainedEarningsAccountId),
      incomeSummaryAccountId: optionalText(input.incomeSummaryAccountId),
      roundingAccountId: optionalText(input.roundingAccountId)
    })
  });
}

export async function getOpeningBalanceProfile(
  companyCode: string
): Promise<OpeningBalanceProfile> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<OpeningBalanceProfile>(
    `/api/v1/accounting/accounting-setup/opening-balance-profile${suffix}`
  );
}

export async function configureOpeningBalanceProfile(
  companyCode: string,
  input: OpeningBalanceImportInput
): Promise<OpeningBalanceProfile> {
  return apiRequest<OpeningBalanceProfile>(
    "/api/v1/accounting/accounting-setup/opening-balance-profile",
    {
      method: "PUT",
      body: JSON.stringify({
        companyCode: optionalText(companyCode),
        fiscalYearFrom: input.profileFromDate,
        fiscalYearTo: input.profileToDate,
        status: input.profileStatus,
        transactionsAllowed: input.transactionsAllowed,
        profitAndLossCarryForwardAccountId: optionalText(
          input.profitAndLossCarryForwardAccountId
        )
      })
    }
  );
}

export async function configureDefaultAccountingControlSettings(
  companyCode: string
): Promise<AccountingControlSettings> {
  return apiRequest<AccountingControlSettings>("/api/v1/accounting/accounting-controls/defaults", {
    method: "POST",
    body: JSON.stringify({
      companyCode: optionalText(companyCode)
    })
  });
}

export async function listVoucherNumberingRules(
  companyCode: string
): Promise<VoucherNumberingRule[]> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;
  const response = await apiRequest<ListVoucherNumberingRulesResponse>(
    `/api/v1/accounting/accounting-setup/voucher-numbering${suffix}`
  );

  return response.rules;
}

export async function configureVoucherNumberingRule(
  sourceType: string,
  companyCode: string,
  input: VoucherNumberingRuleInput
): Promise<VoucherNumberingRule> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<VoucherNumberingRule>(
    `/api/v1/accounting/accounting-setup/voucher-numbering/${encodeURIComponent(sourceType)}${suffix}`,
    {
      method: "PUT",
      body: JSON.stringify({
        prefix: input.prefix,
        numberPaddingWidth: Number(input.numberPaddingWidth),
        isActive: input.isActive
      })
    }
  );
}

export async function listAccountingPeriods(companyCode: string): Promise<AccountingPeriod[]> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;
  const response = await apiRequest<ListAccountingPeriodsResponse>(
    `/api/v1/accounting/accounting-periods${suffix}`
  );

  return response.periods;
}

export async function createAccountingPeriod(
  input: AccountingPeriodFormInput
): Promise<AccountingPeriod> {
  return apiRequest<AccountingPeriod>("/api/v1/accounting/accounting-periods", {
    method: "POST",
    body: JSON.stringify({
      companyCode: optionalText(input.companyCode),
      name: optionalText(input.name),
      startsOn: input.startsOn,
      endsOn: input.endsOn
    })
  });
}

export async function getAccountingPeriodCloseReadiness(
  accountingPeriodId: string
): Promise<AccountingPeriodCloseReadiness> {
  return apiRequest<AccountingPeriodCloseReadiness>(
    `/api/v1/accounting/accounting-periods/${encodeURIComponent(accountingPeriodId)}/close-readiness`
  );
}

export async function getAccountingPeriodCloseJournalPreview(
  accountingPeriodId: string
): Promise<AccountingPeriodCloseJournalPreview> {
  return apiRequest<AccountingPeriodCloseJournalPreview>(
    `/api/v1/accounting/accounting-periods/${encodeURIComponent(accountingPeriodId)}/close-journal-preview`
  );
}

export async function closeAccountingPeriod(
  accountingPeriodId: string
): Promise<AccountingPeriod> {
  return apiRequest<AccountingPeriod>(
    `/api/v1/accounting/accounting-periods/${encodeURIComponent(accountingPeriodId)}/close`,
    {
      method: "POST"
    }
  );
}

export async function reopenAccountingPeriod(
  accountingPeriodId: string
): Promise<AccountingPeriod> {
  return apiRequest<AccountingPeriod>(
    `/api/v1/accounting/accounting-periods/${encodeURIComponent(accountingPeriodId)}/reopen`,
    {
      method: "POST"
    }
  );
}

export async function listJournalEntries(
  filters: JournalEntryFilters,
  cursor?: string | null
): Promise<JournalEntryRegisterPage> {
  const query = new URLSearchParams();
  setQuery(query, "fromDate", filters.fromDate);
  setQuery(query, "toDate", filters.toDate);
  setQuery(query, "sourceType", filters.sourceType);
  setQuery(query, "search", filters.search ?? "");
  setQuery(query, "cursor", cursor ?? "");
  query.set("take", "50");

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;
  const response = await apiRequest<JournalEntryRegisterPage>(
    `/api/v1/accounting/journal-entries${suffix}`
  );

  return {
    ...response,
    entries: response.entries.map((entry) => ({
      ...entry,
      lines: entry.lines ?? []
    }))
  };
}

export async function getJournalEntry(journalEntryId: string): Promise<JournalEntrySummary> {
  return apiRequest<JournalEntrySummary>(
    `/api/v1/accounting/journal-entries/${encodeURIComponent(journalEntryId)}`
  );
}

export async function getJournalEntrySourceDocument(
  journalEntryId: string
): Promise<JournalEntrySourceDocument> {
  return apiRequest<JournalEntrySourceDocument>(
    `/api/v1/accounting/journal-entries/${encodeURIComponent(journalEntryId)}/source-document`
  );
}

export async function previewJournalVoucherNumber(
  sourceType: string,
  entryDate: string
): Promise<JournalVoucherNumberPreview> {
  const query = new URLSearchParams({ sourceType, entryDate });

  return apiRequest<JournalVoucherNumberPreview>(
    `/api/v1/accounting/journal-entries/voucher-number-preview?${query.toString()}`
  );
}

export async function postManualJournalEntry(
  input: ManualJournalEntryInput
): Promise<JournalEntrySummary> {
  return apiRequest<JournalEntrySummary>("/api/v1/accounting/journal-entries/manual", {
    method: "POST",
    body: JSON.stringify({
      entryDate: input.entryDate,
      currencyCode: input.currencyCode,
      sourceReference: optionalText(input.sourceReference),
      memo: optionalText(input.memo),
      lines: input.lines.map((line) => ({
        ledgerAccountId: line.ledgerAccountId,
        debit: amountOrZero(line.debit),
        credit: amountOrZero(line.credit),
        description: optionalText(line.description)
      }))
    })
  });
}

export async function previewOpeningBalanceImport(
  input: OpeningBalanceImportInput
): Promise<OpeningBalanceImportPreview> {
  return apiRequest<OpeningBalanceImportPreview>(
    "/api/v1/accounting/journal-entries/opening-balances/preview",
    {
      method: "POST",
      body: JSON.stringify({
        entryDate: input.entryDate,
        currencyCode: input.currencyCode,
        sourceReference: optionalText(input.sourceReference),
        memo: optionalText(input.memo),
        profileFromDate: input.profileFromDate,
        profileToDate: input.profileToDate,
        profileStatus: input.profileStatus,
        transactionsAllowed: input.transactionsAllowed,
        profitAndLossCarryForwardAccountId: optionalText(
          input.profitAndLossCarryForwardAccountId
        ),
        lines: input.lines.map((line) => ({
          accountCode: line.accountCode,
          debit: amountOrZero(line.debit),
          credit: amountOrZero(line.credit),
          description: optionalText(line.description)
        }))
      })
    }
  );
}

export async function previewOpeningBalanceImportText(
  input: OpeningBalanceImportInput,
  importText: string,
  delimiter: string
): Promise<OpeningBalanceImportTextPreview> {
  return apiRequest<OpeningBalanceImportTextPreview>(
    "/api/v1/accounting/journal-entries/opening-balances/text-preview",
    {
      method: "POST",
      body: JSON.stringify({
        entryDate: input.entryDate,
        currencyCode: input.currencyCode,
        sourceReference: optionalText(input.sourceReference),
        memo: optionalText(input.memo),
        profileFromDate: input.profileFromDate,
        profileToDate: input.profileToDate,
        profileStatus: input.profileStatus,
        transactionsAllowed: input.transactionsAllowed,
        profitAndLossCarryForwardAccountId: optionalText(
          input.profitAndLossCarryForwardAccountId
        ),
        importText,
        delimiter: optionalText(delimiter)
      })
    }
  );
}

export async function postOpeningBalanceImport(
  input: OpeningBalanceImportInput
): Promise<JournalEntrySummary> {
  return apiRequest<JournalEntrySummary>(
    "/api/v1/accounting/journal-entries/opening-balances",
    {
      method: "POST",
      body: JSON.stringify({
        entryDate: input.entryDate,
        currencyCode: input.currencyCode,
        sourceReference: optionalText(input.sourceReference),
        memo: optionalText(input.memo),
        profileFromDate: input.profileFromDate,
        profileToDate: input.profileToDate,
        profileStatus: input.profileStatus,
        transactionsAllowed: input.transactionsAllowed,
        profitAndLossCarryForwardAccountId: optionalText(
          input.profitAndLossCarryForwardAccountId
        ),
        lines: input.lines.map((line) => ({
          accountCode: line.accountCode,
          debit: amountOrZero(line.debit),
          credit: amountOrZero(line.credit),
          description: optionalText(line.description)
        }))
      })
    }
  );
}

export async function voidManualJournalEntry(
  journalEntryId: string,
  input: VoidManualJournalEntryInput
): Promise<VoidManualJournalEntryResult> {
  return apiRequest<VoidManualJournalEntryResult>(
    `/api/v1/accounting/journal-entries/${encodeURIComponent(journalEntryId)}/void`,
    {
      method: "POST",
      body: JSON.stringify({
        voidDate: input.voidDate,
        reason: input.reason
      })
    }
  );
}

export async function getLedgerAccountActivity(
  ledgerAccountId: string,
  filters?: LedgerAccountActivityFilters
): Promise<LedgerAccountActivity> {
  const query = new URLSearchParams();

  if (filters !== undefined) {
    setQuery(query, "fromDate", filters.fromDate);
    setQuery(query, "toDate", filters.toDate);
    setQuery(query, "currencyCode", filters.currencyCode);
  }

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<LedgerAccountActivity>(
    `/api/v1/accounting/ledger-accounts/${encodeURIComponent(ledgerAccountId)}/activity${suffix}`
  );
}

export async function getTrialBalance(
  filters: TrialBalanceFilters
): Promise<TrialBalance> {
  const query = new URLSearchParams();
  setQuery(query, "fromDate", filters.fromDate);
  setQuery(query, "asOfDate", filters.asOfDate);
  setQuery(query, "currencyCode", filters.currencyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<TrialBalance>(`/api/v1/accounting/trial-balance${suffix}`);
}

export async function getProfitAndLossStatement(
  filters: ProfitAndLossStatementFilters
): Promise<ProfitAndLossStatement> {
  const query = new URLSearchParams();
  setQuery(query, "fromDate", filters.fromDate);
  setQuery(query, "toDate", filters.toDate);
  setQuery(query, "currencyCode", filters.currencyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<ProfitAndLossStatement>(`/api/v1/accounting/profit-and-loss${suffix}`);
}

export async function getBalanceSheet(
  filters: BalanceSheetFilters
): Promise<BalanceSheet> {
  const query = new URLSearchParams();
  setQuery(query, "asOfDate", filters.asOfDate);
  setQuery(query, "currencyCode", filters.currencyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<BalanceSheet>(`/api/v1/accounting/balance-sheet${suffix}`);
}

export async function configureAccountCodeRange(
  companyCode: string,
  role: string,
  input: AccountCodeRangeFormInput
): Promise<AccountCodeRange> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", companyCode);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;

  return apiRequest<AccountCodeRange>(
    `/api/v1/accounting/accounting-setup/account-code-ranges/${encodeURIComponent(role)}${suffix}`,
    {
      method: "PUT",
      body: JSON.stringify({
        displayName: input.displayName,
        searchPrefix: input.searchPrefix,
        rangeStart: input.rangeStart,
        rangeEnd: input.rangeEnd,
        codeLength: Number(input.codeLength),
        accountType: input.accountType,
        normalBalance: input.normalBalance,
        isPostingAccount: input.isPostingAccount,
        parentCode: optionalText(input.parentCode),
        isActive: input.isActive
      })
    }
  );
}

function setQuery(query: URLSearchParams, key: string, value?: string) {
  const trimmed = value?.trim() ?? "";

  if (trimmed !== "") {
    query.set(key, trimmed);
  }
}

function optionalText(value: string): string | undefined {
  const trimmed = value.trim();

  return trimmed === "" ? undefined : trimmed;
}

function amountOrZero(value: string): number {
  const amount = Number(value);

  return Number.isFinite(amount) ? amount : 0;
}

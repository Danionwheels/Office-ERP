import { apiRequest } from "../../../shared/api/httpClient";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  JournalEntryFilters,
  JournalEntrySummary,
  LedgerAccountActivity,
  LedgerAccountCodeSuggestion,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  VoidManualJournalEntryInput,
  VoidManualJournalEntryResult
} from "../types/accountingTypes";

type ListLedgerAccountsResponse = {
  companyCode: string;
  accounts: LedgerAccountSummary[];
};

type ListAccountCodeRangesResponse = {
  companyCode: string;
  ranges: AccountCodeRange[];
};

type ListJournalEntriesResponse = {
  entries: JournalEntrySummary[];
};

type LedgerAccountWriteResponse = {
  ledgerAccountId: string;
  code: string;
  name: string;
  type: string;
  normalBalance: string;
  parentAccountId?: string | null;
  isPostingAccount: boolean;
  status: string;
  createdAtUtc?: string;
};

export async function listLedgerAccounts(
  filters: LedgerAccountFilters
): Promise<LedgerAccountSummary[]> {
  const query = new URLSearchParams();
  setQuery(query, "companyCode", filters.companyCode);
  setQuery(query, "search", filters.search);
  setQuery(query, "type", filters.type);
  setQuery(query, "status", filters.status);
  setQuery(query, "role", filters.role);

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
      isPostingAccount: input.isPostingAccount
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
  companyCode: string
): Promise<LedgerAccountCodeSuggestion> {
  const query = new URLSearchParams({ role });
  setQuery(query, "companyCode", companyCode);

  return apiRequest<LedgerAccountCodeSuggestion>(
    `/api/v1/accounting/ledger-accounts/suggest-code?${query.toString()}`
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

export async function listJournalEntries(
  filters: JournalEntryFilters
): Promise<JournalEntrySummary[]> {
  const query = new URLSearchParams();
  setQuery(query, "fromDate", filters.fromDate);
  setQuery(query, "toDate", filters.toDate);
  setQuery(query, "sourceType", filters.sourceType);

  const suffix = query.toString() === "" ? "" : `?${query.toString()}`;
  const response = await apiRequest<ListJournalEntriesResponse>(
    `/api/v1/accounting/journal-entries${suffix}`
  );

  return response.entries;
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
  ledgerAccountId: string
): Promise<LedgerAccountActivity> {
  return apiRequest<LedgerAccountActivity>(
    `/api/v1/accounting/ledger-accounts/${encodeURIComponent(ledgerAccountId)}/activity`
  );
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

function setQuery(query: URLSearchParams, key: string, value: string) {
  const trimmed = value.trim();

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

import { apiRequest } from "../../../shared/api/httpClient";
import type {
  AccountsReceivableAgingFilters,
  AccountsReceivableAgingReport,
  LedgerAccountActivity,
  OutstandingInvoiceFilters,
  OutstandingInvoiceRow,
  OutstandingInvoicesReport,
  PaymentReceiptsFilters,
  PaymentReceiptRow,
  PaymentReceiptsReport,
  ReportClientDirectoryPage,
  ReportClientLookup,
  RevenueSummaryFilters,
  RevenueSummaryReport,
  TrialBalanceFilters,
  TrialBalanceReport
} from "../types/reportTypes";

export async function getAccountsReceivableAging(
  filters: AccountsReceivableAgingFilters
): Promise<AccountsReceivableAgingReport> {
  const query = createQuery({
    currencyCode: filters.currencyCode
  });

  return apiRequest<AccountsReceivableAgingReport>(
    `/api/v1/billing/reports/accounts-receivable-aging?${query}`
  );
}

export async function getRevenueSummary(
  filters: RevenueSummaryFilters
): Promise<RevenueSummaryReport> {
  const query = createQuery({
    fromDate: filters.fromDate,
    toDate: filters.toDate,
    period: filters.period,
    currencyCode: filters.currencyCode
  });

  return apiRequest<RevenueSummaryReport>(`/api/v1/accounting/revenue-summary?${query}`);
}

export async function getOutstandingInvoices(
  filters: OutstandingInvoiceFilters,
  cursor?: string | null
): Promise<OutstandingInvoicesReport> {
  const query = createQuery({
    clientId: filters.clientId,
    fromDate: filters.fromDate,
    toDate: filters.toDate,
    minAmount: filters.minAmount,
    maxAmount: filters.maxAmount,
    status: filters.status,
    currencyCode: filters.currencyCode,
    take: "100",
    cursor: cursor ?? ""
  });

  return apiRequest<OutstandingInvoicesReport>(
    `/api/v1/billing/reports/outstanding-invoices?${query}`
  );
}

export async function getPaymentReceipts(
  filters: PaymentReceiptsFilters,
  cursor?: string | null
): Promise<PaymentReceiptsReport> {
  const query = createQuery({
    clientId: filters.clientId,
    fromDate: filters.fromDate,
    toDate: filters.toDate,
    method: filters.method,
    status: filters.status,
    currencyCode: filters.currencyCode,
    take: "100",
    cursor: cursor ?? ""
  });

  return apiRequest<PaymentReceiptsReport>(`/api/v1/payments/reports/receipts?${query}`);
}

export async function getAllOutstandingInvoices(
  filters: OutstandingInvoiceFilters
): Promise<OutstandingInvoicesReport> {
  const invoices: OutstandingInvoiceRow[] = [];
  let cursor: string | null = null;
  let firstPage: OutstandingInvoicesReport | null = null;

  do {
    const page = await getOutstandingInvoices(filters, cursor);
    firstPage ??= page;
    invoices.push(...page.invoices);
    cursor = page.hasMore ? page.nextCursor ?? null : null;
  } while (cursor !== null);

  return {
    ...(firstPage ?? {
      invoices: [],
      pageSize: 0,
      hasMore: false,
      nextCursor: null,
      filteredCount: 0
    }),
    invoices,
    hasMore: false,
    nextCursor: null,
    filteredCount: firstPage?.filteredCount ?? invoices.length
  };
}

export async function getAllPaymentReceipts(
  filters: PaymentReceiptsFilters
): Promise<PaymentReceiptsReport> {
  const payments: PaymentReceiptRow[] = [];
  let cursor: string | null = null;
  let firstPage: PaymentReceiptsReport | null = null;

  do {
    const page = await getPaymentReceipts(filters, cursor);
    firstPage ??= page;
    payments.push(...page.payments);
    cursor = page.hasMore ? page.nextCursor ?? null : null;
  } while (cursor !== null);

  return {
    ...(firstPage ?? {
      payments: [],
      pageSize: 0,
      hasMore: false,
      nextCursor: null,
      filteredCount: 0
    }),
    payments,
    hasMore: false,
    nextCursor: null,
    filteredCount: firstPage?.filteredCount ?? payments.length
  };
}

export async function getTrialBalance(
  filters: TrialBalanceFilters
): Promise<TrialBalanceReport> {
  const query = createQuery({
    fromDate: filters.fromDate,
    asOfDate: filters.asOfDate,
    currencyCode: filters.currencyCode
  });

  return apiRequest<TrialBalanceReport>(`/api/v1/accounting/trial-balance?${query}`);
}

export async function getLedgerAccountActivity(
  ledgerAccountId: string,
  filters: TrialBalanceFilters
): Promise<LedgerAccountActivity> {
  const query = createQuery({
    fromDate: filters.fromDate,
    toDate: filters.asOfDate,
    currencyCode: filters.currencyCode
  });

  return apiRequest<LedgerAccountActivity>(
    `/api/v1/accounting/ledger-accounts/${encodeURIComponent(ledgerAccountId)}/activity?${query}`
  );
}

export async function listReportClients(searchText = ""): Promise<ReportClientLookup[]> {
  const query = createQuery({
    search: searchText,
    sort: "displayName",
    direction: "asc",
    take: "25"
  });
  const page = await apiRequest<ReportClientDirectoryPage>(`/api/v1/clients?${query}`);

  return page.clients;
}

function createQuery(values: Record<string, string>): string {
  const query = new URLSearchParams();

  Object.entries(values).forEach(([key, value]) => {
    const normalizedValue = value.trim();

    if (normalizedValue !== "") {
      query.set(key, normalizedValue);
    }
  });

  return query.toString();
}

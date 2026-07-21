import { useEffect, useRef, useState } from "react";
import {
  getAccountsReceivableAging,
  getAllOutstandingInvoices,
  getAllPaymentReceipts,
  getLedgerAccountActivity,
  getOutstandingInvoices,
  getPaymentReceipts,
  getRevenueSummary,
  getTrialBalance,
  listReportClients
} from "../api/reportApi";
import type {
  AccountsReceivableAgingFilters,
  AccountsReceivableAgingReport,
  LedgerAccountActivity,
  OutstandingInvoiceFilters,
  OutstandingInvoicesReport,
  PaymentReceiptsFilters,
  PaymentReceiptsReport,
  ReportClientLookup,
  ReportKey,
  RevenueSummaryFilters,
  RevenueSummaryReport,
  TrialBalanceFilters,
  TrialBalanceReport
} from "../types/reportTypes";
import {
  createAccountsReceivableAgingFilters,
  createOutstandingInvoiceFilters,
  createPaymentReceiptsFilters,
  createRevenueSummaryFilters,
  createTrialBalanceFilters
} from "../utils/reportDates";
import { formatReportError } from "../utils/reportFormatting";

type LoadKey = ReportKey | "clients" | "ledger";
type LoadState = Partial<Record<LoadKey, boolean>>;
type ErrorState = Partial<Record<LoadKey, string>>;

export function useReportsWorkspace() {
  const initialLoadsStarted = useRef(false);
  const requestGenerations = useRef<Record<LoadKey, number>>({
    aging: 0,
    revenue: 0,
    "outstanding-invoices": 0,
    "payment-receipts": 0,
    "trial-balance": 0,
    clients: 0,
    ledger: 0
  });
  const [activeReport, setActiveReport] = useState<ReportKey>("aging");
  const [busy, setBusy] = useState<LoadState>({});
  const [errors, setErrors] = useState<ErrorState>({});
  const [clients, setClients] = useState<ReportClientLookup[]>([]);

  const [agingFilters, setAgingFilters] = useState(createAccountsReceivableAgingFilters);
  const [revenueFilters, setRevenueFilters] = useState(createRevenueSummaryFilters);
  const [outstandingFilters, setOutstandingFilters] = useState(createOutstandingInvoiceFilters);
  const [receiptFilters, setReceiptFilters] = useState(createPaymentReceiptsFilters);
  const [trialBalanceFilters, setTrialBalanceFilters] = useState(createTrialBalanceFilters);

  const [aging, setAging] = useState<AccountsReceivableAgingReport | null>(null);
  const [revenue, setRevenue] = useState<RevenueSummaryReport | null>(null);
  const [outstanding, setOutstanding] = useState<OutstandingInvoicesReport | null>(null);
  const [receipts, setReceipts] = useState<PaymentReceiptsReport | null>(null);
  const [trialBalance, setTrialBalance] = useState<TrialBalanceReport | null>(null);
  const [selectedLedgerAccountId, setSelectedLedgerAccountId] = useState("");
  const [ledgerActivity, setLedgerActivity] = useState<LedgerAccountActivity | null>(null);

  useEffect(() => {
    if (initialLoadsStarted.current) {
      return;
    }

    initialLoadsStarted.current = true;
    void execute("clients", () => listReportClients(), (results) => {
      setClients((current) => mergeClientOptions(current, results));
    });
    void refreshAging();
  }, []);

  async function execute<T>(
    key: LoadKey,
    action: () => Promise<T>,
    onSuccess: (result: T) => void
  ): Promise<T | null> {
    const generation = requestGenerations.current[key] + 1;
    requestGenerations.current[key] = generation;
    setBusy((current) => ({ ...current, [key]: true }));
    setErrors((current) => ({ ...current, [key]: undefined }));

    try {
      const result = await action();

      if (requestGenerations.current[key] !== generation) {
        return null;
      }

      onSuccess(result);
      return result;
    } catch (caughtError) {
      if (requestGenerations.current[key] !== generation) {
        return null;
      }

      setErrors((current) => ({
        ...current,
        [key]: formatReportError(caughtError)
      }));
      return null;
    } finally {
      if (requestGenerations.current[key] === generation) {
        setBusy((current) => ({ ...current, [key]: false }));
      }
    }
  }

  function invalidateRequest(key: LoadKey): void {
    requestGenerations.current[key] += 1;
    setBusy((current) => ({ ...current, [key]: false }));
    setErrors((current) => ({ ...current, [key]: undefined }));
  }

  function selectReport(key: ReportKey): void {
    setActiveReport(key);

    if (key === "aging" && aging === null) {
      void refreshAging();
    } else if (key === "revenue" && revenue === null) {
      void refreshRevenue();
    } else if (key === "outstanding-invoices" && outstanding === null) {
      void refreshOutstanding();
    } else if (key === "payment-receipts" && receipts === null) {
      void refreshReceipts();
    } else if (key === "trial-balance" && trialBalance === null) {
      void refreshTrialBalance();
    }
  }

  async function refreshAging(): Promise<void> {
    await execute("aging", () => getAccountsReceivableAging(agingFilters), setAging);
  }

  async function refreshRevenue(): Promise<void> {
    await execute("revenue", () => getRevenueSummary(revenueFilters), setRevenue);
  }

  async function refreshOutstanding(): Promise<void> {
    await execute(
      "outstanding-invoices",
      () => getOutstandingInvoices(outstandingFilters),
      setOutstanding
    );
  }

  async function loadMoreOutstanding(): Promise<void> {
    if (!outstanding?.hasMore || !outstanding.nextCursor) {
      return;
    }

    await execute(
      "outstanding-invoices",
      () => getOutstandingInvoices(outstandingFilters, outstanding.nextCursor),
      (page) => setOutstanding((current) => mergeOutstandingPages(current, page))
    );
  }

  async function loadAllOutstanding(): Promise<OutstandingInvoicesReport | null> {
    return execute(
      "outstanding-invoices",
      () => getAllOutstandingInvoices(outstandingFilters),
      setOutstanding
    );
  }

  async function refreshReceipts(): Promise<void> {
    await execute(
      "payment-receipts",
      () => getPaymentReceipts(receiptFilters),
      setReceipts
    );
  }

  async function loadMoreReceipts(): Promise<void> {
    if (!receipts?.hasMore || !receipts.nextCursor) {
      return;
    }

    await execute(
      "payment-receipts",
      () => getPaymentReceipts(receiptFilters, receipts.nextCursor),
      (page) => setReceipts((current) => mergeReceiptPages(current, page))
    );
  }

  async function loadAllReceipts(): Promise<PaymentReceiptsReport | null> {
    return execute(
      "payment-receipts",
      () => getAllPaymentReceipts(receiptFilters),
      setReceipts
    );
  }

  async function refreshTrialBalance(): Promise<void> {
    invalidateRequest("ledger");
    setSelectedLedgerAccountId("");
    setLedgerActivity(null);

    await execute("trial-balance", () => getTrialBalance(trialBalanceFilters), (report) => {
      setTrialBalance(report);
    });
  }

  async function selectLedgerAccount(ledgerAccountId: string): Promise<void> {
    setSelectedLedgerAccountId(ledgerAccountId);
    setLedgerActivity(null);

    if (ledgerAccountId === "") {
      invalidateRequest("ledger");
      return;
    }

    await execute(
      "ledger",
      () => getLedgerAccountActivity(ledgerAccountId, trialBalanceFilters),
      setLedgerActivity
    );
  }

  async function searchClients(searchText: string): Promise<void> {
    await execute("clients", () => listReportClients(searchText), (results) => {
      setClients((current) => mergeClientOptions(current, results));
    });
  }

  function updateAgingFilters(filters: AccountsReceivableAgingFilters): void {
    invalidateRequest("aging");
    setAgingFilters(filters);
    setAging(null);
  }

  function updateRevenueFilters(filters: RevenueSummaryFilters): void {
    invalidateRequest("revenue");
    setRevenueFilters(filters);
    setRevenue(null);
  }

  function updateOutstandingFilters(filters: OutstandingInvoiceFilters): void {
    invalidateRequest("outstanding-invoices");
    setOutstandingFilters(filters);
    setOutstanding(null);
  }

  function updateReceiptFilters(filters: PaymentReceiptsFilters): void {
    invalidateRequest("payment-receipts");
    setReceiptFilters(filters);
    setReceipts(null);
  }

  function updateTrialBalanceFilters(filters: TrialBalanceFilters): void {
    invalidateRequest("trial-balance");
    invalidateRequest("ledger");
    setTrialBalanceFilters(filters);
    setTrialBalance(null);
    setSelectedLedgerAccountId("");
    setLedgerActivity(null);
  }

  return {
    activeReport,
    busy,
    errors,
    clients,
    agingFilters,
    setAgingFilters: updateAgingFilters,
    revenueFilters,
    setRevenueFilters: updateRevenueFilters,
    outstandingFilters,
    setOutstandingFilters: updateOutstandingFilters,
    receiptFilters,
    setReceiptFilters: updateReceiptFilters,
    trialBalanceFilters,
    setTrialBalanceFilters: updateTrialBalanceFilters,
    aging,
    revenue,
    outstanding,
    receipts,
    trialBalance,
    selectedLedgerAccountId,
    ledgerActivity,
    selectReport,
    refreshAging,
    refreshRevenue,
    refreshOutstanding,
    loadMoreOutstanding,
    loadAllOutstanding,
    refreshReceipts,
    loadMoreReceipts,
    loadAllReceipts,
    refreshTrialBalance,
    selectLedgerAccount,
    searchClients
  };
}

function mergeClientOptions(
  current: ReportClientLookup[],
  incoming: ReportClientLookup[]
): ReportClientLookup[] {
  const byId = new Map(current.map((client) => [client.clientId, client]));

  incoming.forEach((client) => byId.set(client.clientId, client));

  return [...byId.values()].sort((left, right) =>
    left.displayName.localeCompare(right.displayName)
  );
}

function mergeOutstandingPages(
  current: OutstandingInvoicesReport | null,
  page: OutstandingInvoicesReport
): OutstandingInvoicesReport {
  if (current === null) {
    return page;
  }

  const existingIds = new Set(current.invoices.map((invoice) => invoice.invoiceId));
  return {
    ...page,
    invoices: [
      ...current.invoices,
      ...page.invoices.filter((invoice) => !existingIds.has(invoice.invoiceId))
    ],
    filteredCount: page.filteredCount ?? current.filteredCount
  };
}

function mergeReceiptPages(
  current: PaymentReceiptsReport | null,
  page: PaymentReceiptsReport
): PaymentReceiptsReport {
  if (current === null) {
    return page;
  }

  const existingIds = new Set(current.payments.map((payment) => payment.paymentId));
  return {
    ...page,
    payments: [
      ...current.payments,
      ...page.payments.filter((payment) => !existingIds.has(payment.paymentId))
    ],
    filteredCount: page.filteredCount ?? current.filteredCount
  };
}

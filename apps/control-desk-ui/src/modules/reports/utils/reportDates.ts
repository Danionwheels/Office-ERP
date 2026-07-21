import type {
  AccountsReceivableAgingFilters,
  OutstandingInvoiceFilters,
  PaymentReceiptsFilters,
  RevenueSummaryFilters,
  TrialBalanceFilters
} from "../types/reportTypes";

export const defaultReportCurrencyCode = "PKR";

export function createAccountsReceivableAgingFilters(): AccountsReceivableAgingFilters {
  return {
    asOfDate: toDateInputValue(new Date()),
    currencyCode: defaultReportCurrencyCode
  };
}

export function createRevenueSummaryFilters(): RevenueSummaryFilters {
  const range = createCurrentMonthRange();

  return {
    ...range,
    period: "Monthly",
    currencyCode: defaultReportCurrencyCode
  };
}

export function createOutstandingInvoiceFilters(): OutstandingInvoiceFilters {
  return {
    clientId: "",
    ...createCurrentMonthRange(),
    minAmount: "",
    maxAmount: "",
    status: "",
    currencyCode: defaultReportCurrencyCode
  };
}

export function createPaymentReceiptsFilters(): PaymentReceiptsFilters {
  return {
    clientId: "",
    ...createCurrentMonthRange(),
    method: "",
    status: "",
    currencyCode: defaultReportCurrencyCode
  };
}

export function createTrialBalanceFilters(): TrialBalanceFilters {
  const range = createCurrentMonthRange();

  return {
    fromDate: range.fromDate,
    asOfDate: range.toDate,
    currencyCode: defaultReportCurrencyCode
  };
}

function createCurrentMonthRange(): { fromDate: string; toDate: string } {
  const today = new Date();
  const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);

  return {
    fromDate: toDateInputValue(monthStart),
    toDate: toDateInputValue(today)
  };
}

function toDateInputValue(value: Date): string {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

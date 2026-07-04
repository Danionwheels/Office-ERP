export const accountingCompanyCode = "MAIN";
export const accountingCurrencyCode = "PKR";

export const accountTypeOptions = ["Asset", "Liability", "Equity", "Revenue", "Expense"];
export const normalBalanceOptions = ["Debit", "Credit"];

export type LegacyAccountLevelCode = "H" | "T" | "M" | "D" | "C" | "S";

export type LegacyAccountLevel = {
  code: LegacyAccountLevelCode;
  label: string;
};

export const legacyAccountLevels: LegacyAccountLevel[] = [
  { code: "H", label: "Header" },
  { code: "T", label: "Total" },
  { code: "M", label: "Master" },
  { code: "D", label: "Detail" },
  { code: "C", label: "Control" },
  { code: "S", label: "Subsidiary" }
];

export const journalSourceTypeOptions = [
  "Manual",
  "BillingInvoice",
  "PaymentReceipt",
  "OpeningBalance",
  "Adjustment",
  "ManualReversal",
  "PaymentReversal",
  "BillingInvoiceVoid",
  "BillingCreditNote",
  "ClientRefund"
];

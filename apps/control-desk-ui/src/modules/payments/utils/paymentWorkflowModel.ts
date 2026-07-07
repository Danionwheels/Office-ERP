import {
  AlertCircle,
  ArrowRightLeft,
  Banknote,
  CheckCircle2,
  Landmark,
  Receipt
} from "lucide-react";
import type { InvoiceDraft, IssuedInvoice } from "../../billing/types/billingTypes";
import type { ClientAccountingProfile } from "../../clients/types/clientTypes";
import type { ClientStatement } from "../../statements/types/statementTypes";
import type {
  AppliedClientCredit,
  IssuedClientRefund,
  RecordedInvoicePayment
} from "../types/paymentTypes";
import type {
  PaymentPostingBridgeItem,
  PaymentStep,
  PaymentStepItem,
  RefundCreditSummary,
  SettlementCreditSummary
} from "../types/paymentWorkflowTypes";

type PaymentPostingBridgeInput = {
  accountsReceivableAccountId: string;
  amount: number;
  cashOrBankAccountId: string;
  currencyCode: string;
  documentDetail: string;
  documentLabel: string;
  journalEntryId?: string | null;
  journalStatus?: string | null;
  postingDate: string;
  postingVerb: string;
};

type PaymentStepInput = {
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
  accountingProfile: ClientAccountingProfile | null;
  hasCashAccount: boolean;
  hasReceivableAccount: boolean;
  recordedPayment: RecordedInvoicePayment | null;
  issuedRefund: IssuedClientRefund | null;
  appliedCredit: AppliedClientCredit | null;
  refundCredit: RefundCreditSummary;
  settlementCredit: SettlementCreditSummary;
  canRecordPayment: boolean;
};

export function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

export function toDateInputValue(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function getPaymentPostingBridgeItems({
  accountsReceivableAccountId,
  amount,
  cashOrBankAccountId,
  currencyCode,
  documentDetail,
  documentLabel,
  journalEntryId,
  journalStatus,
  postingDate,
  postingVerb
}: PaymentPostingBridgeInput): PaymentPostingBridgeItem[] {
  const normalizedCashAccountId = cleanOptional(cashOrBankAccountId);
  const normalizedReceivableAccountId = cleanOptional(accountsReceivableAccountId);
  const normalizedJournalId = cleanOptional(journalEntryId);
  const normalizedJournalStatus = cleanOptional(journalStatus);
  const normalizedPostingDate = cleanOptional(postingDate);
  const hasAmount = Number.isFinite(amount) && amount > 0;

  return [
    {
      label: "Document",
      value: documentLabel,
      detail: documentDetail,
      tone: documentLabel === "No invoice" ? "warning" : "ready"
    },
    {
      label: "Cash/bank",
      value: normalizedCashAccountId === null ? "Missing" : shortAccountId(normalizedCashAccountId),
      detail: `${postingVerb} debits cash or bank`,
      tone: normalizedCashAccountId === null ? "warning" : "ready"
    },
    {
      label: "AR control",
      value: normalizedReceivableAccountId === null ? "Missing" : shortAccountId(normalizedReceivableAccountId),
      detail: `${postingVerb} clears receivable`,
      tone: normalizedReceivableAccountId === null ? "warning" : "ready"
    },
    {
      label: "Amount",
      value: hasAmount ? formatMoney(amount, currencyCode) : "Missing",
      detail: normalizedPostingDate === null ? "Posting date required" : `Posts ${normalizedPostingDate}`,
      tone: hasAmount && normalizedPostingDate !== null ? "ready" : "warning"
    },
    {
      label: "Journal",
      value: normalizedJournalStatus ?? "Not posted",
      detail: normalizedJournalId === null ? "Journal created after approval/posting" : shortAccountId(normalizedJournalId),
      tone: normalizedJournalId === null ? "neutral" : "ready"
    }
  ];
}

export function getPaymentStepItems({
  invoiceDraft,
  issuedInvoice,
  accountingProfile,
  hasCashAccount,
  hasReceivableAccount,
  recordedPayment,
  issuedRefund,
  appliedCredit,
  refundCredit,
  settlementCredit,
  canRecordPayment
}: PaymentStepInput): PaymentStepItem[] {
  const balanceSummary = invoiceDraft === null
    ? "No invoice"
    : formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode);
  const resultSummary = getPostingResultSummary(recordedPayment, issuedRefund, appliedCredit);

  return [
    {
      step: "readiness",
      label: "Readiness",
      summary: canRecordPayment ? "Ready to post" : getReadinessSummary({
        invoiceDraft,
        issuedInvoice,
        accountingProfile,
        hasCashAccount,
        hasReceivableAccount
      }),
      tone: canRecordPayment ? "ready" : "warning",
      Icon: AlertCircle
    },
    {
      step: "cash",
      label: "Cash account",
      summary: hasCashAccount ? "Linked" : "Missing",
      tone: hasCashAccount ? "ready" : "warning",
      Icon: Banknote
    },
    {
      step: "receipt",
      label: "Receipt",
      summary: balanceSummary,
      tone: canRecordPayment ? "ready" : "neutral",
      Icon: Receipt
    },
    {
      step: "settlement",
      label: "Settlement",
      summary: settlementCredit.availableCredit > 0
        ? formatMoney(settlementCredit.availableCredit, settlementCredit.currencyCode)
        : "No credit",
      tone: settlementCredit.availableCredit > 0 ? "ready" : "neutral",
      Icon: ArrowRightLeft
    },
    {
      step: "refund",
      label: "Refund",
      summary: refundCredit.availableCredit > 0
        ? formatMoney(refundCredit.availableCredit, refundCredit.currencyCode)
        : "No credit",
      tone: refundCredit.availableCredit > 0 ? "ready" : "neutral",
      Icon: Landmark
    },
    {
      step: "result",
      label: "Posting result",
      summary: resultSummary,
      tone: recordedPayment === null && issuedRefund === null && appliedCredit === null ? "neutral" : "ready",
      Icon: CheckCircle2
    }
  ];
}

export function getPaymentStepCue(step: PaymentStep): string {
  switch (step) {
    case "readiness":
      return "Confirm invoice, AR, cash, and balance";
    case "cash":
      return "Create or link the cash/bank posting account";
    case "receipt":
      return "Record payment against the issued invoice";
    case "settlement":
      return "Apply unapplied credit to open balance";
    case "refund":
      return "Return available client credit";
    case "result":
      return "Review posting, approval, reversal, and journal";
  }
}

export function getRefundCredit(
  statement: ClientStatement | null,
  preferredCurrencyCode: string
): RefundCreditSummary {
  const summaries = statement?.currencySummaries ?? [];
  const preferredSummary = summaries.find((summary) =>
    summary.currencyCode.toLowerCase() === preferredCurrencyCode.toLowerCase()
  );
  const creditSummary =
    preferredSummary !== undefined && preferredSummary.balanceDue < 0
      ? preferredSummary
      : summaries.find((summary) => summary.balanceDue < 0);
  const preferredCurrency = preferredCurrencyCode.trim().toUpperCase();
  const currencyCode = creditSummary?.currencyCode ?? (preferredCurrency === "" ? "PKR" : preferredCurrency);
  const balanceDue = creditSummary?.balanceDue ?? preferredSummary?.balanceDue ?? 0;

  return {
    currencyCode,
    balanceDue,
    availableCredit: balanceDue < 0 ? Math.abs(balanceDue) : 0
  };
}

export function getSettlementCredit(
  statement: ClientStatement | null,
  preferredCurrencyCode: string
): SettlementCreditSummary {
  const summaries = statement?.currencySummaries ?? [];
  const preferredSummary = summaries.find((summary) =>
    summary.currencyCode.toLowerCase() === preferredCurrencyCode.toLowerCase()
  );
  const creditSummary =
    preferredSummary !== undefined && preferredSummary.availableCredit > 0
      ? preferredSummary
      : summaries.find((summary) => summary.availableCredit > 0);
  const preferredCurrency = preferredCurrencyCode.trim().toUpperCase();
  const currencyCode = creditSummary?.currencyCode ?? (preferredCurrency === "" ? "PKR" : preferredCurrency);

  return {
    currencyCode,
    availableCredit: creditSummary?.availableCredit ?? preferredSummary?.availableCredit ?? 0
  };
}

export function statusClass(value: string): string {
  return value.toLowerCase().replaceAll(" ", "");
}

export function cleanOptional(value?: string | null): string | null {
  const trimmed = value?.trim() ?? "";

  return trimmed === "" ? null : trimmed;
}

export function shortAccountId(value?: string | null): string {
  const normalizedValue = cleanOptional(value);

  return normalizedValue === null ? "-" : normalizedValue.slice(0, 8);
}

function getPostingResultSummary(
  recordedPayment: RecordedInvoicePayment | null,
  issuedRefund: IssuedClientRefund | null,
  appliedCredit: AppliedClientCredit | null
): string {
  if (appliedCredit !== null) {
    return `${appliedCredit.creditApplicationStatus} ${formatMoney(
      appliedCredit.invoiceBalanceAfter,
      appliedCredit.currencyCode
    )}`;
  }

  if (issuedRefund !== null) {
    return `${issuedRefund.refundStatus} ${formatMoney(
      issuedRefund.clientBalanceAfter,
      issuedRefund.currencyCode
    )}`;
  }

  if (recordedPayment !== null) {
    return `${recordedPayment.invoiceStatus} ${formatMoney(
      recordedPayment.balanceDue,
      recordedPayment.currencyCode
    )}`;
  }

  return "Pending";
}

function getReadinessSummary({
  invoiceDraft,
  issuedInvoice,
  accountingProfile,
  hasCashAccount,
  hasReceivableAccount
}: Pick<
  PaymentStepInput,
  "invoiceDraft" | "issuedInvoice" | "accountingProfile" | "hasCashAccount" | "hasReceivableAccount"
>): string {
  if (invoiceDraft === null) {
    return "No invoice";
  }

  if (issuedInvoice === null) {
    return "Issue invoice";
  }

  if (invoiceDraft.balanceDue <= 0) {
    return "Paid";
  }

  if (accountingProfile === null || !hasReceivableAccount) {
    return "AR missing";
  }

  if (!hasCashAccount) {
    return "Cash missing";
  }

  return "Review";
}

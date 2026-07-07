import type { LucideIcon } from "lucide-react";
import type {
  InvoiceDraft,
  IssuedInvoice,
  LedgerAccountFormInput
} from "../../billing/types/billingTypes";
import type { ClientAccountingProfile } from "../../clients/types/clientTypes";
import type { ClientStatement } from "../../statements/types/statementTypes";
import type {
  AppliedClientCredit,
  ApplyClientCreditInput,
  IssueClientRefundInput,
  IssuedClientRefund,
  RecordedInvoicePayment,
  RecordedInvoicePaymentJournalLine,
  RecordInvoicePaymentInput
} from "./paymentTypes";

export type PaymentStep = "readiness" | "cash" | "receipt" | "settlement" | "refund" | "result";

export type PaymentReceiptPanelProps = {
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
  initialStep?: PaymentStep;
  accountingProfile: ClientAccountingProfile | null;
  cashAccountValue: LedgerAccountFormInput;
  paymentValue: RecordInvoicePaymentInput;
  refundValue: IssueClientRefundInput;
  creditApplicationValue: ApplyClientCreditInput;
  recordedPayment: RecordedInvoicePayment | null;
  issuedRefund: IssuedClientRefund | null;
  appliedCredit: AppliedClientCredit | null;
  clientStatement: ClientStatement | null;
  isBusy: boolean;
  onCashAccountChange: (value: LedgerAccountFormInput) => void;
  onPaymentChange: (value: RecordInvoicePaymentInput) => void;
  onRefundChange: (value: IssueClientRefundInput) => void;
  onCreditApplicationChange: (value: ApplyClientCreditInput) => void;
  onCreateCashAccount: () => Promise<void>;
  onRecordPayment: () => Promise<void>;
  onIssueRefund: () => Promise<void>;
  onApplyCredit: () => Promise<void>;
  onApprovePayment: (decisionNote: string) => Promise<void>;
  onRejectPayment: (decisionNote: string) => Promise<void>;
  onReversePayment: (decisionNote: string, reversalDate: string) => Promise<void>;
  onViewJournalEntry: (journalEntryId: string) => Promise<void>;
};

export type PaymentStepItem = {
  step: PaymentStep;
  label: string;
  summary: string;
  tone: "neutral" | "ready" | "warning";
  Icon: LucideIcon;
};

export type PaymentPostingBridgeItem = {
  label: string;
  value: string;
  detail: string;
  tone: "neutral" | "ready" | "warning";
};

export type PaymentJournalLine = RecordedInvoicePaymentJournalLine;

export type RefundCreditSummary = {
  currencyCode: string;
  balanceDue: number;
  availableCredit: number;
};

export type SettlementCreditSummary = {
  currencyCode: string;
  availableCredit: number;
};

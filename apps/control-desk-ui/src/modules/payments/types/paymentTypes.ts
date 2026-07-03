import type { InvoiceDraft } from "../../billing/types/billingTypes";

export type RecordInvoicePaymentInput = {
  invoiceId: string;
  method: string;
  reference: string;
  amount: string;
  currencyCode: string;
  receivedOn: string;
  cashOrBankAccountId: string;
  accountsReceivableAccountId: string;
  postingDate: string;
};

export type RecordedInvoicePayment = {
  paymentId: string;
  invoiceId: string;
  invoiceNumber: string;
  invoiceStatus: string;
  paymentStatus: string;
  amount: number;
  balanceDue: number;
  currencyCode: string;
  journalEntryId?: string | null;
  journalEntryStatus?: string | null;
  postingDate?: string | null;
  totalDebit: number;
  totalCredit: number;
  journalLines: RecordedInvoicePaymentJournalLine[];
  decisionNote?: string | null;
};

export type InvoicePaymentDocument = {
  invoice: InvoiceDraft;
  payment: RecordedInvoicePayment;
  reversal?: ReversedInvoicePayment | null;
};

export type RecordedInvoicePaymentJournalLine = {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  description?: string | null;
};

export type ApproveInvoicePaymentInput = {
  cashOrBankAccountId: string;
  accountsReceivableAccountId: string;
  postingDate: string;
  decisionNote?: string | null;
};

export type RejectInvoicePaymentResult = {
  paymentId: string;
  invoiceId: string;
  paymentStatus: string;
  decisionNote?: string | null;
};

export type ReverseInvoicePaymentInput = {
  reversalDate: string;
  decisionNote: string;
};

export type ReversedInvoicePayment = {
  paymentId: string;
  invoiceId: string;
  invoiceNumber: string;
  invoiceStatus: string;
  paymentStatus: string;
  amount: number;
  balanceDue: number;
  currencyCode: string;
  reversalJournalEntryId: string;
  reversalJournalEntryStatus: string;
  reversalDate: string;
  originalJournalEntryId: string;
  totalDebit: number;
  totalCredit: number;
  journalLines: RecordedInvoicePaymentJournalLine[];
};

export type IssueClientRefundInput = {
  clientId: string;
  method: string;
  reference: string;
  amount: string;
  currencyCode: string;
  refundedOn: string;
  cashOrBankAccountId: string;
  accountsReceivableAccountId: string;
  postingDate: string;
  note: string;
};

export type IssuedClientRefund = {
  refundId: string;
  clientId: string;
  refundStatus: string;
  method: string;
  reference: string;
  amount: number;
  clientBalanceBefore: number;
  clientBalanceAfter: number;
  currencyCode: string;
  refundedOn: string;
  journalEntryId: string;
  journalEntryStatus: string;
  postingDate: string;
  totalDebit: number;
  totalCredit: number;
  journalLines: RecordedInvoicePaymentJournalLine[];
};

export type ClientRefundDocument = {
  refund: IssuedClientRefund;
};

export type ApplyClientCreditInput = {
  clientId: string;
  invoiceId: string;
  reference: string;
  amount: string;
  currencyCode: string;
  appliedOn: string;
  note: string;
};

export type AppliedClientCredit = {
  creditApplicationId: string;
  clientId: string;
  invoiceId: string;
  invoiceNumber: string;
  invoiceStatus: string;
  reference: string;
  amount: number;
  invoiceBalanceBefore: number;
  invoiceBalanceAfter: number;
  availableCreditBefore: number;
  availableCreditAfter: number;
  clientBalanceBefore: number;
  clientBalanceAfter: number;
  currencyCode: string;
  appliedOn: string;
  creditApplicationStatus: string;
};

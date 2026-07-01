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
  journalEntryId: string;
  journalEntryStatus: string;
  postingDate: string;
  totalDebit: number;
  totalCredit: number;
  journalLines: RecordedInvoicePaymentJournalLine[];
};

export type RecordedInvoicePaymentJournalLine = {
  ledgerAccountId: string;
  debit: number;
  credit: number;
  description?: string | null;
};

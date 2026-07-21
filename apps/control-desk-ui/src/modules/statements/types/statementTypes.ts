export type ClientStatement = {
  clientId: string;
  fromDate?: string | null;
  toDate?: string | null;
  currencySummaries: ClientStatementCurrencySummary[];
  invoices: ClientStatementInvoice[];
  payments: ClientStatementPayment[];
  lines: ClientStatementLine[];
  journalPostings: ClientStatementJournalPosting[];
  registers: ClientStatementRegisters;
};

export type ClientStatementRegister = "invoices" | "payments" | "lines" | "journalPostings";

export type ClientStatementPageMeta = {
  pageSize: number;
  hasMore: boolean;
  nextCursor?: string | null;
  filteredCount: number;
};

export type ClientStatementRegisters = Record<ClientStatementRegister, ClientStatementPageMeta>;

export type ClientStatementCurrencySummary = {
  currencyCode: string;
  totalInvoiced: number;
  totalPaid: number;
  availableCredit: number;
  balanceDue: number;
  invoiceCount: number;
  openInvoiceCount: number;
};

export type ClientStatementInvoice = {
  invoiceId: string;
  contractId: string;
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  status: string;
  totalAmount: number;
  amountPaid: number;
  balanceDue: number;
  currencyCode: string;
  journalEntryId?: string | null;
};

export type ClientStatementPayment = {
  paymentId: string;
  invoiceId: string;
  reference: string;
  method: string;
  status: string;
  amount: number;
  currencyCode: string;
  receivedOn: string;
  journalEntryId?: string | null;
};

export type ClientStatementLine = {
  entryDate: string;
  documentType: string;
  reference: string;
  invoiceId?: string | null;
  paymentId?: string | null;
  refundId?: string | null;
  creditApplicationId?: string | null;
  description: string;
  debit: number;
  credit: number;
  runningBalance: number;
  currencyCode: string;
  journalEntryId?: string | null;
};

export type ClientStatementJournalPosting = {
  journalEntryId: string;
  entryDate: string;
  sourceType: string;
  sourceReference?: string | null;
  memo?: string | null;
  status: string;
  totalDebit: number;
  totalCredit: number;
  currencyCode: string;
  lineCount: number;
};

import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ClientStatement,
  ClientStatementInvoice,
  ClientStatementJournalPosting,
  ClientStatementLine,
  ClientStatementPageMeta,
  ClientStatementPayment,
  ClientStatementRegister
} from "../types/statementTypes";

type FinancialSummaryResponse = Pick<
  ClientStatement,
  "clientId" | "fromDate" | "toDate" | "currencySummaries"
>;

type InvoicePageResponse = ClientStatementPageMeta & {
  invoices: ClientStatementInvoice[];
};

type PaymentPageResponse = ClientStatementPageMeta & {
  payments: ClientStatementPayment[];
};

type ActivityPageResponse = ClientStatementPageMeta & {
  lines: ClientStatementLine[];
};

type JournalPageResponse = ClientStatementPageMeta & {
  journalPostings: ClientStatementJournalPosting[];
};

export async function getClientStatement(clientId: string): Promise<ClientStatement> {
  const encodedClientId = encodeURIComponent(clientId);
  const [summary, invoices, payments, activity, journals] = await Promise.all([
    apiRequest<FinancialSummaryResponse>(`/api/v1/clients/${encodedClientId}/financial-summary`),
    apiRequest<InvoicePageResponse>(`/api/v1/clients/${encodedClientId}/invoices?take=25`),
    apiRequest<PaymentPageResponse>(`/api/v1/clients/${encodedClientId}/payments?take=25`),
    apiRequest<ActivityPageResponse>(`/api/v1/clients/${encodedClientId}/financial-activity?take=25`),
    apiRequest<JournalPageResponse>(`/api/v1/clients/${encodedClientId}/journal-postings?take=20`)
  ]);

  return {
    ...summary,
    invoices: invoices.invoices,
    payments: payments.payments,
    lines: activity.lines,
    journalPostings: journals.journalPostings,
    registers: {
      invoices: toPageMeta(invoices),
      payments: toPageMeta(payments),
      lines: toPageMeta(activity),
      journalPostings: toPageMeta(journals)
    }
  };
}

export async function loadMoreClientStatement(
  clientId: string,
  statement: ClientStatement,
  register: ClientStatementRegister
): Promise<ClientStatement> {
  const page = statement.registers[register];

  if (!page.hasMore || page.nextCursor === null || page.nextCursor === undefined) {
    return statement;
  }

  const encodedClientId = encodeURIComponent(clientId);
  const cursor = encodeURIComponent(page.nextCursor);

  if (register === "invoices") {
    const response = await apiRequest<InvoicePageResponse>(
      `/api/v1/clients/${encodedClientId}/invoices?take=${page.pageSize}&cursor=${cursor}`
    );
    return mergePage(statement, register, response.invoices, response);
  }

  if (register === "payments") {
    const response = await apiRequest<PaymentPageResponse>(
      `/api/v1/clients/${encodedClientId}/payments?take=${page.pageSize}&cursor=${cursor}`
    );
    return mergePage(statement, register, response.payments, response);
  }

  if (register === "lines") {
    const response = await apiRequest<ActivityPageResponse>(
      `/api/v1/clients/${encodedClientId}/financial-activity?take=${page.pageSize}&cursor=${cursor}`
    );
    return mergePage(statement, register, response.lines, response);
  }

  const response = await apiRequest<JournalPageResponse>(
    `/api/v1/clients/${encodedClientId}/journal-postings?take=${page.pageSize}&cursor=${cursor}`
  );
  return mergePage(statement, register, response.journalPostings, response);
}

function mergePage<T extends ClientStatementRegister>(
  statement: ClientStatement,
  register: T,
  items: ClientStatement[T],
  page: ClientStatementPageMeta
): ClientStatement {
  return {
    ...statement,
    [register]: [...statement[register], ...items],
    registers: {
      ...statement.registers,
      [register]: toPageMeta(page)
    }
  };
}

function toPageMeta(page: ClientStatementPageMeta): ClientStatementPageMeta {
  return {
    pageSize: page.pageSize,
    hasMore: page.hasMore,
    nextCursor: page.nextCursor,
    filteredCount: page.filteredCount
  };
}

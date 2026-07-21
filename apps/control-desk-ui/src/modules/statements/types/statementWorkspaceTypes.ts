import type { ClientDetails } from "../../clients/types/clientTypes";
import type { ClientStatement, ClientStatementRegister } from "./statementTypes";

export type ClientStatementPanelProps = {
  client: ClientDetails | null;
  statement: ClientStatement | null;
  isBusy: boolean;
  onRefresh: () => Promise<void>;
  onLoadMore: (register: ClientStatementRegister) => Promise<void>;
};

export type StatementTone = "neutral" | "ready" | "warning";

export type StatementControlKey =
  | "period"
  | "balance"
  | "invoices"
  | "receipts"
  | "lines"
  | "postings";

export type StatementControlRow = {
  key: StatementControlKey;
  label: string;
  status: string;
  detail: string;
  tone: StatementTone;
};

export type StatementLedgerBridgeItem = {
  label: string;
  value: string;
  detail: string;
  tone: StatementTone;
};

export type StatementTotals = {
  balanceSummary: string;
  invoiceCount: number;
  openInvoiceCount: number;
  paidSummary: string;
};

import { ChevronDown, RefreshCw } from "lucide-react";
import type { ClientStatement, ClientStatementRegister } from "../types/statementTypes";
import type { ClientStatementPanelProps } from "../types/statementWorkspaceTypes";
import {
  StatementControlBoard,
  StatementCurrencyBoard,
  StatementInvoiceRegister,
  StatementJournalPostingRegister,
  StatementLedgerBridge,
  StatementLineRegister,
  StatementPaymentRegister,
  StatementStatePanel
} from "./shared/StatementWorkspace";

export function ClientStatementPanel({
  client,
  statement,
  isBusy,
  onRefresh,
  onLoadMore
}: ClientStatementPanelProps) {
  if (client === null) {
    return (
      <StatementStatePanel
        eyebrow="Client statement"
        title="Select a client"
        detail="Receivables, receipts, running balances, and journal links are shown together."
        panel
      />
    );
  }

  return (
    <section className="statement-workspace">
      <header className="statement-header client-panel">
        <div>
          <span>Receivables</span>
          <h2>Client statement</h2>
          <small>{client.code} / {client.displayName}</small>
        </div>
        <button className="icon-button" type="button" onClick={onRefresh} disabled={isBusy} title="Refresh statement">
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      {statement === null ? (
        <StatementStatePanel
          eyebrow="Statement status"
          title="No receivables statement loaded"
          detail={`${client.code} / ${client.displayName}`}
          icon="ledger"
          panel
        />
      ) : (
        <>
          <StatementControlBoard statement={statement} />

          <StatementCurrencyBoard statement={statement} />

          <StatementLedgerBridge statement={statement} />

          <StatementLineRegister statement={statement} />
          <RegisterContinuation
            isBusy={isBusy}
            register="lines"
            statement={statement}
            onLoadMore={onLoadMore}
          />

          <div className="statement-detail-grid">
            <div className="client-panel statement-list-panel">
              <div className="client-panel-heading">
                <div>
                  <span>Documents</span>
                  <strong>Invoices</strong>
                </div>
                <span className="billing-small-fact">
                  {statement.invoices.length} / {statement.registers.invoices.filteredCount}
                </span>
              </div>
              <StatementInvoiceRegister statement={statement} />
              <RegisterContinuation
                isBusy={isBusy}
                register="invoices"
                statement={statement}
                onLoadMore={onLoadMore}
              />
            </div>

            <div className="client-panel statement-list-panel">
              <div className="client-panel-heading">
                <div>
                  <span>Receipts</span>
                  <strong>Payments</strong>
                </div>
                <span className="billing-small-fact">
                  {statement.payments.length} / {statement.registers.payments.filteredCount}
                </span>
              </div>
              <StatementPaymentRegister statement={statement} />
              <RegisterContinuation
                isBusy={isBusy}
                register="payments"
                statement={statement}
                onLoadMore={onLoadMore}
              />
            </div>
          </div>

          <div className="client-panel statement-journal-panel">
            <div className="client-panel-heading">
              <div>
                <span>Accounting</span>
                <strong>Journal postings</strong>
              </div>
              <span className="billing-small-fact">
                {statement.journalPostings.length} / {statement.registers.journalPostings.filteredCount}
              </span>
            </div>
            <StatementJournalPostingRegister statement={statement} />
            <RegisterContinuation
              isBusy={isBusy}
              register="journalPostings"
              statement={statement}
              onLoadMore={onLoadMore}
            />
          </div>
        </>
      )}
    </section>
  );
}

function RegisterContinuation({
  isBusy,
  register,
  statement,
  onLoadMore
}: {
  isBusy: boolean;
  register: ClientStatementRegister;
  statement: ClientStatement;
  onLoadMore: (register: ClientStatementRegister) => Promise<void>;
}) {
  const page = statement.registers[register];

  if (!page.hasMore) {
    return null;
  }

  return (
    <div className="statement-register-continuation">
      <button
        className="icon-button"
        type="button"
        disabled={isBusy}
        onClick={() => void onLoadMore(register)}
      >
        <ChevronDown size={15} />
        Load more
      </button>
    </div>
  );
}

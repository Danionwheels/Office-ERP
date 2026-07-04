import {
  CalendarCheck2,
  ExternalLink,
  ListTree,
  Plus,
  RefreshCw,
  Send,
  Trash2,
  Undo2,
  X
} from "lucide-react";
import { Fragment, type FormEvent } from "react";
import type {
  AccountingPeriod,
  JournalEntryFilters,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  ManualJournalEntryLineInput
} from "../types/accountingTypes";
import { journalSourceTypeOptions } from "../constants/accountingConstants";
import { toDateInputValue } from "../utils/accountingDates";
import {
  amount,
  formatAccount,
  formatMoney,
  formatSourceAmount,
  getPostingPeriodState,
  roundMoney,
  sourceDocumentTitle,
  voidManualJournalTitle
} from "../utils/journalModel";

type JournalWorkbenchPanelProps = {
  accounts: LedgerAccountSummary[];
  periods: AccountingPeriod[];
  entries: JournalEntrySummary[];
  filters: JournalEntryFilters;
  value: ManualJournalEntryInput;
  focusedJournalEntryId: string;
  focusedJournalEntry: JournalEntrySummary | null;
  sourceDocumentsByJournalEntryId: Record<string, JournalEntrySourceDocument>;
  isBusy: boolean;
  onFiltersChange: (value: JournalEntryFilters) => void;
  onValueChange: (value: ManualJournalEntryInput) => void;
  onFocusJournalEntry: (journalEntryId: string) => Promise<void>;
  onPost: () => Promise<void>;
  onVoidEntry: (entry: JournalEntrySummary) => Promise<void>;
  onOpenSourceDocument: (entry: JournalEntrySummary) => Promise<void>;
  getSourceDocumentLabel: (entry: JournalEntrySummary) => string | null;
  getSourceDocumentClientLabel: (sourceDocument: JournalEntrySourceDocument) => string;
  onRefresh: () => Promise<void>;
};

export function JournalWorkbenchPanel({
  accounts,
  periods,
  entries,
  filters,
  value,
  focusedJournalEntryId,
  focusedJournalEntry,
  sourceDocumentsByJournalEntryId,
  isBusy,
  onFiltersChange,
  onValueChange,
  onFocusJournalEntry,
  onPost,
  onVoidEntry,
  onOpenSourceDocument,
  getSourceDocumentLabel,
  getSourceDocumentClientLabel,
  onRefresh
}: JournalWorkbenchPanelProps) {
  const postingAccounts = accounts.filter(
    (account) => account.status === "Active" && account.isPostingAccount
  );
  const focusedEntryInRegister = focusedJournalEntryId !== ""
    && entries.some((entry) => entry.journalEntryId === focusedJournalEntryId);
  const focusedSourceDocumentLabel = focusedJournalEntry === null
    ? null
    : getSourceDocumentLabel(focusedJournalEntry);
  const totalDebit = value.lines.reduce((total, line) => total + amount(line.debit), 0);
  const totalCredit = value.lines.reduce((total, line) => total + amount(line.credit), 0);
  const difference = roundMoney(totalDebit - totalCredit);
  const hasDebit = totalDebit > 0;
  const hasCredit = totalCredit > 0;
  const hasAccounts = value.lines.every((line) => line.ledgerAccountId.trim() !== "");
  const postingPeriodState = getPostingPeriodState(value.entryDate, periods);
  const reversalPeriodState = getPostingPeriodState(toDateInputValue(new Date()), periods);
  const canPost =
    value.entryDate.trim() !== ""
    && value.currencyCode.trim() !== ""
    && value.lines.length >= 2
    && hasDebit
    && hasCredit
    && hasAccounts
    && difference === 0
    && !postingPeriodState.blocksPosting;

  async function handlePost(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onPost();
  }

  function updateLine(index: number, patch: Partial<ManualJournalEntryLineInput>) {
    onValueChange({
      ...value,
      lines: value.lines.map((line, lineIndex) =>
        lineIndex === index ? { ...line, ...patch } : line)
    });
  }

  function addLine() {
    onValueChange({
      ...value,
      lines: [
        ...value.lines,
        {
          ledgerAccountId: "",
          debit: "",
          credit: "",
          description: ""
        }
      ]
    });
  }

  function removeLine(index: number) {
    onValueChange({
      ...value,
      lines: value.lines.filter((_, lineIndex) => lineIndex !== index)
    });
  }

  return (
    <section className="journal-workbench">
      <header className="client-panel journal-header">
        <div>
          <span>{value.currencyCode.trim() === "" ? "PKR" : value.currencyCode}</span>
          <h2>Manual journal</h2>
        </div>
        <button
          className="icon-button"
          type="button"
          onClick={onRefresh}
          disabled={isBusy}
          title="Refresh journal entries"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <section className="client-panel journal-editor-panel">
        <form className="journal-form" onSubmit={handlePost}>
          <div className="billing-form-grid journal-header-fields">
            <label className="form-field">
              <span>Date</span>
              <input
                type="date"
                value={value.entryDate}
                onChange={(event) =>
                  onValueChange({
                    ...value,
                    entryDate: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Currency</span>
              <input
                value={value.currencyCode}
                onChange={(event) =>
                  onValueChange({
                    ...value,
                    currencyCode: event.target.value.toUpperCase()
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Reference</span>
              <input
                value={value.sourceReference}
                onChange={(event) =>
                  onValueChange({
                    ...value,
                    sourceReference: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field wide">
              <span>Memo</span>
              <input
                value={value.memo}
                onChange={(event) =>
                  onValueChange({
                    ...value,
                    memo: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <div className={`journal-period-status ${postingPeriodState.tone}`}>
              <CalendarCheck2 size={16} />
              <span>
                <small>{postingPeriodState.label}</small>
                <strong>{postingPeriodState.status}</strong>
                <em>{postingPeriodState.detail}</em>
              </span>
            </div>
          </div>

          <div className="journal-line-grid">
            <div className="journal-line-head">Account</div>
            <div className="journal-line-head">Debit</div>
            <div className="journal-line-head">Credit</div>
            <div className="journal-line-head">Description</div>
            <div className="journal-line-head"> </div>
            {value.lines.map((line, index) => (
              <div className="journal-line-row" key={`${index}-${line.ledgerAccountId}`}>
                <label className="form-field journal-account-select">
                  <select
                    value={line.ledgerAccountId}
                    onChange={(event) =>
                      updateLine(index, {
                        ledgerAccountId: event.target.value
                      })
                    }
                    disabled={isBusy}
                  >
                    <option value="">Select account</option>
                    {postingAccounts.map((account) => (
                      <option key={account.ledgerAccountId} value={account.ledgerAccountId}>
                        {account.displayCode} - {account.name}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="form-field">
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={line.debit}
                    onChange={(event) =>
                      updateLine(index, {
                        debit: event.target.value,
                        credit: event.target.value.trim() === "" ? line.credit : ""
                      })
                    }
                    disabled={isBusy}
                  />
                </label>
                <label className="form-field">
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={line.credit}
                    onChange={(event) =>
                      updateLine(index, {
                        credit: event.target.value,
                        debit: event.target.value.trim() === "" ? line.debit : ""
                      })
                    }
                    disabled={isBusy}
                  />
                </label>
                <label className="form-field">
                  <input
                    value={line.description}
                    onChange={(event) =>
                      updateLine(index, {
                        description: event.target.value
                      })
                    }
                    disabled={isBusy}
                  />
                </label>
                <button
                  className="table-icon-button"
                  type="button"
                  onClick={() => removeLine(index)}
                  disabled={isBusy || value.lines.length <= 2}
                  title="Remove journal line"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>

          <div className="journal-total-row">
            <div>
              <span>Debit</span>
              <strong>{formatMoney(totalDebit)}</strong>
            </div>
            <div>
              <span>Credit</span>
              <strong>{formatMoney(totalCredit)}</strong>
            </div>
            <div>
              <span>Difference</span>
              <strong>{formatMoney(difference)}</strong>
            </div>
            <div className="journal-actions">
              <button
                className="icon-button"
                type="button"
                onClick={addLine}
                disabled={isBusy}
                title="Add journal line"
              >
                <Plus size={16} />
                Line
              </button>
              <button
                className="icon-button primary"
                type="submit"
                disabled={isBusy || !canPost}
                title={postingPeriodState.blocksPosting
                  ? postingPeriodState.detail
                  : "Post manual journal"}
              >
                <Send size={16} />
                Post
              </button>
            </div>
          </div>
        </form>
      </section>

      <section className="client-panel journal-register-panel">
        <div className="client-panel-heading">
          <div>
            <span>Register</span>
            <strong>Journal entries</strong>
          </div>
          <span className="billing-small-fact">{entries.length}</span>
        </div>
        <div className="coa-filter-panel journal-filter-panel">
          <label className="form-field">
            <span>From</span>
            <input
              type="date"
              value={filters.fromDate}
              onChange={(event) =>
                onFiltersChange({
                  ...filters,
                  fromDate: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>To</span>
            <input
              type="date"
              value={filters.toDate}
              onChange={(event) =>
                onFiltersChange({
                  ...filters,
                  toDate: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Source</span>
            <select
              value={filters.sourceType}
              onChange={(event) =>
                onFiltersChange({
                  ...filters,
                  sourceType: event.target.value
                })
              }
              disabled={isBusy}
            >
              <option value="">All</option>
              {journalSourceTypeOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
        </div>
        {focusedJournalEntry !== null && !focusedEntryInRegister && (
          <div className="journal-focused-entry">
            <div className="journal-focused-entry-heading">
              <div>
                <span>Focused journal</span>
                <strong>{focusedJournalEntry.sourceReference ?? focusedJournalEntry.journalEntryId}</strong>
                <small>
                  {focusedJournalEntry.entryDate} {focusedJournalEntry.sourceType} {focusedJournalEntry.status}
                </small>
              </div>
              <div className="journal-focused-entry-actions">
                <button
                  className="table-icon-button"
                  type="button"
                  onClick={() => void onOpenSourceDocument(focusedJournalEntry)}
                  disabled={isBusy || focusedSourceDocumentLabel === null}
                  title={sourceDocumentTitle(focusedSourceDocumentLabel)}
                >
                  <ExternalLink size={14} />
                </button>
                <button
                  className="table-icon-button"
                  type="button"
                  onClick={() => void onFocusJournalEntry("")}
                  disabled={isBusy}
                  title="Clear focused journal"
                >
                  <X size={14} />
                </button>
              </div>
            </div>
            <JournalLineDetail
              entry={focusedJournalEntry}
              accounts={accounts}
              sourceDocument={sourceDocumentsByJournalEntryId[focusedJournalEntry.journalEntryId] ?? null}
              sourceDocumentClientLabel={sourceDocumentClientLabel(
                sourceDocumentsByJournalEntryId[focusedJournalEntry.journalEntryId],
                getSourceDocumentClientLabel
              )}
            />
          </div>
        )}
        <table className="journal-table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Source</th>
              <th>Reference</th>
              <th>Memo</th>
              <th>Status</th>
              <th>Debit</th>
              <th>Credit</th>
              <th>Lines</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {entries.length === 0 ? (
              <tr>
                <td colSpan={9}>No journal entries</td>
              </tr>
            ) : (
              entries.map((entry) => {
                const sourceDocumentLabel = getSourceDocumentLabel(entry);

                return (
                  <Fragment key={entry.journalEntryId}>
                    <tr className={focusedJournalEntryId === entry.journalEntryId ? "selected" : ""}>
                      <td>{entry.entryDate}</td>
                      <td>{entry.sourceType}</td>
                      <td>{entry.sourceReference ?? "-"}</td>
                      <td>{entry.memo ?? "-"}</td>
                      <td>
                        <span className={`status-pill ${entry.status.toLowerCase()}`}>
                          {entry.status}
                        </span>
                      </td>
                      <td>{formatMoney(entry.totalDebit)}</td>
                      <td>{formatMoney(entry.totalCredit)}</td>
                      <td>{entry.lines.length}</td>
                      <td>
                        <div className="journal-row-actions">
                          <button
                            className={`table-icon-button${
                              focusedJournalEntryId === entry.journalEntryId ? " active" : ""
                            }`}
                            type="button"
                            onClick={() => void onFocusJournalEntry(entry.journalEntryId)}
                            disabled={isBusy}
                            title="View journal lines"
                          >
                            <ListTree size={14} />
                          </button>
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => void onOpenSourceDocument(entry)}
                            disabled={isBusy || sourceDocumentLabel === null}
                            title={sourceDocumentTitle(sourceDocumentLabel)}
                          >
                            <ExternalLink size={14} />
                          </button>
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => void onVoidEntry(entry)}
                            disabled={
                              isBusy
                              || entry.sourceType !== "Manual"
                              || entry.status !== "Posted"
                              || reversalPeriodState.blocksPosting
                            }
                            title={voidManualJournalTitle(entry, reversalPeriodState)}
                          >
                            <Undo2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                    {focusedJournalEntryId === entry.journalEntryId && (
                      <tr className="journal-line-detail-row">
                        <td colSpan={9}>
                          <JournalLineDetail
                            entry={entry}
                            accounts={accounts}
                            sourceDocument={sourceDocumentsByJournalEntryId[entry.journalEntryId] ?? null}
                            sourceDocumentClientLabel={sourceDocumentClientLabel(
                              sourceDocumentsByJournalEntryId[entry.journalEntryId],
                              getSourceDocumentClientLabel
                            )}
                          />
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })
            )}
          </tbody>
        </table>
      </section>
    </section>
  );
}

function JournalLineDetail({
  entry,
  accounts,
  sourceDocument,
  sourceDocumentClientLabel
}: {
  entry: JournalEntrySummary;
  accounts: LedgerAccountSummary[];
  sourceDocument: JournalEntrySourceDocument | null;
  sourceDocumentClientLabel: string;
}) {
  return (
    <div className="journal-line-detail">
      <div className="journal-line-detail-heading">
        <span>{entry.journalEntryId}</span>
        <strong>{entry.sourceReference ?? entry.sourceType}</strong>
      </div>
      {sourceDocument !== null && (
        <SourceDocumentSummary
          sourceDocument={sourceDocument}
          clientLabel={sourceDocumentClientLabel}
        />
      )}
      <table>
        <thead>
          <tr>
            <th>Account</th>
            <th>Description</th>
            <th>Debit</th>
            <th>Credit</th>
          </tr>
        </thead>
        <tbody>
          {entry.lines.map((line, index) => (
            <tr key={`${entry.journalEntryId}-${line.ledgerAccountId}-${index}`}>
              <td>{formatAccount(line.ledgerAccountId, accounts)}</td>
              <td>{line.description ?? "-"}</td>
              <td>{formatMoney(line.debit)}</td>
              <td>{formatMoney(line.credit)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SourceDocumentSummary({
  sourceDocument,
  clientLabel
}: {
  sourceDocument: JournalEntrySourceDocument;
  clientLabel: string;
}) {
  if (!sourceDocument.isResolved) {
    return (
      <div className="journal-source-summary unresolved">
        <span>
          <small>Source document</small>
          <strong>{sourceDocument.message ?? "Not resolved"}</strong>
        </span>
      </div>
    );
  }

  return (
    <div className="journal-source-summary">
      <span>
        <small>Source document</small>
        <strong>{sourceDocument.label ?? sourceDocument.reference ?? sourceDocument.documentKind ?? "Resolved"}</strong>
      </span>
      <span>
        <small>Status</small>
        <strong>{sourceDocument.status ?? "-"}</strong>
      </span>
      <span>
        <small>Date</small>
        <strong>{sourceDocument.documentDate ?? "-"}</strong>
      </span>
      <span>
        <small>Amount</small>
        <strong>{formatSourceAmount(sourceDocument)}</strong>
      </span>
      <span>
        <small>Client</small>
        <strong>{clientLabel}</strong>
      </span>
    </div>
  );
}

function sourceDocumentClientLabel(
  sourceDocument: JournalEntrySourceDocument | undefined,
  getSourceDocumentClientLabel: (sourceDocument: JournalEntrySourceDocument) => string
): string {
  return sourceDocument === undefined ? "-" : getSourceDocumentClientLabel(sourceDocument);
}

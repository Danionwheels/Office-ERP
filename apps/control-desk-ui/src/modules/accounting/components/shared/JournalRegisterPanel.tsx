import {
  ClipboardList,
  ExternalLink,
  FileCheck2,
  ListTree,
  Plus,
  RefreshCw,
  Undo2,
  X
} from "lucide-react";
import type {
  JournalEntryFilters,
  JournalEntrySummary
} from "../../types/accountingTypes";
import { journalSourceTypeOptions } from "../../constants/accountingConstants";
import {
  formatJournalRegisterWindow,
  getJournalEntryBalanceDifference,
  getJournalRegisterStatusItems
} from "../../utils/journalWorkbenchModel";
import {
  formatMoney,
  sourceDocumentTitle,
  type PostingPeriodState,
  voidManualJournalTitle
} from "../../utils/journalModel";

type JournalRegisterPanelProps = {
  entries: JournalEntrySummary[];
  filters: JournalEntryFilters;
  focusedJournalEntryId: string;
  focusedJournalEntry: JournalEntrySummary | null;
  isBusy: boolean;
  reversalPeriodState: PostingPeriodState;
  onFiltersChange: (value: JournalEntryFilters) => void;
  onRefresh: () => Promise<void>;
  onOpenVoucherEntry: () => void;
  onOpenOpeningBalance: () => void;
  onOpenJournalDetail: (journalEntryId: string) => Promise<void>;
  onClearFocusedJournal: () => Promise<void>;
  onVoidEntry: (entry: JournalEntrySummary) => Promise<void>;
  onOpenSourceDocument: (entry: JournalEntrySummary) => Promise<void>;
  getSourceDocumentLabel: (entry: JournalEntrySummary) => string | null;
};

export function JournalRegisterPanel({
  entries,
  filters,
  focusedJournalEntryId,
  focusedJournalEntry,
  isBusy,
  reversalPeriodState,
  onFiltersChange,
  onRefresh,
  onOpenVoucherEntry,
  onOpenOpeningBalance,
  onOpenJournalDetail,
  onClearFocusedJournal,
  onVoidEntry,
  onOpenSourceDocument,
  getSourceDocumentLabel
}: JournalRegisterPanelProps) {
  const journalRegisterStatusItems = getJournalRegisterStatusItems(
    entries,
    filters,
    getSourceDocumentLabel
  );
  const focusedEntryInRegister = focusedJournalEntryId !== ""
    && entries.some((entry) => entry.journalEntryId === focusedJournalEntryId);

  return (
    <section className="journal-register-shell">
      <header className="journal-maintain-toolbar">
        <div className="journal-maintain-title">
          <span>MAIN / Journal Register</span>
          <strong>Journal Entries</strong>
          <small>{formatJournalRegisterWindow(filters)}</small>
        </div>
        <div className="journal-toolbar-fields">
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
          <div className="journal-toolbar-actions">
            <button
              className="icon-button"
              type="button"
              onClick={onOpenVoucherEntry}
              disabled={isBusy}
              title="Open manual voucher entry"
            >
              <Plus size={16} />
              New
            </button>
            <button
              className="icon-button"
              type="button"
              onClick={onOpenOpeningBalance}
              disabled={isBusy}
              title="Open opening balance import"
            >
              <FileCheck2 size={16} />
              Opening
            </button>
            <button
              className="icon-button"
              type="button"
              onClick={() => {
                if (focusedJournalEntryId !== "") {
                  void onOpenJournalDetail(focusedJournalEntryId);
                }
              }}
              disabled={isBusy || focusedJournalEntryId === ""}
              title="Open focused journal details"
            >
              <ClipboardList size={16} />
              Details
            </button>
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
          </div>
        </div>
      </header>

      <section className="client-panel journal-register-panel">
        <div className="client-panel-heading journal-window-heading">
          <div>
            <span>Register</span>
            <strong>Journal Entries</strong>
          </div>
          <span className="billing-small-fact">{entries.length}</span>
        </div>
        <div className="journal-register-status-row" aria-label="Journal register status">
          {journalRegisterStatusItems.map((item) => (
            <span className={item.tone} key={item.label}>
              <small>{item.label}</small>
              <strong>{item.value}</strong>
            </span>
          ))}
        </div>
        {focusedJournalEntry !== null && !focusedEntryInRegister && (
          <div className="journal-focused-entry compact">
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
                  onClick={() => void onOpenJournalDetail(focusedJournalEntry.journalEntryId)}
                  disabled={isBusy}
                  title="Open focused journal details"
                >
                  <ListTree size={14} />
                </button>
                <button
                  className="table-icon-button"
                  type="button"
                  onClick={() => void onClearFocusedJournal()}
                  disabled={isBusy}
                  title="Clear focused journal"
                >
                  <X size={14} />
                </button>
              </div>
            </div>
          </div>
        )}

        <div className="journal-register-table-frame">
          <table className="journal-table journal-register-table">
            <thead>
              <tr>
                <th>Voucher No.</th>
                <th>Date</th>
                <th>Source</th>
                <th>Dr / Cr</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {entries.length === 0 ? (
                <tr>
                  <td colSpan={6}>No journal entries</td>
                </tr>
              ) : (
                entries.map((entry) => {
                  const sourceDocumentLabel = getSourceDocumentLabel(entry);
                  const registerDifference = getJournalEntryBalanceDifference(entry);
                  const registerBalanceTone = registerDifference === 0 ? "ready" : "warning";

                  return (
                    <tr
                      className={focusedJournalEntryId === entry.journalEntryId ? "selected" : ""}
                      key={entry.journalEntryId}
                    >
                      <td>
                        <span className="journal-reference-cell">
                          <strong>{entry.sourceReference ?? entry.journalEntryId}</strong>
                          <small>{entry.journalEntryId}</small>
                        </span>
                      </td>
                      <td>
                        <span className="journal-date-cell">
                          <strong>{entry.entryDate}</strong>
                          <small>{entry.currencyCode}</small>
                        </span>
                      </td>
                      <td>
                        <span className="journal-source-cell">
                          <strong>{entry.sourceType}</strong>
                          <small>{sourceDocumentLabel ?? "Journal only"}</small>
                        </span>
                      </td>
                      <td>
                        <span className={`journal-amount-cell ${registerBalanceTone}`}>
                          <strong>Dr {formatMoney(entry.totalDebit)}</strong>
                          <small>Cr {formatMoney(entry.totalCredit)}</small>
                          <em>{registerDifference === 0 ? "Balanced" : `Out ${formatMoney(Math.abs(registerDifference))}`}</em>
                        </span>
                      </td>
                      <td>
                        <span className="journal-status-cell">
                          <strong className={`journal-register-status ${entry.status.toLowerCase()}`}>
                            {entry.status}
                          </strong>
                          <small>{entry.lines.length} lines</small>
                        </span>
                      </td>
                      <td>
                        <div className="journal-row-actions">
                          <button
                            className={`table-icon-button${
                              focusedJournalEntryId === entry.journalEntryId ? " active" : ""
                            }`}
                            type="button"
                            onClick={() => void onOpenJournalDetail(entry.journalEntryId)}
                            disabled={isBusy}
                            title="Open journal details"
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
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </section>
    </section>
  );
}

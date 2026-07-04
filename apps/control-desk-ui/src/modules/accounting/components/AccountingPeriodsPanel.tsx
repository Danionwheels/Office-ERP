import {
  CalendarRange,
  FileCheck2,
  FileSearch,
  ListTree,
  Lock,
  Plus,
  RefreshCw,
  ShieldCheck,
  Unlock
} from "lucide-react";
import { type FormEvent, useState } from "react";
import type {
  AccountingPeriod,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  AccountingPeriodFormInput
} from "../types/accountingTypes";

type AccountingPeriodsPanelProps = {
  periods: AccountingPeriod[];
  readiness: AccountingPeriodCloseReadiness | null;
  closeJournalPreview: AccountingPeriodCloseJournalPreview | null;
  value: AccountingPeriodFormInput;
  isBusy: boolean;
  onValueChange: (value: AccountingPeriodFormInput) => void;
  onPrepareNext: () => void;
  onCreate: () => Promise<void>;
  onCheckReadiness: (period: AccountingPeriod) => Promise<void>;
  onPreviewCloseJournal: (period: AccountingPeriod) => Promise<void>;
  onClose: (period: AccountingPeriod) => Promise<void>;
  onReopen: (period: AccountingPeriod) => Promise<void>;
  onViewCloseJournalEntry: (journalEntryId: string) => Promise<void>;
  onRefresh: () => Promise<void>;
};

const accountingCompanyCode = "MAIN";

export function AccountingPeriodsPanel({
  periods,
  readiness,
  closeJournalPreview,
  value,
  isBusy,
  onValueChange,
  onPrepareNext,
  onCreate,
  onCheckReadiness,
  onPreviewCloseJournal,
  onClose,
  onReopen,
  onViewCloseJournalEntry,
  onRefresh
}: AccountingPeriodsPanelProps) {
  const [selectedArtifactPeriodId, setSelectedArtifactPeriodId] = useState("");
  const openPeriods = periods.filter((period) => period.status === "Open").length;
  const closedPeriods = periods.filter((period) => period.status === "Closed").length;
  const currentPeriod = periods.find((period) => containsDate(period, value.startsOn)) ?? periods[0] ?? null;
  const selectedReadinessPeriod = readiness?.period.accountingPeriodId ?? "";
  const artifactPeriod =
    periods.find((period) =>
      period.accountingPeriodId === selectedArtifactPeriodId && period.closeArtifact
    )
    ?? periods.find((period) => period.closeArtifact)
    ?? null;
  const canCreate =
    value.startsOn.trim() !== ""
    && value.endsOn.trim() !== ""
    && value.endsOn >= value.startsOn;
  const selectedPreviewPeriod = closeJournalPreview?.period.accountingPeriodId ?? "";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreate();
  }

  return (
    <section className="accounting-period-workbench">
      <header className="client-panel accounting-period-header">
        <div>
          <span>{accountingCompanyCode}</span>
          <h2>Accounting periods</h2>
        </div>
        <div className="accounting-period-header-actions">
          <button
            className="icon-button"
            type="button"
            onClick={onPrepareNext}
            disabled={isBusy}
            title="Prepare next monthly period"
          >
            <CalendarRange size={16} />
            Next
          </button>
          <button
            className="icon-button"
            type="button"
            onClick={onRefresh}
            disabled={isBusy}
            title="Refresh accounting periods"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>
      </header>

      <section className="client-panel accounting-period-form-panel">
        <form className="accounting-period-form" onSubmit={handleSubmit}>
          <label className="form-field">
            <span>Company</span>
            <input
              value={accountingCompanyCode}
              disabled
              readOnly
            />
          </label>
          <label className="form-field">
            <span>Name</span>
            <input
              value={value.name}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  name: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Starts</span>
            <input
              type="date"
              value={value.startsOn}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  startsOn: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Ends</span>
            <input
              type="date"
              value={value.endsOn}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  endsOn: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <button
            className="icon-button primary"
            type="submit"
            disabled={isBusy || !canCreate}
            title="Create accounting period"
          >
            <Plus size={16} />
            Create
          </button>
        </form>
      </section>

      <div className="accounting-period-summary-row">
        <article className="client-panel accounting-period-summary-card">
          <span>Current</span>
          <strong>{currentPeriod?.name ?? "-"}</strong>
        </article>
        <article className="client-panel accounting-period-summary-card">
          <span>Open</span>
          <strong>{openPeriods}</strong>
        </article>
        <article className="client-panel accounting-period-summary-card">
          <span>Closed</span>
          <strong>{closedPeriods}</strong>
        </article>
      </div>

      <section className="client-panel accounting-period-table-panel">
        <div className="client-panel-heading">
          <div>
            <span>Ledger calendar</span>
            <strong>Periods</strong>
          </div>
          <span className="billing-small-fact">{periods.length}</span>
        </div>
        <table className="accounting-period-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Starts</th>
              <th>Ends</th>
              <th>Status</th>
              <th>Updated</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {periods.length === 0 ? (
              <tr>
                <td colSpan={6}>No accounting periods</td>
              </tr>
            ) : (
              periods.map((period) => (
                <tr key={period.accountingPeriodId}>
                  <td>
                    <strong>{period.name}</strong>
                  </td>
                  <td>{period.startsOn}</td>
                  <td>{period.endsOn}</td>
                  <td>
                    <span className={`status-pill ${period.status.toLowerCase()}`}>
                      {period.status}
                    </span>
                  </td>
                  <td>{formatTimestamp(period.updatedAtUtc)}</td>
                  <td>
                    {period.status === "Closed" ? (
                      <div className="accounting-period-row-actions">
                        <button
                          className={`table-icon-button${
                            selectedPreviewPeriod === period.accountingPeriodId ? " active" : ""
                          }`}
                          type="button"
                          onClick={() => void onPreviewCloseJournal(period)}
                          disabled={isBusy}
                          title="Preview close journal"
                        >
                          <FileSearch size={14} />
                        </button>
                        <button
                          className={`table-icon-button${
                            artifactPeriod?.accountingPeriodId === period.accountingPeriodId ? " active" : ""
                          }`}
                          type="button"
                          onClick={() => setSelectedArtifactPeriodId(period.accountingPeriodId)}
                          disabled={!period.closeArtifact}
                          title="View close artifact"
                        >
                          <FileCheck2 size={14} />
                        </button>
                        <button
                          className="table-icon-button"
                          type="button"
                          onClick={() => void onReopen(period)}
                          disabled={isBusy}
                          title="Reopen accounting period"
                        >
                          <Unlock size={14} />
                        </button>
                      </div>
                    ) : (
                      <div className="accounting-period-row-actions">
                        <button
                          className={`table-icon-button${
                            selectedReadinessPeriod === period.accountingPeriodId ? " active" : ""
                          }`}
                          type="button"
                          onClick={() => void onCheckReadiness(period)}
                          disabled={isBusy}
                          title="Check close readiness"
                        >
                          <ShieldCheck size={14} />
                        </button>
                        <button
                          className={`table-icon-button${
                            selectedPreviewPeriod === period.accountingPeriodId ? " active" : ""
                          }`}
                          type="button"
                          onClick={() => void onPreviewCloseJournal(period)}
                          disabled={isBusy}
                          title="Preview close journal"
                        >
                          <FileSearch size={14} />
                        </button>
                        <button
                          className="table-icon-button"
                          type="button"
                          onClick={() => {
                            setSelectedArtifactPeriodId(period.accountingPeriodId);
                            void onClose(period);
                          }}
                          disabled={isBusy || isKnownBlocked(period, readiness)}
                          title="Close accounting period"
                        >
                          <Lock size={14} />
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </section>

      {readiness !== null && (
        <section className={`client-panel accounting-period-readiness-panel${
          readiness.canClose ? " ready" : " blocked"
        }`}>
          <div className="client-panel-heading">
            <div>
              <span>{readiness.period.name}</span>
              <strong>Close readiness</strong>
            </div>
            <span className={`status-pill ${readiness.canClose ? "open" : "voided"}`}>
              {readiness.canClose ? "Ready" : "Blocked"}
            </span>
          </div>
          <div className="accounting-period-readiness-grid">
            {readiness.checks.map((check) => (
              <article
                className={`accounting-period-readiness-check ${check.status.toLowerCase()}`}
                key={check.code}
              >
                <span>{check.status}</span>
                <strong>{check.code}</strong>
                <small>{check.message}</small>
              </article>
            ))}
          </div>
          <table className="accounting-period-currency-table">
            <thead>
              <tr>
                <th>Currency</th>
                <th>Debit</th>
                <th>Credit</th>
                <th>Difference</th>
                <th>Posted</th>
                <th>Draft</th>
              </tr>
            </thead>
            <tbody>
              {readiness.currencies.length === 0 ? (
                <tr>
                  <td colSpan={6}>No journal activity</td>
                </tr>
              ) : (
                readiness.currencies.map((currency) => (
                  <tr key={currency.currencyCode}>
                    <td>
                      <strong>{currency.currencyCode}</strong>
                    </td>
                    <td>{formatMoney(currency.totalDebit)}</td>
                    <td>{formatMoney(currency.totalCredit)}</td>
                    <td>{formatMoney(currency.difference)}</td>
                    <td>{currency.postedJournalCount}</td>
                    <td>{currency.draftJournalCount}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </section>
      )}

      {closeJournalPreview !== null && (
        <section className={`client-panel accounting-close-preview-panel${
          closeJournalPreview.canGenerate ? " ready" : " blocked"
        }`}>
          <div className="client-panel-heading">
            <div>
              <span>{closeJournalPreview.period.name}</span>
              <strong>Close journal preview</strong>
            </div>
            <span className={`status-pill ${closeJournalPreview.canGenerate ? "open" : "voided"}`}>
              {closeJournalPreview.canGenerate ? "Ready" : "Blocked"}
            </span>
          </div>
          <div className="accounting-close-preview-summary">
            <span>
              <small>Base currency</small>
              <strong>{closeJournalPreview.baseCurrencyCode || "-"}</strong>
            </span>
            <span>
              <small>Net income</small>
              <strong>{formatMoney(closeJournalPreview.netIncome)}</strong>
            </span>
            <span>
              <small>Total debit</small>
              <strong>{formatMoney(closeJournalPreview.totalDebit)}</strong>
            </span>
            <span>
              <small>Total credit</small>
              <strong>{formatMoney(closeJournalPreview.totalCredit)}</strong>
            </span>
          </div>
          {closeJournalPreview.blockers.length > 0 && (
            <div className="accounting-close-preview-blockers">
              {closeJournalPreview.blockers.map((blocker) => (
                <span key={blocker}>{blocker}</span>
              ))}
            </div>
          )}
          <div className="accounting-close-preview-entry-list">
            {closeJournalPreview.entries.length === 0 ? (
              <p className="accounting-close-preview-empty">No close journal lines</p>
            ) : (
              closeJournalPreview.entries.map((entry) => (
                <article className="accounting-close-preview-entry" key={entry.sourceReference}>
                  <header className="accounting-close-preview-entry-header">
                    <div>
                      <span>{entry.sourceReference}</span>
                      <strong>{entry.memo}</strong>
                    </div>
                    <div>
                      <span>{entry.entryDate}</span>
                      <strong>{entry.currencyCode}</strong>
                    </div>
                  </header>
                  <table className="accounting-close-preview-table">
                    <thead>
                      <tr>
                        <th>Account</th>
                        <th>Type</th>
                        <th>Description</th>
                        <th>Debit</th>
                        <th>Credit</th>
                      </tr>
                    </thead>
                    <tbody>
                      {entry.lines.map((line) => (
                        <tr key={`${entry.sourceReference}-${line.ledgerAccountId}-${line.description}`}>
                          <td>
                            <strong>{line.code}</strong>
                            <small>{line.name}</small>
                          </td>
                          <td>{line.type}</td>
                          <td>{line.description}</td>
                          <td>{formatMoney(line.debit)}</td>
                          <td>{formatMoney(line.credit)}</td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot>
                      <tr>
                        <td colSpan={3}>Entry total</td>
                        <td>{formatMoney(entry.totalDebit)}</td>
                        <td>{formatMoney(entry.totalCredit)}</td>
                      </tr>
                    </tfoot>
                  </table>
                </article>
              ))
            )}
          </div>
        </section>
      )}

      {artifactPeriod?.closeArtifact && (
        <section className="client-panel accounting-period-close-artifact-panel">
          <div className="client-panel-heading">
            <div>
              <span>{artifactPeriod.name}</span>
              <strong>Close artifact</strong>
            </div>
            <span className="billing-small-fact">
              {formatTimestamp(artifactPeriod.closeArtifact.generatedAtUtc)}
            </span>
          </div>
          <div className="accounting-period-artifact-summary">
            <span>
              <small>Generated by</small>
              <strong>{artifactPeriod.closeArtifact.generatedBy}</strong>
            </span>
            <span>
              <small>Checks</small>
              <strong>{artifactPeriod.closeArtifact.checkCount}</strong>
            </span>
            <span>
              <small>Currencies</small>
              <strong>{artifactPeriod.closeArtifact.currencyCount}</strong>
            </span>
            <span>
              <small>Posted journals</small>
              <strong>{artifactPeriod.closeArtifact.postedJournalCount}</strong>
            </span>
          </div>
          <div className="accounting-period-readiness-grid">
            {artifactPeriod.closeArtifact.checks.map((check) => (
              <article
                className={`accounting-period-readiness-check ${check.status.toLowerCase()}`}
                key={`${artifactPeriod.accountingPeriodId}-${check.code}`}
              >
                <span>{check.status}</span>
                <strong>{check.code}</strong>
                <small>{check.message}</small>
              </article>
            ))}
          </div>
          <table className="accounting-period-currency-table">
            <thead>
              <tr>
                <th>Currency</th>
                <th>Debit</th>
                <th>Credit</th>
                <th>Difference</th>
                <th>Posted</th>
                <th>Draft</th>
              </tr>
            </thead>
            <tbody>
              {artifactPeriod.closeArtifact.currencies.length === 0 ? (
                <tr>
                  <td colSpan={6}>No journal activity</td>
                </tr>
              ) : (
                artifactPeriod.closeArtifact.currencies.map((currency) => (
                  <tr key={`${artifactPeriod.accountingPeriodId}-${currency.currencyCode}`}>
                    <td>
                      <strong>{currency.currencyCode}</strong>
                    </td>
                    <td>{formatMoney(currency.totalDebit)}</td>
                    <td>{formatMoney(currency.totalCredit)}</td>
                    <td>{formatMoney(currency.difference)}</td>
                    <td>{currency.postedJournalCount}</td>
                    <td>{currency.draftJournalCount}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
          <table className="accounting-period-currency-table">
            <thead>
              <tr>
                <th>Close journal</th>
                <th>Date</th>
                <th>Currency</th>
                <th>Debit</th>
                <th>Credit</th>
                <th>Memo</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {artifactPeriod.closeArtifact.closeJournalEntries.length === 0 ? (
                <tr>
                  <td colSpan={7}>No generated close journals</td>
                </tr>
              ) : (
                artifactPeriod.closeArtifact.closeJournalEntries.map((entry) => (
                  <tr key={entry.journalEntryId}>
                    <td>
                      <strong>{entry.sourceReference}</strong>
                    </td>
                    <td>{entry.entryDate}</td>
                    <td>{entry.currencyCode}</td>
                    <td>{formatMoney(entry.totalDebit)}</td>
                    <td>{formatMoney(entry.totalCredit)}</td>
                    <td>{entry.memo}</td>
                    <td>
                      <button
                        className="table-icon-button"
                        type="button"
                        onClick={() => void onViewCloseJournalEntry(entry.journalEntryId)}
                        disabled={isBusy}
                        title={`Open close journal ${entry.sourceReference}`}
                      >
                        <ListTree size={14} />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </section>
      )}
    </section>
  );
}

function isKnownBlocked(
  period: AccountingPeriod,
  readiness: AccountingPeriodCloseReadiness | null
): boolean {
  return readiness?.period.accountingPeriodId === period.accountingPeriodId && !readiness.canClose;
}

function containsDate(period: AccountingPeriod, date: string): boolean {
  return date.trim() !== "" && date >= period.startsOn && date <= period.endsOn;
}

function formatTimestamp(value: string): string {
  return value.slice(0, 10);
}

function formatMoney(value: number): string {
  return value.toFixed(2);
}

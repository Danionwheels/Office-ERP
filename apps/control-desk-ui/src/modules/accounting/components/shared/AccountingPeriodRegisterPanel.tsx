import {
  FileCheck2,
  FileSearch,
  Lock,
  ShieldCheck,
  Unlock
} from "lucide-react";
import type {
  AccountingPeriod,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness
} from "../../types/accountingTypes";
import {
  formatTimestamp,
  isKnownBlocked
} from "../../utils/accountingPeriodsWorkspaceModel";

type AccountingPeriodRegisterPanelProps = {
  periods: AccountingPeriod[];
  readiness: AccountingPeriodCloseReadiness | null;
  closeJournalPreview: AccountingPeriodCloseJournalPreview | null;
  artifactPeriod: AccountingPeriod | null;
  inspectedPeriodIds: string[];
  isBusy: boolean;
  onCheckReadiness: (period: AccountingPeriod) => Promise<void>;
  onPreviewCloseJournal: (period: AccountingPeriod) => Promise<void>;
  onSelectArtifactPeriod: (period: AccountingPeriod) => void;
  onClose: (period: AccountingPeriod) => Promise<void>;
  onReopen: (period: AccountingPeriod) => Promise<void>;
};

export function AccountingPeriodRegisterPanel({
  periods,
  readiness,
  closeJournalPreview,
  artifactPeriod,
  inspectedPeriodIds,
  isBusy,
  onCheckReadiness,
  onPreviewCloseJournal,
  onSelectArtifactPeriod,
  onClose,
  onReopen
}: AccountingPeriodRegisterPanelProps) {
  const selectedReadinessPeriod = readiness?.period.accountingPeriodId ?? "";
  const selectedPreviewPeriod = closeJournalPreview?.period.accountingPeriodId ?? "";

  return (
    <section className="client-panel accounting-period-table-panel">
      <div className="client-panel-heading">
        <div>
          <span>Period Register</span>
          <strong>Periods</strong>
        </div>
        <span className="billing-small-fact">{periods.length}</span>
      </div>
      <table className="accounting-period-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Start</th>
            <th>End</th>
            <th>Status</th>
            <th>Updated</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {periods.length === 0 ? (
            <tr>
              <td colSpan={6}>No periods opened</td>
            </tr>
          ) : (
            periods.map((period) => (
              <tr
                className={`accounting-period-row ${period.status.toLowerCase()}${
                  inspectedPeriodIds.includes(period.accountingPeriodId) ? " inspected" : ""
                }`}
                key={period.accountingPeriodId}
              >
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
                        onClick={() => onSelectArtifactPeriod(period)}
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
                        onClick={() => void onClose(period)}
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
  );
}

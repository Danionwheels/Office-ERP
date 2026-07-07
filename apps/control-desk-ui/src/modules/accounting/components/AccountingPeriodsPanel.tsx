import {
  CalendarRange,
  PanelRightOpen,
  RefreshCw
} from "lucide-react";
import { useState } from "react";
import type {
  AccountingPeriod,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  AccountingPeriodFormInput
} from "../types/accountingTypes";
import {
  canCreateAccountingPeriod,
  getAccountingPeriodSummary,
  getArtifactPeriod,
  getInspectedAccountingPeriodIds
} from "../utils/accountingPeriodsWorkspaceModel";
import { AccountingPeriodRegisterPanel } from "./shared/AccountingPeriodRegisterPanel";
import { AccountingPeriodSummaryStrip } from "./shared/AccountingPeriodSummaryStrip";
import { AccountingPeriodWorkWindow } from "./shared/AccountingPeriodWorkWindow";

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
  const [isWorkWindowOpen, setIsWorkWindowOpen] = useState(false);
  const summary = getAccountingPeriodSummary(periods, value);
  const artifactPeriod = getArtifactPeriod(periods, selectedArtifactPeriodId);
  const inspectedPeriodIds = getInspectedAccountingPeriodIds(
    readiness,
    closeJournalPreview,
    artifactPeriod
  );
  const canCreate = canCreateAccountingPeriod(value);

  function handlePrepareNextPeriod() {
    onPrepareNext();
    setIsWorkWindowOpen(true);
  }

  async function handleCreatePeriod() {
    await onCreate();
  }

  async function handleCheckReadiness(period: AccountingPeriod) {
    setIsWorkWindowOpen(true);
    await onCheckReadiness(period);
  }

  async function handlePreviewCloseJournal(period: AccountingPeriod) {
    setIsWorkWindowOpen(true);
    await onPreviewCloseJournal(period);
  }

  function handleSelectArtifactPeriod(period: AccountingPeriod) {
    setSelectedArtifactPeriodId(period.accountingPeriodId);
    setIsWorkWindowOpen(true);
  }

  async function handleClosePeriod(period: AccountingPeriod) {
    setSelectedArtifactPeriodId(period.accountingPeriodId);
    setIsWorkWindowOpen(true);
    await onClose(period);
  }

  async function handleReopenPeriod(period: AccountingPeriod) {
    setSelectedArtifactPeriodId(period.accountingPeriodId);
    setIsWorkWindowOpen(true);
    await onReopen(period);
  }

  return (
    <section className="accounting-period-workbench">
      <header className="client-panel accounting-period-header">
        <div>
          <span>{accountingCompanyCode}</span>
          <h2>Accounting Periods</h2>
        </div>
        <div className="accounting-period-header-actions">
          <button
            className="icon-button"
            type="button"
            onClick={handlePrepareNextPeriod}
            disabled={isBusy}
            title="Prepare next monthly period"
          >
            <CalendarRange size={16} />
            Next Period
          </button>
          <button
            className={`icon-button${isWorkWindowOpen ? " primary" : ""}`}
            type="button"
            onClick={() => setIsWorkWindowOpen(true)}
            disabled={isBusy}
            title="Open period work window"
          >
            <PanelRightOpen size={16} />
            Window
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

      <AccountingPeriodSummaryStrip
        currentPeriod={summary.currentPeriod}
        openPeriods={summary.openPeriods}
        closedPeriods={summary.closedPeriods}
        totalPeriods={periods.length}
      />

      <AccountingPeriodRegisterPanel
        periods={periods}
        readiness={readiness}
        closeJournalPreview={closeJournalPreview}
        artifactPeriod={artifactPeriod}
        inspectedPeriodIds={inspectedPeriodIds}
        isBusy={isBusy}
        onCheckReadiness={handleCheckReadiness}
        onPreviewCloseJournal={handlePreviewCloseJournal}
        onSelectArtifactPeriod={handleSelectArtifactPeriod}
        onClose={handleClosePeriod}
        onReopen={handleReopenPeriod}
      />

      {isWorkWindowOpen && (
        <AccountingPeriodWorkWindow
          value={value}
          readiness={readiness}
          closeJournalPreview={closeJournalPreview}
          artifactPeriod={artifactPeriod}
          isBusy={isBusy}
          canCreate={canCreate}
          onValueChange={onValueChange}
          onCreate={handleCreatePeriod}
          onViewCloseJournalEntry={onViewCloseJournalEntry}
          onClose={() => setIsWorkWindowOpen(false)}
        />
      )}
    </section>
  );
}

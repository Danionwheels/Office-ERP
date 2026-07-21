import { X } from "lucide-react";
import type {
  AccountingPeriod,
  AccountingPeriodCloseJournalPreview,
  AccountingPeriodCloseReadiness,
  AccountingPeriodFormInput
} from "../../types/accountingTypes";
import { AccountingClosePreviewPanel } from "./AccountingClosePreviewPanel";
import { AccountingPeriodArtifactPanel } from "./AccountingPeriodArtifactPanel";
import { AccountingPeriodCreatePanel } from "./AccountingPeriodCreatePanel";
import { AccountingPeriodReadinessPanel } from "./AccountingPeriodReadinessPanel";

type AccountingPeriodWorkWindowProps = {
  value: AccountingPeriodFormInput;
  readiness: AccountingPeriodCloseReadiness | null;
  closeJournalPreview: AccountingPeriodCloseJournalPreview | null;
  artifactPeriod: AccountingPeriod | null;
  isBusy: boolean;
  canCreate: boolean;
  onValueChange: (value: AccountingPeriodFormInput) => void;
  onCreate: () => Promise<void>;
  onViewCloseJournalEntry: (journalEntryId: string) => Promise<void>;
  onClose: () => void;
};

export function AccountingPeriodWorkWindow({
  value,
  readiness,
  closeJournalPreview,
  artifactPeriod,
  isBusy,
  canCreate,
  onValueChange,
  onCreate,
  onViewCloseJournalEntry,
  onClose
}: AccountingPeriodWorkWindowProps) {
  return (
    <aside
      className="accounting-period-work-window"
      role="dialog"
      aria-label="Accounting period work window"
    >
      <header className="accounting-period-work-window-header">
        <div>
          <strong>Period Work Window</strong>
          <small>Accounting periods</small>
        </div>
        <button
          className="table-icon-button"
          type="button"
          onClick={onClose}
          title="Close period work window"
        >
          <X size={15} />
        </button>
      </header>
      <div className="accounting-period-work-window-body">
        <AccountingPeriodCreatePanel
          value={value}
          isBusy={isBusy}
          canCreate={canCreate}
          onValueChange={onValueChange}
          onCreate={onCreate}
        />

        {readiness !== null && (
          <AccountingPeriodReadinessPanel readiness={readiness} />
        )}

        {closeJournalPreview !== null && (
          <AccountingClosePreviewPanel closeJournalPreview={closeJournalPreview} />
        )}

        {artifactPeriod?.closeArtifact && (
          <AccountingPeriodArtifactPanel
            period={artifactPeriod}
            isBusy={isBusy}
            onViewCloseJournalEntry={onViewCloseJournalEntry}
          />
        )}
      </div>
    </aside>
  );
}

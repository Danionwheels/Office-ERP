import { useState } from "react";
import type {
  AccountingPeriod,
  JournalEntryFilters,
  JournalEntryRegisterPage,
  JournalEntrySourceDocument,
  JournalEntrySummary,
  JournalVoucherNumberPreview,
  LedgerAccountSummary,
  ManualJournalEntryInput,
  OpeningBalanceImportInput,
  OpeningBalanceImportPreview,
  OpeningBalanceImportTemplateFormat,
  OpeningBalanceImportTextPreview
} from "../types/accountingTypes";
import { toDateInputValue } from "../utils/accountingDates";
import { getPostingPeriodState } from "../utils/journalModel";
import { sourceDocumentClientLabel } from "../utils/journalWorkbenchModel";
import { JournalEntryDetailPanel } from "./shared/JournalEntryDetailPanel";
import { JournalEntryEditorPanel } from "./shared/JournalEntryEditorPanel";
import { JournalRegisterPanel } from "./shared/JournalRegisterPanel";
import {
  JournalWorkWindow,
  type JournalWorkWindowView
} from "./shared/JournalWorkWindow";
import { OpeningBalanceImportPanel } from "./shared/OpeningBalanceImportPanel";

type JournalWorkbenchPanelProps = {
  accounts: LedgerAccountSummary[];
  periods: AccountingPeriod[];
  entries: JournalEntrySummary[];
  page: Omit<JournalEntryRegisterPage, "entries">;
  filters: JournalEntryFilters;
  value: ManualJournalEntryInput;
  manualVoucherPreview: JournalVoucherNumberPreview | null;
  openingBalanceValue: OpeningBalanceImportInput;
  openingBalancePreview: OpeningBalanceImportPreview | null;
  openingBalanceImportText: string;
  openingBalanceImportDelimiter: string;
  openingBalanceImportTextPreview: OpeningBalanceImportTextPreview | null;
  focusedJournalEntryId: string;
  focusedJournalEntry: JournalEntrySummary | null;
  sourceDocumentsByJournalEntryId: Record<string, JournalEntrySourceDocument>;
  isBusy: boolean;
  onFiltersChange: (value: JournalEntryFilters) => void;
  onValueChange: (value: ManualJournalEntryInput) => void;
  onSuggestVoucherNumber: () => Promise<void>;
  onOpeningBalanceValueChange: (value: OpeningBalanceImportInput) => void;
  onPreviewOpeningBalance: () => Promise<void>;
  onOpeningBalanceImportTextChange: (value: string) => void;
  onOpeningBalanceImportDelimiterChange: (value: string) => void;
  onPreviewOpeningBalanceText: () => Promise<void>;
  onUseOpeningBalanceTemplate: (format?: OpeningBalanceImportTemplateFormat) => void;
  onSaveOpeningBalanceProfile: () => Promise<void>;
  onPostOpeningBalance: () => Promise<void>;
  onFocusJournalEntry: (journalEntryId: string) => Promise<void>;
  onPost: () => Promise<void>;
  onVoidEntry: (entry: JournalEntrySummary) => Promise<void>;
  onOpenSourceDocument: (entry: JournalEntrySummary) => Promise<void>;
  getSourceDocumentLabel: (entry: JournalEntrySummary) => string | null;
  getSourceDocumentClientLabel: (sourceDocument: JournalEntrySourceDocument) => string;
  onRefresh: () => Promise<void>;
  onLoadMore: () => Promise<void>;
};

export function JournalWorkbenchPanel({
  accounts,
  periods,
  entries,
  page,
  filters,
  value,
  manualVoucherPreview,
  openingBalanceValue,
  openingBalancePreview,
  openingBalanceImportText,
  openingBalanceImportDelimiter,
  openingBalanceImportTextPreview,
  focusedJournalEntryId,
  focusedJournalEntry,
  sourceDocumentsByJournalEntryId,
  isBusy,
  onFiltersChange,
  onValueChange,
  onSuggestVoucherNumber,
  onOpeningBalanceValueChange,
  onPreviewOpeningBalance,
  onOpeningBalanceImportTextChange,
  onOpeningBalanceImportDelimiterChange,
  onPreviewOpeningBalanceText,
  onUseOpeningBalanceTemplate,
  onSaveOpeningBalanceProfile,
  onPostOpeningBalance,
  onFocusJournalEntry,
  onPost,
  onVoidEntry,
  onOpenSourceDocument,
  getSourceDocumentLabel,
  getSourceDocumentClientLabel,
  onRefresh,
  onLoadMore
}: JournalWorkbenchPanelProps) {
  const [isWorkWindowOpen, setIsWorkWindowOpen] = useState(false);
  const [activeWorkView, setActiveWorkView] = useState<JournalWorkWindowView>("voucher");
  const reversalPeriodState = getPostingPeriodState(toDateInputValue(new Date()), periods);
  const focusedSourceDocument = focusedJournalEntry === null
    ? null
    : sourceDocumentsByJournalEntryId[focusedJournalEntry.journalEntryId] ?? null;
  const focusedSourceDocumentClientLabel = focusedJournalEntry === null
    ? "-"
    : sourceDocumentClientLabel(
      sourceDocumentsByJournalEntryId[focusedJournalEntry.journalEntryId],
      getSourceDocumentClientLabel
    );

  function openWorkWindow(view: JournalWorkWindowView) {
    setActiveWorkView(view);
    setIsWorkWindowOpen(true);
  }

  function handleWorkWindowViewChange(view: JournalWorkWindowView) {
    if (view === "detail" && focusedJournalEntryId === "") {
      return;
    }

    setActiveWorkView(view);
  }

  async function handleOpenJournalDetail(journalEntryId: string) {
    if (journalEntryId !== focusedJournalEntryId) {
      await onFocusJournalEntry(journalEntryId);
    }

    openWorkWindow("detail");
  }

  async function handleClearFocusedJournal() {
    await onFocusJournalEntry("");

    if (activeWorkView === "detail") {
      setIsWorkWindowOpen(false);
      setActiveWorkView("voucher");
    }
  }

  return (
    <section className="journal-workbench">
      <JournalRegisterPanel
        entries={entries}
        page={page}
        filters={filters}
        focusedJournalEntryId={focusedJournalEntryId}
        focusedJournalEntry={focusedJournalEntry}
        isBusy={isBusy}
        reversalPeriodState={reversalPeriodState}
        onFiltersChange={onFiltersChange}
        onRefresh={onRefresh}
        onLoadMore={onLoadMore}
        onOpenVoucherEntry={() => openWorkWindow("voucher")}
        onOpenOpeningBalance={() => openWorkWindow("opening")}
        onOpenJournalDetail={handleOpenJournalDetail}
        onClearFocusedJournal={handleClearFocusedJournal}
        onVoidEntry={onVoidEntry}
        onOpenSourceDocument={onOpenSourceDocument}
        getSourceDocumentLabel={getSourceDocumentLabel}
      />

      {isWorkWindowOpen && (
        <JournalWorkWindow
          activeView={activeWorkView}
          detailDisabled={focusedJournalEntryId === ""}
          title={getJournalWorkWindowTitle(activeWorkView)}
          subtitle={getJournalWorkWindowSubtitle(activeWorkView, focusedJournalEntry)}
          onViewChange={handleWorkWindowViewChange}
          onClose={() => setIsWorkWindowOpen(false)}
        >
          {activeWorkView === "voucher" && (
            <JournalEntryEditorPanel
              accounts={accounts}
              periods={periods}
              value={value}
              manualVoucherPreview={manualVoucherPreview}
              isBusy={isBusy}
              onValueChange={onValueChange}
              onSuggestVoucherNumber={onSuggestVoucherNumber}
              onPost={onPost}
            />
          )}

          {activeWorkView === "opening" && (
            <OpeningBalanceImportPanel
              accounts={accounts}
              value={openingBalanceValue}
              preview={openingBalancePreview}
              importText={openingBalanceImportText}
              importDelimiter={openingBalanceImportDelimiter}
              textPreview={openingBalanceImportTextPreview}
              isBusy={isBusy}
              onValueChange={onOpeningBalanceValueChange}
              onPreviewOpeningBalance={onPreviewOpeningBalance}
              onImportTextChange={onOpeningBalanceImportTextChange}
              onImportDelimiterChange={onOpeningBalanceImportDelimiterChange}
              onPreviewImportText={onPreviewOpeningBalanceText}
              onUseTemplate={onUseOpeningBalanceTemplate}
              onSaveProfile={onSaveOpeningBalanceProfile}
              onPostOpeningBalance={onPostOpeningBalance}
            />
          )}

          {activeWorkView === "detail" && (
            <JournalEntryDetailPanel
              entry={focusedJournalEntry}
              accounts={accounts}
              sourceDocument={focusedSourceDocument}
              sourceDocumentClientLabel={focusedSourceDocumentClientLabel}
            />
          )}
        </JournalWorkWindow>
      )}
    </section>
  );
}

function getJournalWorkWindowTitle(view: JournalWorkWindowView): string {
  switch (view) {
    case "opening":
      return "Opening Balance Window";
    case "detail":
      return "Journal Detail Window";
    default:
      return "Voucher Entry Window";
  }
}

function getJournalWorkWindowSubtitle(
  view: JournalWorkWindowView,
  focusedJournalEntry: JournalEntrySummary | null
): string {
  if (view === "opening") {
    return "Setup utility and legacy import";
  }

  if (view === "detail") {
    return focusedJournalEntry === null
      ? "Select a journal from the register"
      : `${focusedJournalEntry.entryDate} / ${focusedJournalEntry.sourceReference ?? focusedJournalEntry.journalEntryId}`;
  }

  return "Manual voucher master and lines";
}

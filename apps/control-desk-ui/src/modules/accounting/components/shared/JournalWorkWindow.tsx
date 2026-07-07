import {
  ClipboardList,
  FileCheck2,
  ListTree,
  X
} from "lucide-react";
import type { ReactNode } from "react";

export type JournalWorkWindowView = "voucher" | "opening" | "detail";

type JournalWorkWindowProps = {
  activeView: JournalWorkWindowView;
  detailDisabled: boolean;
  title: string;
  subtitle: string;
  children: ReactNode;
  onViewChange: (view: JournalWorkWindowView) => void;
  onClose: () => void;
};

const journalWorkWindowTabs: Array<{
  view: JournalWorkWindowView;
  label: string;
  title: string;
  icon: typeof ClipboardList;
}> = [
  {
    view: "voucher",
    label: "Voucher",
    title: "Open manual voucher entry",
    icon: ClipboardList
  },
  {
    view: "opening",
    label: "Opening",
    title: "Open opening balance import",
    icon: FileCheck2
  },
  {
    view: "detail",
    label: "Details",
    title: "Open selected journal details",
    icon: ListTree
  }
];

export function JournalWorkWindow({
  activeView,
  detailDisabled,
  title,
  subtitle,
  children,
  onViewChange,
  onClose
}: JournalWorkWindowProps) {
  return (
    <aside
      className="journal-work-window"
      role="dialog"
      aria-label="Journal work window"
    >
      <header className="journal-work-window-header">
        <div>
          <strong>{title}</strong>
          <small>{subtitle}</small>
        </div>
        <nav className="journal-work-window-tabs" aria-label="Journal work window views">
          {journalWorkWindowTabs.map((tab) => {
            const Icon = tab.icon;
            const disabled = tab.view === "detail" && detailDisabled;

            return (
              <button
                className={activeView === tab.view ? "active" : ""}
                type="button"
                key={tab.view}
                onClick={() => onViewChange(tab.view)}
                disabled={disabled}
                title={disabled ? "Select a journal entry first" : tab.title}
              >
                <Icon size={14} />
                {tab.label}
              </button>
            );
          })}
        </nav>
        <button
          className="table-icon-button"
          type="button"
          onClick={onClose}
          title="Close journal work window"
        >
          <X size={15} />
        </button>
      </header>
      <div className="journal-work-window-body">
        {children}
      </div>
    </aside>
  );
}

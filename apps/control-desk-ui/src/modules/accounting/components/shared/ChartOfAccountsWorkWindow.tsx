import {
  ClipboardList,
  FileInput,
  ListTree,
  X
} from "lucide-react";
import type { ReactNode } from "react";

export type ChartOfAccountsWorkWindowView = "account" | "ranges" | "import" | "activity";

type ChartOfAccountsWorkWindowProps = {
  activeView: ChartOfAccountsWorkWindowView;
  activityDisabled: boolean;
  title: string;
  subtitle: string;
  children: ReactNode;
  onViewChange: (view: ChartOfAccountsWorkWindowView) => void;
  onClose: () => void;
};

const chartOfAccountsWorkWindowTabs: Array<{
  view: ChartOfAccountsWorkWindowView;
  label: string;
  title: string;
  icon: typeof ClipboardList;
}> = [
  {
    view: "account",
    label: "Account",
    title: "Open account maintenance",
    icon: ClipboardList
  },
  {
    view: "ranges",
    label: "Ranges",
    title: "Open COA range setup",
    icon: ListTree
  },
  {
    view: "import",
    label: "Import",
    title: "Open COA import preview",
    icon: FileInput
  },
  {
    view: "activity",
    label: "Activity",
    title: "Open selected account activity",
    icon: ListTree
  }
];

export function ChartOfAccountsWorkWindow({
  activeView,
  activityDisabled,
  title,
  subtitle,
  children,
  onViewChange,
  onClose
}: ChartOfAccountsWorkWindowProps) {
  return (
    <aside
      className="coa-workbench-window"
      role="dialog"
      aria-label="Chart of accounts work window"
    >
      <header className="coa-workbench-window-header">
        <div>
          <strong>{title}</strong>
          <small>{subtitle}</small>
        </div>
        <nav className="coa-workbench-window-tabs" aria-label="Chart of accounts work window views">
          {chartOfAccountsWorkWindowTabs.map((tab) => {
            const Icon = tab.icon;
            const disabled = tab.view === "activity" && activityDisabled;

            return (
              <button
                className={activeView === tab.view ? "active" : ""}
                type="button"
                key={tab.view}
                onClick={() => onViewChange(tab.view)}
                disabled={disabled}
                title={disabled ? "Open account activity from a row first" : tab.title}
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
          title="Close COA work window"
        >
          <X size={15} />
        </button>
      </header>
      <div className="coa-workbench-window-body">
        {children}
      </div>
    </aside>
  );
}

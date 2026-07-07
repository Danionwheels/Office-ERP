import { AlertCircle, CheckCircle2 } from "lucide-react";
import type { ReactNode } from "react";
import type {
  DashboardModule,
  DashboardNavigationItem,
  ModuleCommandItem
} from "../types/clientDashboardTypes";
import type { ClientDetails } from "../types/clientTypes";

type ClientDeskShellProps = {
  activeModule: DashboardModule;
  activeNavigationItem: DashboardNavigationItem;
  navigationItems: DashboardNavigationItem[];
  selectedClient: ClientDetails | null;
  commandItems: ModuleCommandItem[];
  outputCommandItems: ModuleCommandItem[];
  commandStatus: string;
  message: string;
  error: string;
  children: ReactNode;
  onModuleChange: (module: DashboardModule) => void;
};

export function ClientDeskShell({
  activeModule,
  activeNavigationItem,
  navigationItems,
  selectedClient,
  commandItems,
  outputCommandItems,
  commandStatus,
  message,
  error,
  children,
  onModuleChange
}: ClientDeskShellProps) {
  return (
    <div className="client-desk control-desk-shell">
      <aside className="control-sidebar" aria-label="Client control navigation">
        <div className="sidebar-brand">
          <div>
            <span>SafarSuite</span>
            <h1>Control Desk</h1>
          </div>
          {selectedClient !== null && (
            <span className={`status-pill ${selectedClient.status.toLowerCase()}`}>
              {selectedClient.status}
            </span>
          )}
        </div>

        <nav className="module-sidebar-nav" aria-label="Client modules">
          {navigationItems.map((item) => (
            <button
              aria-current={activeModule === item.module ? "page" : undefined}
              className={`module-nav-item ${item.tone}${
                activeModule === item.module ? " active" : ""
              }`}
              key={item.module}
              type="button"
              onClick={() => onModuleChange(item.module)}
            >
              <item.Icon size={18} />
              <span>
                <strong>{item.label}</strong>
                <small>{item.summary}</small>
              </span>
            </button>
          ))}
        </nav>
      </aside>

      <main className="control-main-window">
        <div className="status-line" aria-live="polite">
          {error !== "" && (
            <span className="status-error">
              <AlertCircle size={16} />
              {error}
            </span>
          )}
          {message !== "" && (
            <span className="status-success">
              <CheckCircle2 size={16} />
              {message}
            </span>
          )}
        </div>

        <section className="module-window">
          <header className="module-window-header">
            <div>
              <span>{selectedClient?.code ?? "No client selected"}</span>
              <h1>{activeNavigationItem.label}</h1>
              <p>{activeNavigationItem.description}</p>
            </div>
            {selectedClient !== null && (
              <div className="module-window-client">
                <span>{selectedClient.displayName}</span>
                <strong>{selectedClient.legalName}</strong>
              </div>
            )}
          </header>

          <ModuleCommandBar
            label={activeNavigationItem.label}
            items={commandItems}
            outputItems={outputCommandItems}
            status={commandStatus}
          />

          <div className="module-window-body">{children}</div>
        </section>
      </main>
    </div>
  );
}

function ModuleCommandBar({
  label,
  items,
  outputItems,
  status
}: {
  label: string;
  items: ModuleCommandItem[];
  outputItems: ModuleCommandItem[];
  status: string;
}) {
  return (
    <section className="module-command-bar" aria-label={`${label} commands`}>
      <div className="module-command-group" role="toolbar" aria-label="Module commands">
        {items.map((item) => (
          <button
            className={`module-command-button ${
              item.variant === "primary" ? "primary" : ""
            }`}
            disabled={item.disabled === true || item.onClick === undefined}
            key={item.key}
            onClick={() => {
              void item.onClick?.();
            }}
            title={item.title}
            type="button"
          >
            <item.Icon size={15} />
            <span>{item.label}</span>
          </button>
        ))}
      </div>

      <div className="module-command-group module-command-output" role="toolbar" aria-label="Output commands">
        {outputItems.map((item) => (
          <button
            className="module-command-button"
            disabled
            key={item.key}
            title={item.title}
            type="button"
          >
            <item.Icon size={15} />
            <span>{item.label}</span>
          </button>
        ))}
      </div>

      <span className="module-command-status">{status}</span>
    </section>
  );
}

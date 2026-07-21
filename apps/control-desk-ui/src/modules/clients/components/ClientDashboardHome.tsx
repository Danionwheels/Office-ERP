import { ArrowRight } from "lucide-react";
import type {
  DashboardMetric,
  DashboardModule,
  DashboardWorkQueueItem
} from "../types/clientDashboardTypes";
import type { ClientDetails } from "../types/clientTypes";
import { formatDashboardQueuePriority } from "../utils/clientDashboardModel";

type ClientDashboardHomeProps = {
  selectedClient: ClientDetails | null;
  metrics: DashboardMetric[];
  workQueueItems: DashboardWorkQueueItem[];
  onNavigate: (module: DashboardModule) => void;
};

export function ClientDashboardHome({
  selectedClient,
  metrics,
  workQueueItems,
  onNavigate
}: ClientDashboardHomeProps) {
  return (
    <section className="client-stat-window">
      <div className="client-dashboard-heading">
        <div>
          <span>{selectedClient?.code ?? "No client selected"}</span>
          <h2>{selectedClient?.displayName ?? "Select a client"}</h2>
        </div>
        {selectedClient !== null && (
          <span className={`status-pill large ${selectedClient.status.toLowerCase()}`}>
            {selectedClient.status}
          </span>
        )}
      </div>

      <div className="dashboard-metrics stat-action-grid">
        {metrics.map((metric) => (
          <button
            className={`dashboard-metric stat-action ${metric.tone}`}
            key={metric.label}
            type="button"
            onClick={() => onNavigate(metric.module)}
          >
            <metric.Icon size={20} />
            <div>
              <span>{metric.label}</span>
              <strong>{metric.value}</strong>
              <small>{metric.summary}</small>
            </div>
            <ArrowRight className="stat-action-arrow" size={16} />
          </button>
        ))}
      </div>

      <DashboardWorkQueue items={workQueueItems} onNavigate={onNavigate} />
    </section>
  );
}

function DashboardWorkQueue({
  items,
  onNavigate
}: {
  items: DashboardWorkQueueItem[];
  onNavigate: (module: DashboardModule) => void;
}) {
  const openItemCount = items.filter((item) => item.priority !== "done").length;

  return (
    <section className="dashboard-work-queue" aria-label="Dashboard work queue">
      <div className="dashboard-work-queue-heading">
        <div>
          <span>Daily desk</span>
          <strong>Work queue</strong>
        </div>
        <em>{openItemCount === 0 ? "Clear" : `${openItemCount} open`}</em>
      </div>

      <div className="dashboard-work-queue-frame">
        <table className="dashboard-work-queue-table">
          <thead>
            <tr>
              <th scope="col">Priority</th>
              <th scope="col">Area</th>
              <th scope="col">Work item</th>
              <th scope="col">Status</th>
              <th scope="col">Next step</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr className={item.priority} key={item.key}>
                <td>
                  <span className={`dashboard-queue-priority ${item.priority}`}>
                    {formatDashboardQueuePriority(item.priority)}
                  </span>
                </td>
                <td>
                  <span className="dashboard-queue-area">
                    <item.Icon size={15} />
                    {item.area}
                  </span>
                </td>
                <td>
                  <strong>{item.label}</strong>
                  <small>{item.detail}</small>
                </td>
                <td>{item.status}</td>
                <td>
                  <button
                    className="dashboard-queue-action"
                    type="button"
                    onClick={() => onNavigate(item.module)}
                    title={item.actionLabel}
                  >
                    <ArrowRight size={14} />
                    <span>{item.actionLabel}</span>
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

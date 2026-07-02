import { Activity, Cloud, RefreshCw } from "lucide-react";
import type { ClientDetails } from "../../clients/types/clientTypes";
import type { ControlCloudInstallationStatus } from "../types/controlCloudTypes";

type CloudInstallationStatusPanelProps = {
  client: ClientDetails | null;
  installationId: string;
  status: ControlCloudInstallationStatus | null;
  isBusy: boolean;
  onInstallationIdChange: (value: string) => void;
  onRefresh: () => Promise<void>;
};

export function CloudInstallationStatusPanel({
  client,
  installationId,
  status,
  isBusy,
  onInstallationIdChange,
  onRefresh
}: CloudInstallationStatusPanelProps) {
  const latestHeartbeat = status?.latestHeartbeat ?? null;
  const latestEntitlement = status?.latestEntitlement ?? null;
  const commandStatus = status?.commandStatus ?? null;
  const statusText =
    latestHeartbeat?.licenseStatus ?? status?.installationStatus ?? "Not loaded";

  return (
    <section className="client-panel cloud-status-panel">
      <div className="client-panel-heading">
        <div>
          <span>Control Cloud</span>
          <strong>{statusText}</strong>
        </div>
        {status !== null && (
          <span className={`status-pill ${statusClass(statusText)}`}>
            {statusText}
          </span>
        )}
      </div>

      <div className="entitlement-action-row">
        <label className="cloud-installation-field">
          <span>Installation</span>
          <input
            type="text"
            value={installationId}
            disabled={isBusy}
            maxLength={160}
            onChange={(event) => onInstallationIdChange(event.target.value)}
          />
        </label>
        <button
          className="icon-button"
          type="button"
          disabled={isBusy || client === null || installationId.trim() === ""}
          onClick={onRefresh}
          title="Refresh cloud installation status"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
        <span className="billing-small-fact">
          {client === null ? "Select a client" : client.code}
        </span>
      </div>

      {status === null ? (
        <div className="client-empty-state entitlement-empty">
          Cloud status not loaded
        </div>
      ) : (
        <>
          <dl className="entitlement-facts cloud-status-facts">
            <div>
              <dt>Installation</dt>
              <dd>{status.installationStatus}</dd>
            </div>
            <div>
              <dt>License</dt>
              <dd>{latestHeartbeat?.licenseStatus ?? "No heartbeat"}</dd>
            </div>
            <div>
              <dt>Heartbeat</dt>
              <dd>{formatNullableDateTime(latestHeartbeat?.receivedAtUtc)}</dd>
            </div>
            <div>
              <dt>Paid until</dt>
              <dd>
                {latestHeartbeat?.paidUntil
                  ?? latestEntitlement?.paidUntil
                  ?? "Not issued"}
              </dd>
            </div>
            <div>
              <dt>Entitlement</dt>
              <dd>{status.latestEntitlementVersion}</dd>
            </div>
            <div>
              <dt>Commands</dt>
              <dd>{commandStatus?.pendingCommandCount ?? 0} pending</dd>
            </div>
          </dl>

          <div className="cloud-status-notes">
            <span>
              <Cloud size={15} />
              Last bundle {formatNullableDateTime(status.lastBundleIssuedAtUtc)}
            </span>
            <span>
              <Activity size={15} />
              {commandStatus?.latestCommandStatus ?? "No command"} command
            </span>
          </div>
        </>
      )}
    </section>
  );
}

function statusClass(value: string): string {
  const normalized = value.trim().toLowerCase();

  if (normalized === "active" || normalized === "healthy" || normalized === "registered") {
    return "active";
  }

  if (normalized === "suspended" || normalized === "expired" || normalized === "failed") {
    return "suspended";
  }

  return "draft";
}

function formatNullableDateTime(value: string | null | undefined): string {
  if (value === null || value === undefined || value.trim() === "") {
    return "Not available";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

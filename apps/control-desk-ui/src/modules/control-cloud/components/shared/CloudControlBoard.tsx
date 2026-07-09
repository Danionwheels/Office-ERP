import {
  Activity,
  Cloud,
  FileClock,
  Gauge,
  History,
  KeyRound,
  ListChecks,
  Network,
  ServerCog,
  ShieldCheck,
  type LucideIcon
} from "lucide-react";
import type { CloudControlKey, CloudControlRow } from "../../types/cloudWorkspaceTypes";

const controlIcons: Record<CloudControlKey, LucideIcon> = {
  cloudLink: Cloud,
  installation: Cloud,
  heartbeat: Activity,
  pairing: Network,
  entitlement: ShieldCheck,
  appActivation: KeyRound,
  commands: ListChecks,
  diagnostics: Gauge,
  deployment: ServerCog,
  history: FileClock
};

export function CloudControlBoard({ rows }: { rows: CloudControlRow[] }) {
  return (
    <div className="cloud-control-board">
      {rows.map((row) => {
        const Icon = controlIcons[row.key] ?? History;

        return (
          <article className={`cloud-control-card ${row.tone}`} key={row.key}>
            <Icon size={17} />
            <span>
              <strong>{row.label}</strong>
              <em>{row.status}</em>
              <small>{row.detail}</small>
            </span>
          </article>
        );
      })}
    </div>
  );
}

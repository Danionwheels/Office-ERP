import {
  BadgeCheck,
  FileText,
  Gauge,
  ReceiptText,
  ShieldCheck,
  WalletCards,
  type LucideIcon
} from "lucide-react";
import type { ProductModule } from "../../../contracts/types/contractTypes";
import type { EntitlementSnapshot } from "../../types/entitlementTypes";
import type {
  EntitlementControlKey,
  EntitlementControlRow,
  EntitlementFact
} from "../../types/entitlementWorkspaceTypes";
import {
  getEntitlementFacts,
  getEntitlementModuleRows
} from "../../utils/entitlementSnapshotModel";

const controlIcons: Record<EntitlementControlKey, LucideIcon> = {
  invoice: ReceiptText,
  receipt: WalletCards,
  snapshot: ShieldCheck,
  limits: Gauge,
  source: FileText
};

export function EntitlementControlBoard({ rows }: { rows: EntitlementControlRow[] }) {
  return (
    <div className="entitlement-control-board">
      {rows.map((row) => {
        const Icon = controlIcons[row.key];

        return (
          <article className={`entitlement-control-card ${row.tone}`} key={row.key}>
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

export function EntitlementSnapshotSummary({
  snapshot,
  sourceInvoiceNumber
}: {
  snapshot: EntitlementSnapshot;
  sourceInvoiceNumber: string | null;
}) {
  const facts = getEntitlementFacts(snapshot);

  return (
    <div className="entitlement-snapshot-summary">
      <dl className="entitlement-facts">
        {facts.map((fact) => (
          <EntitlementFactItem fact={fact} key={fact.label} />
        ))}
      </dl>

      <div className="entitlement-source-note">
        <BadgeCheck size={14} />
        <span title={snapshot.approvalReason}>
          Approved {formatApprovalTime(snapshot.approvedAtUtc)}: {snapshot.approvalReason}
        </span>
      </div>

      {sourceInvoiceNumber !== null && (
        <div className="entitlement-source-note">
          <BadgeCheck size={14} />
          <span>Source invoice {sourceInvoiceNumber}</span>
        </div>
      )}
    </div>
  );
}

export function EntitlementModuleRegister({
  modules,
  productModules
}: {
  modules: EntitlementSnapshot["modules"];
  productModules: ProductModule[];
}) {
  const moduleRows = getEntitlementModuleRows(modules, productModules);

  if (moduleRows.length === 0) {
    return (
      <div className="entitlement-module-register">
        <span className="entitlement-module-register-empty">No modules</span>
      </div>
    );
  }

  return (
    <div className="entitlement-module-register">
      <div className="entitlement-module-grid" aria-label="Entitlement modules">
        {moduleRows.map((module) => (
          <article
            className={`entitlement-module-card ${module.isEnabled ? "enabled" : "disabled"}`}
            key={module.moduleCode}
          >
            <header>
              <span>
                <strong>{module.displayName}</strong>
                <small>{module.moduleCode}</small>
              </span>
              <em>{module.isEnabled ? "Enabled" : "Disabled"}</em>
            </header>
            <p>{module.billingText}</p>
            <small>{module.meta}</small>
          </article>
        ))}
      </div>
    </div>
  );
}

export function EntitlementFeatureLimitRegister({
  featureLimits
}: {
  featureLimits: EntitlementSnapshot["featureLimits"];
}) {
  if ((featureLimits ?? []).length === 0) {
    return null;
  }

  return (
    <section className="entitlement-feature-limit-register" aria-label="Entitlement feature limits">
      <header>
        <span>Feature limits</span>
        <strong>{featureLimits.length}</strong>
      </header>
      <div>
        {featureLimits.map((limit) => (
          <span key={`${limit.moduleCode}-${limit.featureCode}`}>
            <strong>{limit.moduleCode}.{limit.featureCode}</strong>
            <small>{limit.limitValue} {limit.unit}</small>
          </span>
        ))}
      </div>
    </section>
  );
}

function formatApprovalTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function EntitlementFactItem({ fact }: { fact: EntitlementFact }) {
  return (
    <div>
      <dt>{fact.label}</dt>
      <dd>{fact.value}</dd>
    </div>
  );
}

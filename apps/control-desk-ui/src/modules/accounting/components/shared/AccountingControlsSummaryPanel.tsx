import { Hash, Settings2 } from "lucide-react";
import type {
  AccountingControlFact,
  AccountingControlTone
} from "../../utils/accountingControlsWorkspaceModel";

type AccountingControlsSummaryPanelProps = {
  controlFacts: AccountingControlFact[];
  voucherFacts: AccountingControlFact[];
};

export function AccountingControlsSummaryPanel({
  controlFacts,
  voucherFacts
}: AccountingControlsSummaryPanelProps) {
  return (
    <div className="accounting-controls-overview-grid">
      <section className="client-panel accounting-controls-summary-panel">
        <div className="client-panel-heading">
          <div>
            <span>Control accounts</span>
            <strong>Configured posting flow</strong>
          </div>
          <Settings2 size={17} />
        </div>
        <div className="accounting-controls-summary">
          {controlFacts.map((fact) => (
            <ControlFact
              key={fact.label}
              label={fact.label}
              value={fact.value}
              tone={fact.tone}
            />
          ))}
        </div>
      </section>

      <section className="client-panel accounting-controls-summary-panel">
        <div className="client-panel-heading">
          <div>
            <span>Voucher numbering</span>
            <strong>Source rules</strong>
          </div>
          <Hash size={17} />
        </div>
        <div className="voucher-numbering-status-row">
          {voucherFacts.map((fact) => (
            <span className={fact.tone} key={fact.label} title={fact.title}>
              <small>{fact.label}</small>
              <strong>{fact.value}</strong>
            </span>
          ))}
        </div>
      </section>
    </div>
  );
}

function ControlFact({
  label,
  value,
  tone = "neutral"
}: {
  label: string;
  value: string;
  tone?: AccountingControlTone;
}) {
  return (
    <span className={tone}>
      <Settings2 size={15} />
      <small>{label}</small>
      <strong>{value}</strong>
    </span>
  );
}

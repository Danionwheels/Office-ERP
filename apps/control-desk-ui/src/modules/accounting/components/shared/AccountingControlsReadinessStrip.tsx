import type { AccountingControlFact } from "../../utils/accountingControlsWorkspaceModel";

type AccountingControlsReadinessStripProps = {
  facts: AccountingControlFact[];
};

export function AccountingControlsReadinessStrip({
  facts
}: AccountingControlsReadinessStripProps) {
  return (
    <div className="accounting-controls-readiness-row">
      {facts.map((fact) => (
        <span className={fact.tone} key={fact.label} title={fact.title}>
          <small>{fact.label}</small>
          <strong>{fact.value}</strong>
        </span>
      ))}
    </div>
  );
}

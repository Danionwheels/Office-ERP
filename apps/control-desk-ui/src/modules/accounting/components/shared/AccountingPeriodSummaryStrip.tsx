import type { AccountingPeriod } from "../../types/accountingTypes";

type AccountingPeriodSummaryStripProps = {
  currentPeriod: AccountingPeriod | null;
  openPeriods: number;
  closedPeriods: number;
  totalPeriods: number;
};

export function AccountingPeriodSummaryStrip({
  currentPeriod,
  openPeriods,
  closedPeriods,
  totalPeriods
}: AccountingPeriodSummaryStripProps) {
  return (
    <div className="accounting-period-summary-row">
      <article className="client-panel accounting-period-summary-card">
        <span>Current</span>
        <strong>{currentPeriod?.name ?? "Not opened"}</strong>
      </article>
      <article className="client-panel accounting-period-summary-card">
        <span>Open</span>
        <strong>{openPeriods}</strong>
      </article>
      <article className="client-panel accounting-period-summary-card">
        <span>Closed</span>
        <strong>{closedPeriods}</strong>
      </article>
      <article className="client-panel accounting-period-summary-card">
        <span>Total</span>
        <strong>{totalPeriods}</strong>
      </article>
    </div>
  );
}

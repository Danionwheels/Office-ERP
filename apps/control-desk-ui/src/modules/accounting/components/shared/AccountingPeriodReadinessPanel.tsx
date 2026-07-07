import type { AccountingPeriodCloseReadiness } from "../../types/accountingTypes";
import {
  formatMoney,
  formatReadinessCheckMessage,
  getAccountingPeriodReadinessFacts,
  getCurrencyCloseState
} from "../../utils/accountingPeriodsWorkspaceModel";

type AccountingPeriodReadinessPanelProps = {
  readiness: AccountingPeriodCloseReadiness;
};

export function AccountingPeriodReadinessPanel({
  readiness
}: AccountingPeriodReadinessPanelProps) {
  return (
    <section className={`client-panel accounting-period-readiness-panel${
      readiness.canClose ? " ready" : " blocked"
    }`}>
      <div className="client-panel-heading">
        <div>
          <span>{readiness.period.name}</span>
          <strong>Close readiness</strong>
        </div>
        <span className={`status-pill ${readiness.canClose ? "open" : "voided"}`}>
          {readiness.canClose ? "Ready" : "Blocked"}
        </span>
      </div>
      <div className="accounting-period-readiness-summary">
        {getAccountingPeriodReadinessFacts(readiness).map((fact) => (
          <span className={fact.tone} key={fact.label} title={fact.title}>
            <small>{fact.label}</small>
            <strong>{fact.value}</strong>
          </span>
        ))}
      </div>
      <div className="accounting-period-readiness-grid">
        {readiness.checks.map((check) => (
          <article
            className={`accounting-period-readiness-check ${check.status.toLowerCase()}`}
            key={check.code}
          >
            <span>{check.status}</span>
            <strong>{check.code}</strong>
            <small>{formatReadinessCheckMessage(check)}</small>
          </article>
        ))}
      </div>
      <table className="accounting-period-currency-table accounting-period-close-journal-table">
        <thead>
          <tr>
            <th>Currency</th>
            <th>Close state</th>
            <th>Debit</th>
            <th>Credit</th>
            <th>Difference</th>
            <th>Posted</th>
            <th>Draft</th>
          </tr>
        </thead>
        <tbody>
          {readiness.currencies.length === 0 ? (
            <tr>
              <td colSpan={7}>No journal activity</td>
            </tr>
          ) : (
            readiness.currencies.map((currency) => {
              const closeState = getCurrencyCloseState(currency);

              return (
                <tr key={currency.currencyCode}>
                  <td>
                    <strong>{currency.currencyCode}</strong>
                  </td>
                  <td>
                    <span
                      className={`accounting-period-currency-state ${closeState.tone}`}
                      title={closeState.title}
                    >
                      {closeState.label}
                    </span>
                  </td>
                  <td className="numeric">{formatMoney(currency.totalDebit)}</td>
                  <td className="numeric">{formatMoney(currency.totalCredit)}</td>
                  <td className="numeric">{formatMoney(currency.difference)}</td>
                  <td className="numeric">{currency.postedJournalCount}</td>
                  <td className="numeric">{currency.draftJournalCount}</td>
                </tr>
              );
            })
          )}
        </tbody>
      </table>
    </section>
  );
}

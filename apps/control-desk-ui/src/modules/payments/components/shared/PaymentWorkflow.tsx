import { ArrowRight, ListTree } from "lucide-react";
import type {
  PaymentJournalLine,
  PaymentStep,
  PaymentStepItem
} from "../../types/paymentWorkflowTypes";
import {
  cleanOptional,
  formatMoney,
  getPaymentPostingBridgeItems,
  getPaymentStepCue,
  shortAccountId
} from "../../utils/paymentWorkflowModel";

export function PaymentFlowRegister({
  activeStep,
  onSelectStep,
  steps
}: {
  activeStep: PaymentStep;
  onSelectStep: (step: PaymentStep) => void;
  steps: PaymentStepItem[];
}) {
  return (
    <nav className="billing-step-board payment-step-board" aria-label="Payment workflow">
      {steps.map((step, index) => {
        const isActive = activeStep === step.step;

        return (
          <button
            aria-current={isActive ? "step" : undefined}
            className={`billing-step-board-item payment-step-board-item ${step.tone}${isActive ? " active" : ""}`}
            key={step.step}
            type="button"
            onClick={() => onSelectStep(step.step)}
            title={`Open ${step.label}`}
          >
            <span className="billing-step-number">{index + 1}</span>
            <step.Icon size={16} />
            <span>
              <strong>{step.label}</strong>
              <small>{step.summary}</small>
            </span>
            <em>{getPaymentStepCue(step.step)}</em>
            <ArrowRight size={14} />
          </button>
        );
      })}
    </nav>
  );
}

export function PaymentPostingBridge({
  accountsReceivableAccountId,
  amount,
  cashOrBankAccountId,
  currencyCode,
  documentDetail,
  documentLabel,
  journalEntryId,
  journalStatus,
  postingDate,
  postingVerb
}: {
  accountsReceivableAccountId: string;
  amount: number;
  cashOrBankAccountId: string;
  currencyCode: string;
  documentDetail: string;
  documentLabel: string;
  journalEntryId?: string | null;
  journalStatus?: string | null;
  postingDate: string;
  postingVerb: string;
}) {
  const items = getPaymentPostingBridgeItems({
    accountsReceivableAccountId,
    amount,
    cashOrBankAccountId,
    currencyCode,
    documentDetail,
    documentLabel,
    journalEntryId,
    journalStatus,
    postingDate,
    postingVerb
  });

  return (
    <section className="billing-posting-bridge payment-posting-bridge" aria-label={`${postingVerb} posting bridge`}>
      <div className="billing-subform-heading">
        <ListTree size={16} />
        <strong>Posting bridge</strong>
      </div>
      <div className="billing-posting-bridge-grid">
        {items.map((item) => (
          <span className={`billing-posting-bridge-item ${item.tone}`} key={item.label}>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
            <em>{item.detail}</em>
          </span>
        ))}
      </div>
    </section>
  );
}

export function PaymentPostingResult({
  actionLabel,
  actionStatus,
  amount,
  balanceLabel,
  balanceValue,
  currencyCode,
  journalEntryId,
  journalEntryStatus,
  journalLines,
  onViewJournalEntry,
  totalCredit,
  totalDebit,
  isBusy,
  tableLabel
}: {
  actionLabel: string;
  actionStatus: string;
  amount: number;
  balanceLabel: string;
  balanceValue: number;
  currencyCode: string;
  journalEntryId: string;
  journalEntryStatus: string;
  journalLines: PaymentJournalLine[];
  onViewJournalEntry: (journalEntryId: string) => Promise<void>;
  totalCredit: number;
  totalDebit: number;
  isBusy: boolean;
  tableLabel: string;
}) {
  return (
    <div className="payment-posting-result">
      <dl className="payment-result-facts">
        <div>
          <dt>{actionLabel}</dt>
          <dd>{actionStatus}</dd>
        </div>
        <div>
          <dt>Amount</dt>
          <dd>{formatMoney(amount, currencyCode)}</dd>
        </div>
        <div>
          <dt>{balanceLabel}</dt>
          <dd>{formatMoney(balanceValue, currencyCode)}</dd>
        </div>
        <div>
          <dt>Journal</dt>
          <dd className="fact-action-value">
            <span>{journalEntryStatus}</span>
            <button
              className="table-icon-button"
              type="button"
              onClick={() => void onViewJournalEntry(journalEntryId)}
              disabled={isBusy}
              title="Open related journal"
            >
              <ListTree size={14} />
            </button>
          </dd>
        </div>
        <div>
          <dt>Debit</dt>
          <dd>{formatMoney(totalDebit, currencyCode)}</dd>
        </div>
        <div>
          <dt>Credit</dt>
          <dd>{formatMoney(totalCredit, currencyCode)}</dd>
        </div>
      </dl>

      <PaymentJournalRegister
        currencyCode={currencyCode}
        emptyText="No debit/credit lines"
        lines={journalLines}
        title={tableLabel}
      />
    </div>
  );
}

export function PaymentJournalRegister({
  currencyCode,
  emptyText,
  lines,
  title
}: {
  currencyCode: string;
  emptyText: string;
  lines: PaymentJournalLine[];
  title: string;
}) {
  return (
    <div className="billing-journal-register payment-journal-register">
      <div className="billing-journal-register-title">
        <ListTree size={15} />
        <strong>{title}</strong>
        <span>{lines.length} lines</span>
      </div>
      <table className="billing-lines-table payment-journal-table">
        <thead>
          <tr>
            <th>Account</th>
            <th>Description</th>
            <th className="numeric">Debit</th>
            <th className="numeric">Credit</th>
          </tr>
        </thead>
        <tbody>
          {lines.length === 0 ? (
            <tr className="billing-empty-row">
              <td colSpan={4}>{emptyText}</td>
            </tr>
          ) : (
            lines.map((line, index) => (
              <tr key={`${line.ledgerAccountId}-${line.debit}-${line.credit}-${index}`}>
                <td>
                  <PaymentAccountReference line={line} />
                </td>
                <td>{cleanOptional(line.description) ?? "-"}</td>
                <td className="numeric">{line.debit === 0 ? "-" : formatMoney(line.debit, currencyCode)}</td>
                <td className="numeric">{line.credit === 0 ? "-" : formatMoney(line.credit, currencyCode)}</td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

function PaymentAccountReference({ line }: { line: PaymentJournalLine }) {
  const accountCode = cleanOptional(line.ledgerAccountCode) ?? shortAccountId(line.ledgerAccountId);
  const accountName = cleanOptional(line.ledgerAccountName) ?? line.ledgerAccountId;
  const accountLevel = cleanOptional(line.ledgerAccountLevel);
  const metadata = [
    cleanOptional(line.ledgerAccountType),
    cleanOptional(line.ledgerAccountNormalBalance),
    accountLevel === null ? null : `Level ${accountLevel}`,
    line.isPostingAccount === null || line.isPostingAccount === undefined
      ? null
      : line.isPostingAccount
        ? "Posting"
        : "Non-posting",
    cleanOptional(line.ledgerAccountStatus)
  ].filter((value): value is string => value !== null);

  return (
    <span className="billing-account-reference">
      <strong>{accountCode}</strong>
      <small>{accountName}</small>
      {metadata.length > 0 ? <em>{metadata.join(" / ")}</em> : null}
    </span>
  );
}

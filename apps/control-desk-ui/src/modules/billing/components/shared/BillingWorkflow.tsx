import { ArrowRight, ListTree } from "lucide-react";
import type {
  ClientAccountingProfile
} from "../../../clients/types/clientTypes";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRuleFormInput,
  InvoiceDraft,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccountFormInput
} from "../../types/billingTypes";
import type {
  BillingJournalLine,
  BillingStep,
  BillingStepItem
} from "../../types/billingWorkflowTypes";
import {
  cleanOptional,
  formatLedgerAccountCode,
  formatMoney,
  getBillingPostingBridgeItems,
  getBillingStepCue,
  shortAccountId
} from "../../utils/billingWorkflowModel";

export function BillingFlowRegister({
  activeStep,
  onSelectStep,
  steps
}: {
  activeStep: BillingStep;
  onSelectStep: (step: BillingStep) => void;
  steps: BillingStepItem[];
}) {
  return (
    <nav className="billing-step-board" aria-label="Billing workflow">
      {steps.map((step, index) => {
        const isActive = activeStep === step.step;

        return (
          <button
            aria-current={isActive ? "step" : undefined}
            className={`billing-step-board-item ${step.tone}${isActive ? " active" : ""}`}
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
            <em>{getBillingStepCue(step.step)}</em>
            <ArrowRight size={14} />
          </button>
        );
      })}
    </nav>
  );
}

export function AccountDefaults({ account }: { account: LedgerAccountFormInput }) {
  return (
    <div className="billing-account-defaults">
      <span>{formatLedgerAccountCode(account.code)}</span>
      <span>{account.type}</span>
      <span>{account.normalBalance}</span>
      <span>{account.isPostingAccount ? "Posting account" : "Header account"}</span>
    </div>
  );
}

export function BillingPostingBridge({
  accountingProfile,
  chargeCodeValue,
  chargeCodes,
  chargeRuleValue,
  invoiceDraft,
  issueInvoiceValue,
  issuedInvoice
}: {
  accountingProfile: ClientAccountingProfile | null;
  chargeCodeValue: ChargeCodeFormInput;
  chargeCodes: ChargeCodeLookup[];
  chargeRuleValue: ClientChargeRuleFormInput;
  invoiceDraft: InvoiceDraft;
  issueInvoiceValue: IssueInvoiceFormInput;
  issuedInvoice: IssuedInvoice | null;
}) {
  const items = getBillingPostingBridgeItems({
    accountingProfile,
    chargeCodeValue,
    chargeCodes,
    chargeRuleValue,
    invoiceDraft,
    issueInvoiceValue,
    issuedInvoice
  });

  return (
    <section className="billing-posting-bridge" aria-label="Billing posting bridge">
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

export function BillingPostingResult({
  actionLabel,
  actionStatus,
  amount,
  className,
  currencyCode,
  journalEntryId,
  journalEntryStatus,
  journalLabel = "Journal",
  journalLines,
  onViewJournalEntry,
  totalCredit,
  totalDebit,
  isBusy,
  tableLabel
}: {
  actionLabel: string;
  actionStatus: string;
  amount?: number;
  className: string;
  currencyCode: string;
  journalEntryId: string;
  journalEntryStatus: string;
  journalLabel?: string;
  journalLines: BillingJournalLine[];
  onViewJournalEntry: (journalEntryId: string) => Promise<void>;
  totalCredit: number;
  totalDebit: number;
  isBusy: boolean;
  tableLabel: string;
}) {
  return (
    <div className={`billing-posting-result ${className}`}>
      <dl className="billing-result-facts issued">
        <div>
          <dt>{actionLabel}</dt>
          <dd>{actionStatus}</dd>
        </div>
        {amount !== undefined && (
          <div>
            <dt>Amount</dt>
            <dd>{formatMoney(amount, currencyCode)}</dd>
          </div>
        )}
        <div>
          <dt>{journalLabel}</dt>
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

      <BillingJournalRegister
        currencyCode={currencyCode}
        emptyText="No debit/credit lines"
        lines={journalLines}
        title={tableLabel}
      />
    </div>
  );
}

function BillingJournalRegister({
  currencyCode,
  emptyText,
  lines,
  title
}: {
  currencyCode: string;
  emptyText: string;
  lines: BillingJournalLine[];
  title: string;
}) {
  return (
    <div className="billing-journal-register">
      <div className="billing-journal-register-title">
        <ListTree size={15} />
        <strong>{title}</strong>
        <span>{lines.length} lines</span>
      </div>
      <table className="billing-lines-table billing-journal-table">
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
                  <BillingAccountReference line={line} />
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

function BillingAccountReference({ line }: { line: BillingJournalLine }) {
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

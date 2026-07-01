import {
  AlertCircle,
  Banknote,
  CheckCircle2,
  PlusCircle,
  Receipt,
  type LucideIcon
} from "lucide-react";
import { type FormEvent, useState } from "react";
import type {
  InvoiceDraft,
  IssuedInvoice,
  LedgerAccountFormInput
} from "../../billing/types/billingTypes";
import type { ClientAccountingProfile } from "../../clients/types/clientTypes";
import type {
  RecordedInvoicePayment,
  RecordInvoicePaymentInput
} from "../types/paymentTypes";

type PaymentReceiptPanelProps = {
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
  accountingProfile: ClientAccountingProfile | null;
  cashAccountValue: LedgerAccountFormInput;
  paymentValue: RecordInvoicePaymentInput;
  recordedPayment: RecordedInvoicePayment | null;
  isBusy: boolean;
  onCashAccountChange: (value: LedgerAccountFormInput) => void;
  onPaymentChange: (value: RecordInvoicePaymentInput) => void;
  onCreateCashAccount: () => Promise<void>;
  onRecordPayment: () => Promise<void>;
};

type PaymentStep = "readiness" | "cash" | "receipt" | "result";

type PaymentStepItem = {
  step: PaymentStep;
  label: string;
  summary: string;
  tone: "neutral" | "ready" | "warning";
  Icon: LucideIcon;
};

export function PaymentReceiptPanel({
  invoiceDraft,
  issuedInvoice,
  accountingProfile,
  cashAccountValue,
  paymentValue,
  recordedPayment,
  isBusy,
  onCashAccountChange,
  onPaymentChange,
  onCreateCashAccount,
  onRecordPayment
}: PaymentReceiptPanelProps) {
  const [activePaymentStep, setActivePaymentStep] = useState<PaymentStep>("readiness");

  async function handleCreateCashAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreateCashAccount();
  }

  async function handleRecordPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onRecordPayment();
  }

  const hasCashAccount = paymentValue.cashOrBankAccountId.trim() !== "";
  const hasReceivableAccount = paymentValue.accountsReceivableAccountId.trim() !== "";
  const canEditReceipt =
    issuedInvoice !== null
    && invoiceDraft !== null
    && invoiceDraft.balanceDue > 0
    && !isBusy;
  const canRecordPayment =
    canEditReceipt
    && hasCashAccount
    && hasReceivableAccount;
  const paymentSteps = getPaymentStepItems({
    invoiceDraft,
    issuedInvoice,
    accountingProfile,
    hasCashAccount,
    hasReceivableAccount,
    recordedPayment,
    canRecordPayment
  });
  const activePaymentStepItem =
    paymentSteps.find((step) => step.step === activePaymentStep) ?? paymentSteps[0];

  return (
    <section className="payment-workspace billing-workspace billing-step-workspace">
      <header className="billing-step-header">
        <div>
          <span>Payments</span>
          <h2>{activePaymentStepItem.label}</h2>
        </div>
        <div className="billing-step-summary-grid">
          {paymentSteps.map((step) => (
            <button
              className={`billing-step-summary payment-step-summary ${step.tone}${
                activePaymentStep === step.step ? " active" : ""
              }`}
              key={step.step}
              type="button"
              onClick={() => setActivePaymentStep(step.step)}
            >
              <step.Icon size={16} />
              <span>
                <strong>{step.label}</strong>
                <small>{step.summary}</small>
              </span>
            </button>
          ))}
        </div>
      </header>

      <section
        className={`client-panel billing-light-panel payment-readiness-panel${
          activePaymentStep === "readiness" ? "" : " billing-step-hidden"
        }`}
      >
        <div className="client-panel-heading">
          <div>
            <span>Payments</span>
            <strong>Readiness</strong>
          </div>
          <span className={`status-pill ${canRecordPayment ? "active" : "draft"}`}>
            {canRecordPayment ? "Ready" : "Pending"}
          </span>
        </div>

        <dl className="payment-result-facts payment-readiness-facts">
          <div>
            <dt>Invoice</dt>
            <dd>{invoiceDraft === null ? "No draft" : invoiceDraft.status}</dd>
          </div>
          <div>
            <dt>Balance</dt>
            <dd>
              {invoiceDraft === null
                ? "0.00 PKR"
                : formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}
            </dd>
          </div>
          <div>
            <dt>AR account</dt>
            <dd>{hasReceivableAccount ? "Linked" : "Missing"}</dd>
          </div>
          <div>
            <dt>Cash/bank</dt>
            <dd>{hasCashAccount ? "Linked" : "Missing"}</dd>
          </div>
          <div>
            <dt>Issue</dt>
            <dd>{issuedInvoice === null ? "Required" : issuedInvoice.invoiceStatus}</dd>
          </div>
          <div>
            <dt>Receipt</dt>
            <dd>{recordedPayment === null ? "Pending" : recordedPayment.paymentStatus}</dd>
          </div>
        </dl>

        <div className="payment-readiness-actions">
          <button
            className="icon-button"
            type="button"
            onClick={() => setActivePaymentStep("cash")}
            title="Open cash or bank account"
          >
            <Banknote size={16} />
            Cash account
          </button>
          <button
            className="icon-button primary"
            type="button"
            onClick={() => setActivePaymentStep("receipt")}
            disabled={invoiceDraft === null || issuedInvoice === null}
            title="Open receipt"
          >
            <Receipt size={16} />
            Receipt
          </button>
        </div>
      </section>

      <form
        className={`client-panel billing-light-panel payment-cash-panel${
          activePaymentStep === "cash" ? "" : " billing-step-hidden"
        }`}
        onSubmit={handleCreateCashAccount}
      >
        <div className="client-panel-heading">
          <div>
            <span>Payments</span>
            <strong>Cash or bank account</strong>
          </div>
          <span className={`status-pill ${hasCashAccount ? "active" : "draft"}`}>
            {hasCashAccount ? "Linked" : "Missing"}
          </span>
        </div>
        <div className="billing-subform-heading">
          <Banknote size={16} />
          <strong>Posting account</strong>
        </div>
        <div className="payment-form-grid account">
          <label className="form-field">
            <span>Code</span>
            <input
              value={cashAccountValue.code}
              onChange={(event) =>
                onCashAccountChange({
                  ...cashAccountValue,
                  code: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy}
              maxLength={32}
            />
          </label>
          <label className="form-field">
            <span>Name</span>
            <input
              value={cashAccountValue.name}
              onChange={(event) =>
                onCashAccountChange({
                  ...cashAccountValue,
                  name: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
        </div>
        <div className="billing-action-row">
          <button className="icon-button" type="submit" disabled={isBusy} title="Create cash or bank account">
            <PlusCircle size={16} />
            Create
          </button>
          {hasCashAccount && (
            <span className="billing-small-fact">{paymentValue.cashOrBankAccountId}</span>
          )}
        </div>
      </form>

      <form
        className={`client-panel billing-light-panel payment-receipt-panel${
          activePaymentStep === "receipt" ? "" : " billing-step-hidden"
        }`}
        onSubmit={handleRecordPayment}
      >
        <div className="client-panel-heading">
          <div>
            <span>Payments</span>
            <strong>Receipt posting</strong>
          </div>
          {invoiceDraft !== null && (
            <span className={`status-pill ${statusClass(invoiceDraft.status)}`}>
              {invoiceDraft.status}
            </span>
          )}
        </div>

        <dl className="payment-result-facts payment-invoice-facts">
          <div>
            <dt>Invoice</dt>
            <dd>{invoiceDraft?.invoiceNumber ?? "No draft"}</dd>
          </div>
          <div>
            <dt>Total</dt>
            <dd>
              {invoiceDraft === null
                ? "0.00 PKR"
                : formatMoney(invoiceDraft.totalAmount, invoiceDraft.currencyCode)}
            </dd>
          </div>
          <div>
            <dt>Balance</dt>
            <dd>
              {invoiceDraft === null
                ? "0.00 PKR"
                : formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}
            </dd>
          </div>
          <div>
            <dt>Due</dt>
            <dd>{invoiceDraft?.dueDate ?? "-"}</dd>
          </div>
        </dl>

        <div className="billing-subform-heading payment-receipt-heading">
          <Receipt size={16} />
          <strong>Receipt</strong>
        </div>
        <div className="payment-form-grid receipt">
          <label className="form-field">
            <span>Method</span>
            <select
              value={paymentValue.method}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  method: event.target.value
                })
              }
              disabled={!canEditReceipt}
            >
              <option value="Card">Card</option>
              <option value="ManualCash">Manual cash</option>
              <option value="ManualAdjustment">Manual adjustment</option>
            </select>
          </label>
          <label className="form-field">
            <span>Reference</span>
            <input
              value={paymentValue.reference}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  reference: event.target.value.toUpperCase()
                })
              }
              disabled={!canEditReceipt}
            />
          </label>
          <label className="form-field">
            <span>Amount</span>
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={paymentValue.amount}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  amount: event.target.value
                })
              }
              disabled={!canEditReceipt}
            />
          </label>
          <label className="form-field">
            <span>Currency</span>
            <input
              value={paymentValue.currencyCode}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
              disabled={!canEditReceipt}
              maxLength={3}
            />
          </label>
          <label className="form-field">
            <span>Received</span>
            <input
              type="date"
              value={paymentValue.receivedOn}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  receivedOn: event.target.value
                })
              }
              disabled={!canEditReceipt}
            />
          </label>
          <label className="form-field">
            <span>Posting</span>
            <input
              type="date"
              value={paymentValue.postingDate}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  postingDate: event.target.value
                })
              }
              disabled={!canEditReceipt}
            />
          </label>
          <label className="form-field wide">
            <span>Cash/bank account ID</span>
            <input
              value={paymentValue.cashOrBankAccountId}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  cashOrBankAccountId: event.target.value
                })
              }
              disabled={!canEditReceipt}
            />
          </label>
          <label className="form-field wide">
            <span>AR account ID</span>
            <input
              value={paymentValue.accountsReceivableAccountId}
              onChange={(event) =>
                onPaymentChange({
                  ...paymentValue,
                  accountsReceivableAccountId: event.target.value
                })
              }
              disabled={!canEditReceipt}
            />
          </label>
        </div>

        <div className="billing-action-row">
          <button
            className="icon-button primary"
            type="submit"
            disabled={!canRecordPayment}
            title="Record payment"
          >
            <CheckCircle2 size={16} />
            Record
          </button>
          <span className="billing-small-fact">
            {invoiceDraft === null
              ? "No invoice draft"
              : issuedInvoice === null
                ? "Issue invoice first"
                : !hasReceivableAccount
                  ? "AR account required"
                  : !hasCashAccount
                    ? "Cash account required"
                    : `Balance ${formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}`}
          </span>
        </div>
      </form>

      <section
        className={`client-panel billing-light-panel payment-result-panel${
          activePaymentStep === "result" ? "" : " billing-step-hidden"
        }`}
      >
        <div className="client-panel-heading">
          <div>
            <span>Payments</span>
            <strong>Posting result</strong>
          </div>
          {recordedPayment !== null && (
            <span className={`status-pill ${statusClass(recordedPayment.invoiceStatus)}`}>
              {recordedPayment.invoiceStatus}
            </span>
          )}
        </div>

        {recordedPayment === null ? (
          <div className="client-empty-state">No payment recorded</div>
        ) : (
          <>
            <dl className="payment-result-facts">
              <div>
                <dt>Payment</dt>
                <dd>{recordedPayment.paymentStatus}</dd>
              </div>
              <div>
                <dt>Amount</dt>
                <dd>{formatMoney(recordedPayment.amount, recordedPayment.currencyCode)}</dd>
              </div>
              <div>
                <dt>Balance</dt>
                <dd>{formatMoney(recordedPayment.balanceDue, recordedPayment.currencyCode)}</dd>
              </div>
              <div>
                <dt>Journal</dt>
                <dd>{recordedPayment.journalEntryStatus}</dd>
              </div>
              <div>
                <dt>Debit</dt>
                <dd>{formatMoney(recordedPayment.totalDebit, recordedPayment.currencyCode)}</dd>
              </div>
              <div>
                <dt>Credit</dt>
                <dd>{formatMoney(recordedPayment.totalCredit, recordedPayment.currencyCode)}</dd>
              </div>
            </dl>

            <table className="billing-lines-table payment-journal-table">
              <thead>
                <tr>
                  <th>Description</th>
                  <th className="numeric">Debit</th>
                  <th className="numeric">Credit</th>
                </tr>
              </thead>
              <tbody>
                {recordedPayment.journalLines.map((line) => (
                  <tr key={line.ledgerAccountId}>
                    <td>{line.description ?? line.ledgerAccountId}</td>
                    <td className="numeric">{formatMoney(line.debit, recordedPayment.currencyCode)}</td>
                    <td className="numeric">{formatMoney(line.credit, recordedPayment.currencyCode)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        )}
      </section>

      {accountingProfile === null && (
        <div className="client-empty-state payment-warning">Accounting profile required before payment posting</div>
      )}
    </section>
  );
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

type PaymentStepInput = {
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
  accountingProfile: ClientAccountingProfile | null;
  hasCashAccount: boolean;
  hasReceivableAccount: boolean;
  recordedPayment: RecordedInvoicePayment | null;
  canRecordPayment: boolean;
};

function getPaymentStepItems({
  invoiceDraft,
  issuedInvoice,
  accountingProfile,
  hasCashAccount,
  hasReceivableAccount,
  recordedPayment,
  canRecordPayment
}: PaymentStepInput): PaymentStepItem[] {
  const balanceSummary = invoiceDraft === null
    ? "No invoice"
    : formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode);

  return [
    {
      step: "readiness",
      label: "Readiness",
      summary: canRecordPayment ? "Ready to post" : getReadinessSummary({
        invoiceDraft,
        issuedInvoice,
        accountingProfile,
        hasCashAccount,
        hasReceivableAccount
      }),
      tone: canRecordPayment ? "ready" : "warning",
      Icon: AlertCircle
    },
    {
      step: "cash",
      label: "Cash account",
      summary: hasCashAccount ? "Linked" : "Missing",
      tone: hasCashAccount ? "ready" : "warning",
      Icon: Banknote
    },
    {
      step: "receipt",
      label: "Receipt",
      summary: balanceSummary,
      tone: canRecordPayment ? "ready" : "neutral",
      Icon: Receipt
    },
    {
      step: "result",
      label: "Posting result",
      summary: recordedPayment === null
        ? "Pending"
        : `${recordedPayment.invoiceStatus} ${formatMoney(
            recordedPayment.balanceDue,
            recordedPayment.currencyCode
          )}`,
      tone: recordedPayment === null ? "neutral" : "ready",
      Icon: CheckCircle2
    }
  ];
}

function getReadinessSummary({
  invoiceDraft,
  issuedInvoice,
  accountingProfile,
  hasCashAccount,
  hasReceivableAccount
}: Pick<
  PaymentStepInput,
  "invoiceDraft" | "issuedInvoice" | "accountingProfile" | "hasCashAccount" | "hasReceivableAccount"
>): string {
  if (invoiceDraft === null) {
    return "No invoice";
  }

  if (issuedInvoice === null) {
    return "Issue invoice";
  }

  if (invoiceDraft.balanceDue <= 0) {
    return "Paid";
  }

  if (accountingProfile === null || !hasReceivableAccount) {
    return "AR missing";
  }

  if (!hasCashAccount) {
    return "Cash missing";
  }

  return "Review";
}

function statusClass(value: string): string {
  return value.toLowerCase().replaceAll(" ", "");
}

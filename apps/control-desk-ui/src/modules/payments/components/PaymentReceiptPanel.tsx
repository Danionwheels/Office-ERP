import { Banknote, CheckCircle2, PlusCircle, Receipt } from "lucide-react";
import type { FormEvent } from "react";
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
  async function handleCreateCashAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreateCashAccount();
  }

  async function handleRecordPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onRecordPayment();
  }

  const canRecordPayment =
    issuedInvoice !== null
    && invoiceDraft !== null
    && invoiceDraft.balanceDue > 0
    && !isBusy;

  return (
    <section className="client-panel payment-receipt-panel">
      <div className="client-panel-heading">
        <div>
          <span>Payments</span>
          <strong>Receipt posting</strong>
        </div>
        {recordedPayment !== null && (
          <span className={`status-pill ${recordedPayment.invoiceStatus.toLowerCase()}`}>
            {recordedPayment.invoiceStatus}
          </span>
        )}
      </div>

      <form className="billing-subform first" onSubmit={handleCreateCashAccount}>
        <div className="billing-subform-heading">
          <Banknote size={16} />
          <strong>Cash or bank account</strong>
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
        </div>
      </form>

      <form className="billing-subform" onSubmit={handleRecordPayment}>
        <div className="billing-subform-heading">
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
              disabled={!canRecordPayment}
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
              disabled={!canRecordPayment}
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
              disabled={!canRecordPayment}
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
              disabled={!canRecordPayment}
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
              disabled={!canRecordPayment}
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
              disabled={!canRecordPayment}
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
              disabled={!canRecordPayment}
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
              disabled={!canRecordPayment}
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
                : `Balance ${formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}`}
          </span>
        </div>
      </form>

      {recordedPayment !== null && (
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
      )}

      {accountingProfile === null && (
        <div className="client-empty-state payment-warning">Accounting profile required before payment posting</div>
      )}
    </section>
  );
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

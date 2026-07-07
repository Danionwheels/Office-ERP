import {
  AlertCircle,
  ArrowRightLeft,
  Banknote,
  CheckCircle2,
  Landmark,
  ListTree,
  PlusCircle,
  Receipt
} from "lucide-react";
import { type FormEvent, useEffect, useState } from "react";
import {
  PaymentFlowRegister,
  PaymentJournalRegister,
  PaymentPostingBridge,
  PaymentPostingResult
} from "./shared/PaymentWorkflow";
import type {
  PaymentReceiptPanelProps,
  PaymentStep
} from "../types/paymentWorkflowTypes";
import {
  formatMoney,
  getPaymentStepItems,
  getRefundCredit,
  getSettlementCredit,
  statusClass,
  toDateInputValue
} from "../utils/paymentWorkflowModel";

export function PaymentReceiptPanel({
  invoiceDraft,
  issuedInvoice,
  initialStep = "readiness",
  accountingProfile,
  cashAccountValue,
  paymentValue,
  refundValue,
  creditApplicationValue,
  recordedPayment,
  issuedRefund,
  appliedCredit,
  clientStatement,
  isBusy,
  onCashAccountChange,
  onPaymentChange,
  onRefundChange,
  onCreditApplicationChange,
  onCreateCashAccount,
  onRecordPayment,
  onIssueRefund,
  onApplyCredit,
  onApprovePayment,
  onRejectPayment,
  onReversePayment,
  onViewJournalEntry
}: PaymentReceiptPanelProps) {
  const [activePaymentStep, setActivePaymentStep] = useState<PaymentStep>(initialStep);
  const [decisionNote, setDecisionNote] = useState("");
  const [reversalDate, setReversalDate] = useState(() => toDateInputValue(new Date()));

  useEffect(() => {
    setActivePaymentStep(initialStep);
  }, [initialStep]);

  async function handleCreateCashAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreateCashAccount();
  }

  async function handleRecordPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onRecordPayment();
  }

  async function handleIssueRefund(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onIssueRefund();
  }

  async function handleApplyCredit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onApplyCredit();
  }

  async function handleApprovePayment() {
    await onApprovePayment(decisionNote);
  }

  async function handleRejectPayment() {
    await onRejectPayment(decisionNote);
  }

  async function handleReversePayment() {
    await onReversePayment(decisionNote, reversalDate);
  }

  const hasCashAccount = paymentValue.cashOrBankAccountId.trim() !== "";
  const hasReceivableAccount = paymentValue.accountsReceivableAccountId.trim() !== "";
  const hasRefundCashAccount = refundValue.cashOrBankAccountId.trim() !== "";
  const hasRefundReceivableAccount = refundValue.accountsReceivableAccountId.trim() !== "";
  const canCreateCashAccount =
    cashAccountValue.code.trim() !== ""
    && cashAccountValue.name.trim() !== "";
  const refundCredit = getRefundCredit(clientStatement, refundValue.currencyCode);
  const settlementCredit = getSettlementCredit(clientStatement, creditApplicationValue.currencyCode);
  const paymentAmount = Number(paymentValue.amount);
  const refundAmount = Number(refundValue.amount);
  const creditApplicationAmount = Number(creditApplicationValue.amount);
  const invoiceBalance = invoiceDraft?.balanceDue ?? 0;
  const hasPendingReviewPayment = recordedPayment?.paymentStatus === "PendingReview";
  const hasApprovedPayment = recordedPayment?.paymentStatus === "Approved";
  const canEditReceipt =
    issuedInvoice !== null
    && invoiceDraft !== null
    && invoiceDraft.balanceDue > 0
    && !isBusy
    && !hasPendingReviewPayment;
  const canRecordPayment =
    canEditReceipt
    && hasCashAccount
    && hasReceivableAccount
    && paymentValue.invoiceId.trim() !== ""
    && paymentValue.method.trim() !== ""
    && paymentValue.reference.trim() !== ""
    && paymentAmount > 0
    && paymentAmount <= invoiceBalance
    && paymentValue.currencyCode.trim().length === 3
    && paymentValue.receivedOn !== ""
    && paymentValue.postingDate !== "";
  const canIssueRefund =
    refundCredit.availableCredit > 0
    && refundValue.clientId.trim() !== ""
    && refundValue.method.trim() !== ""
    && refundValue.reference.trim() !== ""
    && refundAmount > 0
    && refundAmount <= refundCredit.availableCredit
    && refundValue.currencyCode.trim().length === 3
    && refundValue.refundedOn !== ""
    && refundValue.postingDate !== ""
    && hasRefundCashAccount
    && hasRefundReceivableAccount
    && !isBusy;
  const canApplyCredit =
    invoiceDraft !== null
    && ["Issued", "PartiallyPaid"].includes(invoiceDraft.status)
    && creditApplicationValue.clientId.trim() !== ""
    && creditApplicationValue.invoiceId.trim() !== ""
    && creditApplicationValue.reference.trim() !== ""
    && invoiceBalance > 0
    && settlementCredit.availableCredit > 0
    && creditApplicationAmount > 0
    && creditApplicationAmount <= Math.min(settlementCredit.availableCredit, invoiceBalance)
    && creditApplicationValue.currencyCode.trim().length === 3
    && creditApplicationValue.appliedOn !== ""
    && !isBusy;
  const canApprovePayment =
    hasPendingReviewPayment
    && hasCashAccount
    && hasReceivableAccount
    && paymentValue.postingDate !== ""
    && !isBusy;
  const canRejectPayment =
    hasPendingReviewPayment
    && decisionNote.trim() !== ""
    && !isBusy;
  const canReversePayment =
    hasApprovedPayment
    && decisionNote.trim() !== ""
    && reversalDate !== ""
    && !isBusy;
  const paymentSteps = getPaymentStepItems({
    invoiceDraft,
    issuedInvoice,
    accountingProfile,
    hasCashAccount,
    hasReceivableAccount,
    recordedPayment,
    issuedRefund,
    appliedCredit,
    refundCredit,
    settlementCredit,
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
        <div className={`billing-step-current ${activePaymentStepItem.tone}`}>
          <span>Current status</span>
          <strong>{activePaymentStepItem.summary}</strong>
        </div>
      </header>

      <PaymentFlowRegister
        activeStep={activePaymentStep}
        onSelectStep={setActivePaymentStep}
        steps={paymentSteps}
      />

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
          <div>
            <dt>Unapplied</dt>
            <dd>{formatMoney(settlementCredit.availableCredit, settlementCredit.currencyCode)}</dd>
          </div>
        </dl>

        <PaymentPostingBridge
          accountsReceivableAccountId={paymentValue.accountsReceivableAccountId}
          amount={paymentAmount}
          cashOrBankAccountId={paymentValue.cashOrBankAccountId}
          currencyCode={paymentValue.currencyCode}
          documentDetail={
            invoiceDraft === null
              ? "No issued invoice"
              : `${invoiceDraft.status} / ${formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}`
          }
          documentLabel={invoiceDraft?.invoiceNumber ?? "No invoice"}
          journalEntryId={recordedPayment?.journalEntryId ?? null}
          journalStatus={recordedPayment?.journalEntryStatus ?? null}
          postingDate={paymentValue.postingDate}
          postingVerb="Receipt"
        />

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
          <button
            className="icon-button"
            type="button"
            onClick={() => setActivePaymentStep("settlement")}
            disabled={settlementCredit.availableCredit <= 0 || invoiceDraft === null}
            title="Open credit settlement"
          >
            <ArrowRightLeft size={16} />
            Settle
          </button>
          <button
            className="icon-button"
            type="button"
            onClick={() => setActivePaymentStep("refund")}
            disabled={refundCredit.availableCredit <= 0}
            title="Open client refund"
          >
            <Landmark size={16} />
            Refund
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
          <button
            className="icon-button"
            type="submit"
            disabled={isBusy || !canCreateCashAccount}
            title="Create cash or bank account"
          >
            <PlusCircle size={16} />
            Create
          </button>
          {hasCashAccount && (
            <span className="billing-small-fact">{paymentValue.cashOrBankAccountId}</span>
          )}
        </div>
      </form>

      <form
        className={`client-panel billing-light-panel payment-settlement-panel${
          activePaymentStep === "settlement" ? "" : " billing-step-hidden"
        }`}
        onSubmit={handleApplyCredit}
      >
        <div className="client-panel-heading">
          <div>
            <span>Payments</span>
            <strong>Credit settlement</strong>
          </div>
          <span className={`status-pill ${canApplyCredit ? "active" : "draft"}`}>
            {canApplyCredit ? "Ready" : "Pending"}
          </span>
        </div>

        <dl className="payment-result-facts payment-invoice-facts">
          <div>
            <dt>Invoice</dt>
            <dd>{invoiceDraft?.invoiceNumber ?? "No invoice"}</dd>
          </div>
          <div>
            <dt>Balance</dt>
            <dd>
              {invoiceDraft === null
                ? formatMoney(0, settlementCredit.currencyCode)
                : formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}
            </dd>
          </div>
          <div>
            <dt>Unapplied</dt>
            <dd>{formatMoney(settlementCredit.availableCredit, settlementCredit.currencyCode)}</dd>
          </div>
          <div>
            <dt>After</dt>
            <dd>
              {invoiceDraft === null
                ? formatMoney(0, settlementCredit.currencyCode)
                : formatMoney(
                    Math.max(invoiceDraft.balanceDue - Math.max(creditApplicationAmount, 0), 0),
                    invoiceDraft.currencyCode
                  )}
            </dd>
          </div>
        </dl>

        <div className="billing-subform-heading payment-receipt-heading">
          <ArrowRightLeft size={16} />
          <strong>Apply credit</strong>
        </div>
        <div className="payment-form-grid receipt">
          <label className="form-field wide">
            <span>Invoice ID</span>
            <input
              value={creditApplicationValue.invoiceId}
              onChange={(event) =>
                onCreditApplicationChange({
                  ...creditApplicationValue,
                  invoiceId: event.target.value
                })
              }
              disabled={isBusy || settlementCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field">
            <span>Reference</span>
            <input
              value={creditApplicationValue.reference}
              onChange={(event) =>
                onCreditApplicationChange({
                  ...creditApplicationValue,
                  reference: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy || settlementCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field">
            <span>Amount</span>
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={creditApplicationValue.amount}
              onChange={(event) =>
                onCreditApplicationChange({
                  ...creditApplicationValue,
                  amount: event.target.value
                })
              }
              disabled={isBusy || settlementCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field">
            <span>Currency</span>
            <input
              value={creditApplicationValue.currencyCode}
              onChange={(event) =>
                onCreditApplicationChange({
                  ...creditApplicationValue,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy || settlementCredit.availableCredit <= 0}
              maxLength={3}
            />
          </label>
          <label className="form-field">
            <span>Applied</span>
            <input
              type="date"
              value={creditApplicationValue.appliedOn}
              onChange={(event) =>
                onCreditApplicationChange({
                  ...creditApplicationValue,
                  appliedOn: event.target.value
                })
              }
              disabled={isBusy || settlementCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field wide">
            <span>Note</span>
            <input
              value={creditApplicationValue.note}
              onChange={(event) =>
                onCreditApplicationChange({
                  ...creditApplicationValue,
                  note: event.target.value
                })
              }
              disabled={isBusy || settlementCredit.availableCredit <= 0}
            />
          </label>
        </div>

        <div className="billing-action-row">
          <button
            className="icon-button primary"
            type="submit"
            disabled={!canApplyCredit}
            title="Apply client credit"
          >
            <ArrowRightLeft size={16} />
            Apply
          </button>
          <span className="billing-small-fact">
            {settlementCredit.availableCredit <= 0
              ? "No unapplied credit"
              : invoiceDraft === null
                ? "Select invoice"
                : invoiceDraft.balanceDue <= 0
                  ? "Invoice paid"
                  : `Can apply ${formatMoney(
                      Math.min(settlementCredit.availableCredit, invoiceDraft.balanceDue),
                      invoiceDraft.currencyCode
                    )}`}
          </span>
        </div>

        {appliedCredit !== null && (
          <dl className="payment-result-facts">
            <div>
              <dt>Applied</dt>
              <dd>{appliedCredit.creditApplicationStatus}</dd>
            </div>
            <div>
              <dt>Amount</dt>
              <dd>{formatMoney(appliedCredit.amount, appliedCredit.currencyCode)}</dd>
            </div>
            <div>
              <dt>Invoice</dt>
              <dd>{appliedCredit.invoiceStatus}</dd>
            </div>
            <div>
              <dt>Balance</dt>
              <dd>{formatMoney(appliedCredit.invoiceBalanceAfter, appliedCredit.currencyCode)}</dd>
            </div>
            <div>
              <dt>Credit left</dt>
              <dd>{formatMoney(appliedCredit.availableCreditAfter, appliedCredit.currencyCode)}</dd>
            </div>
          </dl>
        )}
      </form>

      <form
        className={`client-panel billing-light-panel payment-refund-panel${
          activePaymentStep === "refund" ? "" : " billing-step-hidden"
        }`}
        onSubmit={handleIssueRefund}
      >
        <div className="client-panel-heading">
          <div>
            <span>Payments</span>
            <strong>Client refund</strong>
          </div>
          <span className={`status-pill ${refundCredit.availableCredit > 0 ? "active" : "draft"}`}>
            {refundCredit.availableCredit > 0 ? "Credit" : "No credit"}
          </span>
        </div>

        <dl className="payment-result-facts payment-invoice-facts">
          <div>
            <dt>Available</dt>
            <dd>{formatMoney(refundCredit.availableCredit, refundCredit.currencyCode)}</dd>
          </div>
          <div>
            <dt>Balance</dt>
            <dd>{formatMoney(refundCredit.balanceDue, refundCredit.currencyCode)}</dd>
          </div>
          <div>
            <dt>AR account</dt>
            <dd>{hasRefundReceivableAccount ? "Linked" : "Missing"}</dd>
          </div>
          <div>
            <dt>Cash/bank</dt>
            <dd>{hasRefundCashAccount ? "Linked" : "Missing"}</dd>
          </div>
        </dl>

        <PaymentPostingBridge
          accountsReceivableAccountId={refundValue.accountsReceivableAccountId}
          amount={refundAmount}
          cashOrBankAccountId={refundValue.cashOrBankAccountId}
          currencyCode={refundValue.currencyCode}
          documentDetail={
            refundCredit.availableCredit <= 0
              ? "No client credit"
              : `Available ${formatMoney(refundCredit.availableCredit, refundCredit.currencyCode)}`
          }
          documentLabel={refundValue.reference.trim() === "" ? "Refund" : refundValue.reference}
          journalEntryId={issuedRefund?.journalEntryId ?? null}
          journalStatus={issuedRefund?.journalEntryStatus ?? null}
          postingDate={refundValue.postingDate}
          postingVerb="Refund"
        />

        <div className="billing-subform-heading payment-receipt-heading">
          <Landmark size={16} />
          <strong>Refund</strong>
        </div>
        <div className="payment-form-grid receipt">
          <label className="form-field">
            <span>Method</span>
            <select
              value={refundValue.method}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  method: event.target.value
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            >
              <option value="BankTransfer">Bank transfer</option>
              <option value="ManualCash">Manual cash</option>
              <option value="ManualAdjustment">Manual adjustment</option>
            </select>
          </label>
          <label className="form-field">
            <span>Reference</span>
            <input
              value={refundValue.reference}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  reference: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field">
            <span>Amount</span>
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={refundValue.amount}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  amount: event.target.value
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field">
            <span>Currency</span>
            <input
              value={refundValue.currencyCode}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
              maxLength={3}
            />
          </label>
          <label className="form-field">
            <span>Refunded</span>
            <input
              type="date"
              value={refundValue.refundedOn}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  refundedOn: event.target.value
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field">
            <span>Posting</span>
            <input
              type="date"
              value={refundValue.postingDate}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  postingDate: event.target.value
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field wide">
            <span>Cash/bank account ID</span>
            <input
              value={refundValue.cashOrBankAccountId}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  cashOrBankAccountId: event.target.value
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field wide">
            <span>AR account ID</span>
            <input
              value={refundValue.accountsReceivableAccountId}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  accountsReceivableAccountId: event.target.value
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            />
          </label>
          <label className="form-field wide">
            <span>Note</span>
            <input
              value={refundValue.note}
              onChange={(event) =>
                onRefundChange({
                  ...refundValue,
                  note: event.target.value
                })
              }
              disabled={isBusy || refundCredit.availableCredit <= 0}
            />
          </label>
        </div>

        <div className="billing-action-row">
          <button
            className="icon-button primary"
            type="submit"
            disabled={!canIssueRefund}
            title="Issue refund"
          >
            <Landmark size={16} />
            Refund
          </button>
          <span className="billing-small-fact">
            {refundCredit.availableCredit <= 0
              ? "No client credit"
              : !hasRefundReceivableAccount
                ? "AR account required"
                : !hasRefundCashAccount
                  ? "Cash account required"
                  : `Available ${formatMoney(refundCredit.availableCredit, refundCredit.currencyCode)}`}
          </span>
        </div>

        {issuedRefund !== null && (
          <PaymentPostingResult
            actionLabel="Refund"
            actionStatus={issuedRefund.refundStatus}
            amount={issuedRefund.amount}
            balanceLabel="Balance"
            balanceValue={issuedRefund.clientBalanceAfter}
            currencyCode={issuedRefund.currencyCode}
            journalEntryId={issuedRefund.journalEntryId}
            journalEntryStatus={issuedRefund.journalEntryStatus}
            journalLines={issuedRefund.journalLines}
            onViewJournalEntry={onViewJournalEntry}
            totalCredit={issuedRefund.totalCredit}
            totalDebit={issuedRefund.totalDebit}
            isBusy={isBusy}
            tableLabel="Refund journal lines"
          />
        )}
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

        <PaymentPostingBridge
          accountsReceivableAccountId={paymentValue.accountsReceivableAccountId}
          amount={paymentAmount}
          cashOrBankAccountId={paymentValue.cashOrBankAccountId}
          currencyCode={paymentValue.currencyCode}
          documentDetail={
            invoiceDraft === null
              ? "No issued invoice"
              : `${invoiceDraft.status} / ${formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}`
          }
          documentLabel={invoiceDraft?.invoiceNumber ?? "No invoice"}
          journalEntryId={recordedPayment?.journalEntryId ?? null}
          journalStatus={recordedPayment?.journalEntryStatus ?? null}
          postingDate={paymentValue.postingDate}
          postingVerb="Receipt"
        />

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
              <option value="BankTransfer">Bank transfer</option>
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
                : hasPendingReviewPayment
                  ? "Review pending"
                  : paymentAmount <= 0
                    ? "Amount required"
                    : paymentAmount > invoiceDraft.balanceDue
                      ? "Amount exceeds balance"
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
                <dd className="fact-action-value">
                  <span>{recordedPayment.journalEntryStatus ?? "Not posted"}</span>
                  {recordedPayment.journalEntryId !== null && recordedPayment.journalEntryId !== undefined && (
                    <button
                      className="table-icon-button"
                      type="button"
                      onClick={() => void onViewJournalEntry(recordedPayment.journalEntryId ?? "")}
                      disabled={isBusy}
                      title="Open related journal"
                    >
                      <ListTree size={14} />
                    </button>
                  )}
                </dd>
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

            <PaymentJournalRegister
              currencyCode={recordedPayment.currencyCode}
              emptyText="Payment is waiting for review before GL posting"
              lines={recordedPayment.journalLines}
              title="Payment journal lines"
            />

            {(recordedPayment.paymentStatus === "PendingReview" || recordedPayment.paymentStatus === "Approved") && (
              <div className="payment-review-actions">
                <label className="form-field wide">
                  <span>Decision note</span>
                  <input
                    value={decisionNote}
                    onChange={(event) => setDecisionNote(event.target.value)}
                    disabled={isBusy}
                  />
                </label>
                {recordedPayment.paymentStatus === "Approved" && (
                  <label className="form-field">
                    <span>Reversal date</span>
                    <input
                      type="date"
                      value={reversalDate}
                      onChange={(event) => setReversalDate(event.target.value)}
                      disabled={isBusy}
                    />
                  </label>
                )}
                <div className="billing-action-row">
                  {recordedPayment.paymentStatus === "PendingReview" && (
                    <>
                      <button
                        className="icon-button primary"
                        type="button"
                        onClick={handleApprovePayment}
                        disabled={!canApprovePayment}
                        title="Approve payment"
                      >
                        <CheckCircle2 size={16} />
                        Approve
                      </button>
                      <button
                        className="icon-button"
                        type="button"
                        onClick={handleRejectPayment}
                        disabled={!canRejectPayment}
                        title="Reject payment"
                      >
                        <AlertCircle size={16} />
                        Reject
                      </button>
                    </>
                  )}
                  {recordedPayment.paymentStatus === "Approved" && (
                    <button
                      className="icon-button"
                      type="button"
                      onClick={handleReversePayment}
                      disabled={!canReversePayment}
                      title="Reverse payment"
                    >
                      <AlertCircle size={16} />
                      Reverse
                    </button>
                  )}
                </div>
              </div>
            )}
          </>
        )}
      </section>

      {accountingProfile === null && (
        <div className="client-empty-state payment-warning">Accounting profile required before payment posting</div>
      )}
    </section>
  );
}

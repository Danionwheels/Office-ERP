import {
  Banknote,
  CircleDollarSign,
  FileCheck2,
  FilePlus2,
  PlusCircle,
  ReceiptText,
  RefreshCw,
  Save,
  type LucideIcon
} from "lucide-react";
import { type FormEvent, useState } from "react";
import type { ClientContract } from "../../contracts/types/contractTypes";
import type {
  ClientAccountingProfile,
  ClientDetails,
  ConfigureClientAccountingProfileInput
} from "../../clients/types/clientTypes";
import type {
  ChargeCodeFormInput,
  ChargeCodeLookup,
  ClientChargeRule,
  ClientChargeRuleFormInput,
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccountFormInput
} from "../types/billingTypes";

type ClientBillingSetupPanelProps = {
  client: ClientDetails | null;
  contracts: ClientContract[];
  accountingProfile: ClientAccountingProfile | null;
  accountingProfileMissing: boolean;
  chargeCodes: ChargeCodeLookup[];
  receivableAccountValue: LedgerAccountFormInput;
  revenueAccountValue: LedgerAccountFormInput;
  accountingProfileValue: ConfigureClientAccountingProfileInput;
  chargeCodeValue: ChargeCodeFormInput;
  chargeRuleValue: ClientChargeRuleFormInput;
  invoiceDraftValue: InvoiceDraftFormInput;
  issueInvoiceValue: IssueInvoiceFormInput;
  latestChargeRule: ClientChargeRule | null;
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
  isBusy: boolean;
  onReceivableAccountChange: (value: LedgerAccountFormInput) => void;
  onRevenueAccountChange: (value: LedgerAccountFormInput) => void;
  onAccountingProfileChange: (value: ConfigureClientAccountingProfileInput) => void;
  onChargeCodeChange: (value: ChargeCodeFormInput) => void;
  onChargeRuleChange: (value: ClientChargeRuleFormInput) => void;
  onInvoiceDraftChange: (value: InvoiceDraftFormInput) => void;
  onIssueInvoiceChange: (value: IssueInvoiceFormInput) => void;
  onCreateReceivableAccount: () => Promise<void>;
  onCreateRevenueAccount: () => Promise<void>;
  onSaveAccountingProfile: () => Promise<void>;
  onCreateChargeCode: () => Promise<void>;
  onRefreshChargeCodes: () => Promise<void>;
  onCreateChargeRule: () => Promise<void>;
  onGenerateInvoiceDraft: () => Promise<void>;
  onIssueInvoice: () => Promise<void>;
};

type BillingStep = "accounting" | "rules" | "draft" | "issue";

type BillingStepItem = {
  step: BillingStep;
  label: string;
  summary: string;
  tone: "neutral" | "ready" | "warning";
  Icon: LucideIcon;
};

export function ClientBillingSetupPanel({
  client,
  contracts,
  accountingProfile,
  accountingProfileMissing,
  chargeCodes,
  receivableAccountValue,
  revenueAccountValue,
  accountingProfileValue,
  chargeCodeValue,
  chargeRuleValue,
  invoiceDraftValue,
  issueInvoiceValue,
  latestChargeRule,
  invoiceDraft,
  issuedInvoice,
  isBusy,
  onReceivableAccountChange,
  onRevenueAccountChange,
  onAccountingProfileChange,
  onChargeCodeChange,
  onChargeRuleChange,
  onInvoiceDraftChange,
  onIssueInvoiceChange,
  onCreateReceivableAccount,
  onCreateRevenueAccount,
  onSaveAccountingProfile,
  onCreateChargeCode,
  onRefreshChargeCodes,
  onCreateChargeRule,
  onGenerateInvoiceDraft,
  onIssueInvoice
}: ClientBillingSetupPanelProps) {
  const [activeBillingStep, setActiveBillingStep] = useState<BillingStep>("accounting");

  async function handleCreateReceivableAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreateReceivableAccount();
  }

  async function handleCreateRevenueAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreateRevenueAccount();
  }

  async function handleSaveAccountingProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSaveAccountingProfile();
  }

  async function handleCreateChargeCode(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreateChargeCode();
  }

  async function handleCreateChargeRule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreateChargeRule();
  }

  async function handleGenerateInvoiceDraft(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onGenerateInvoiceDraft();
  }

  async function handleIssueInvoice(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onIssueInvoice();
  }

  const billingSteps = getBillingStepItems({
    accountingProfile,
    accountingProfileMissing,
    chargeCodes,
    latestChargeRule,
    invoiceDraft,
    issuedInvoice
  });
  const activeBillingStepItem =
    billingSteps.find((step) => step.step === activeBillingStep) ?? billingSteps[0];

  if (client === null) {
    return (
      <section className="client-panel client-billing-empty">
        <div className="client-empty-detail">Select a client</div>
      </section>
    );
  }

  return (
    <section className="client-billing-zone billing-workspace billing-step-workspace">
      <header className="billing-step-header">
        <div>
          <span>Billing setup</span>
          <h2>{activeBillingStepItem.label}</h2>
        </div>
        <div className="billing-step-summary-grid">
          {billingSteps.map((step) => (
            <button
              className={`billing-step-summary ${step.tone}${
                activeBillingStep === step.step ? " active" : ""
              }`}
              key={step.step}
              type="button"
              onClick={() => setActiveBillingStep(step.step)}
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

      <div
        className={`client-panel billing-accounting-panel billing-light-panel${
          activeBillingStep === "accounting" ? "" : " billing-step-hidden"
        }`}
      >
        <div className="client-panel-heading">
          <div>
            <span>Billing setup</span>
            <strong>Accounting profile</strong>
          </div>
          <span className={`status-pill ${accountingProfileMissing ? "draft" : "active"}`}>
            {accountingProfileMissing ? "Missing" : "Linked"}
          </span>
        </div>

        <form className="billing-subform" onSubmit={handleCreateReceivableAccount}>
          <div className="billing-subform-heading">
            <Banknote size={16} />
            <strong>AR account</strong>
          </div>
          <div className="billing-form-grid two">
            <label className="form-field">
              <span>Code</span>
              <input
                value={receivableAccountValue.code}
                onChange={(event) =>
                  onReceivableAccountChange({
                    ...receivableAccountValue,
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
                value={receivableAccountValue.name}
                onChange={(event) =>
                  onReceivableAccountChange({
                    ...receivableAccountValue,
                    name: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
          </div>
          <div className="billing-action-row">
            <button className="icon-button" type="submit" disabled={isBusy} title="Create AR account">
              <PlusCircle size={16} />
              Create
            </button>
          </div>
        </form>

        <form className="billing-subform" onSubmit={handleSaveAccountingProfile}>
          <div className="billing-subform-heading">
            <Save size={16} />
            <strong>Profile</strong>
          </div>
          <div className="billing-form-grid three">
            <label className="form-field wide">
              <span>AR account ID</span>
              <input
                value={accountingProfileValue.accountsReceivableAccountId}
                onChange={(event) =>
                  onAccountingProfileChange({
                    ...accountingProfileValue,
                    accountsReceivableAccountId: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Currency</span>
              <input
                value={accountingProfileValue.defaultCurrencyCode}
                onChange={(event) =>
                  onAccountingProfileChange({
                    ...accountingProfileValue,
                    defaultCurrencyCode: event.target.value.toUpperCase()
                  })
                }
                disabled={isBusy}
                maxLength={3}
              />
            </label>
            <label className="form-field">
              <span>Cloud customer</span>
              <input
                value={accountingProfileValue.cloudCustomerId}
                onChange={(event) =>
                  onAccountingProfileChange({
                    ...accountingProfileValue,
                    cloudCustomerId: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
          </div>
          <div className="billing-action-row">
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Save accounting profile">
              <Save size={16} />
              Save
            </button>
            {accountingProfile !== null && (
              <span className="billing-small-fact">
                Updated {formatDateTime(accountingProfile.updatedAtUtc)}
              </span>
            )}
          </div>
        </form>
      </div>

      <div
        className={`client-panel billing-charge-panel billing-light-panel${
          activeBillingStep === "rules" ? "" : " billing-step-hidden"
        }`}
      >
        <div className="client-panel-heading">
          <div>
            <span>Billing setup</span>
            <strong>Charge code</strong>
          </div>
          <button
            className="mini-button"
            type="button"
            onClick={onRefreshChargeCodes}
            disabled={isBusy}
            title="Refresh charge codes"
          >
            <RefreshCw size={14} />
            Refresh
          </button>
        </div>

        <form className="billing-subform" onSubmit={handleCreateRevenueAccount}>
          <div className="billing-subform-heading">
            <CircleDollarSign size={16} />
            <strong>Revenue account</strong>
          </div>
          <div className="billing-form-grid two">
            <label className="form-field">
              <span>Code</span>
              <input
                value={revenueAccountValue.code}
                onChange={(event) =>
                  onRevenueAccountChange({
                    ...revenueAccountValue,
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
                value={revenueAccountValue.name}
                onChange={(event) =>
                  onRevenueAccountChange({
                    ...revenueAccountValue,
                    name: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
          </div>
          <div className="billing-action-row">
            <button className="icon-button" type="submit" disabled={isBusy} title="Create revenue account">
              <PlusCircle size={16} />
              Create
            </button>
          </div>
        </form>

        <form className="billing-subform" onSubmit={handleCreateChargeCode}>
          <div className="billing-subform-heading">
            <ReceiptText size={16} />
            <strong>Charge code</strong>
          </div>
          <div className="billing-form-grid charge-code">
            <label className="form-field">
              <span>Code</span>
              <input
                value={chargeCodeValue.code}
                onChange={(event) =>
                  onChargeCodeChange({
                    ...chargeCodeValue,
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
                value={chargeCodeValue.name}
                onChange={(event) =>
                  onChargeCodeChange({
                    ...chargeCodeValue,
                    name: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Price</span>
              <input
                type="number"
                min="0"
                step="0.01"
                value={chargeCodeValue.defaultUnitPriceAmount}
                onChange={(event) =>
                  onChargeCodeChange({
                    ...chargeCodeValue,
                    defaultUnitPriceAmount: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Currency</span>
              <input
                value={chargeCodeValue.currencyCode}
                onChange={(event) =>
                  onChargeCodeChange({
                    ...chargeCodeValue,
                    currencyCode: event.target.value.toUpperCase()
                  })
                }
                disabled={isBusy}
                maxLength={3}
              />
            </label>
            <label className="form-field wide">
              <span>Revenue account ID</span>
              <input
                value={chargeCodeValue.revenueAccountId}
                onChange={(event) =>
                  onChargeCodeChange({
                    ...chargeCodeValue,
                    revenueAccountId: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field wide">
              <span>Description</span>
              <input
                value={chargeCodeValue.description}
                onChange={(event) =>
                  onChargeCodeChange({
                    ...chargeCodeValue,
                    description: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
          </div>
          <div className="billing-action-row">
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Create charge code">
              <FilePlus2 size={16} />
              Create
            </button>
            <span className="billing-small-fact">{chargeCodes.length} charge codes</span>
          </div>
        </form>
      </div>

      <form
        className={`client-panel billing-rule-form billing-light-panel${
          activeBillingStep === "rules" ? "" : " billing-step-hidden"
        }`}
        onSubmit={handleCreateChargeRule}
      >
        <div className="client-panel-heading">
          <div>
            <span>Billing setup</span>
            <strong>Charge rule</strong>
          </div>
          <button className="icon-button primary" type="submit" disabled={isBusy} title="Add charge rule">
            <PlusCircle size={16} />
            Add
          </button>
        </div>

        <div className="billing-form-grid rule">
          <label className="form-field wide">
            <span>Contract</span>
            <select
              value={chargeRuleValue.contractId}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  contractId: event.target.value
                })
              }
              disabled={isBusy}
            >
              <option value="">Select contract</option>
              {contracts.map((contract) => (
                <option value={contract.contractId} key={contract.contractId}>
                  {contract.contractNumber} ({contract.status})
                </option>
              ))}
            </select>
          </label>
          <label className="form-field wide">
            <span>Charge code</span>
            <select
              value={chargeRuleValue.chargeCodeId}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  chargeCodeId: event.target.value,
                  ...chargeRulePatchForChargeCode(event.target.value, chargeCodes)
                })
              }
              disabled={isBusy}
            >
              <option value="">Select charge code</option>
              {chargeCodes.map((chargeCode) => (
                <option value={chargeCode.chargeCodeId} key={chargeCode.chargeCodeId}>
                  {chargeCode.code} - {chargeCode.name}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field">
            <span>Unit price</span>
            <input
              type="number"
              min="0"
              step="0.01"
              value={chargeRuleValue.unitPriceAmount}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  unitPriceAmount: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Quantity</span>
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={chargeRuleValue.quantity}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  quantity: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Currency</span>
            <input
              value={chargeRuleValue.currencyCode}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy}
              maxLength={3}
            />
          </label>
          <label className="form-field">
            <span>Cycle</span>
            <select
              value={chargeRuleValue.billingCycle}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  billingCycle: event.target.value
                })
              }
              disabled={isBusy}
            >
              <option value="Monthly">Monthly</option>
              <option value="Quarterly">Quarterly</option>
              <option value="SemiAnnual">SemiAnnual</option>
              <option value="Annual">Annual</option>
            </select>
          </label>
          <label className="form-field">
            <span>Bill day</span>
            <input
              type="number"
              min="1"
              max="28"
              value={chargeRuleValue.billingDayOfMonth}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  billingDayOfMonth: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Start</span>
            <input
              type="date"
              value={chargeRuleValue.effectiveStartsOn}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  effectiveStartsOn: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>End</span>
            <input
              type="date"
              value={chargeRuleValue.effectiveEndsOn}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  effectiveEndsOn: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field wide">
            <span>Description</span>
            <input
              value={chargeRuleValue.descriptionOverride}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  descriptionOverride: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
        </div>

        {latestChargeRule !== null && (
          <dl className="billing-result-facts">
            <div>
              <dt>Latest rule</dt>
              <dd>{latestChargeRule.status}</dd>
            </div>
            <div>
              <dt>Line amount</dt>
              <dd>{formatMoney(latestChargeRule.lineAmount, latestChargeRule.currencyCode)}</dd>
            </div>
          </dl>
        )}
      </form>

      <div
        className={`client-panel billing-invoice-panel billing-light-panel${
          activeBillingStep === "draft" || activeBillingStep === "issue"
            ? ""
            : " billing-step-hidden"
        }`}
      >
        <div className="client-panel-heading">
          <div>
            <span>Billing setup</span>
            <strong>{activeBillingStep === "issue" ? "Invoice issue" : "Invoice draft"}</strong>
          </div>
          {invoiceDraft !== null && (
            <span className={`status-pill ${invoiceDraft.status.toLowerCase()}`}>
              {invoiceDraft.status}
            </span>
          )}
        </div>

        <form
          className={`billing-subform first${
            activeBillingStep === "draft" ? "" : " billing-step-hidden"
          }`}
          onSubmit={handleGenerateInvoiceDraft}
        >
          <div className="billing-form-grid invoice">
            <label className="form-field wide">
              <span>Contract</span>
              <select
                value={invoiceDraftValue.contractId}
                onChange={(event) =>
                  onInvoiceDraftChange({
                    ...invoiceDraftValue,
                    contractId: event.target.value
                  })
                }
                disabled={isBusy}
              >
                <option value="">Select contract</option>
                {contracts.map((contract) => (
                  <option value={contract.contractId} key={contract.contractId}>
                    {contract.contractNumber} ({contract.status})
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span>Invoice #</span>
              <input
                value={invoiceDraftValue.invoiceNumber}
                onChange={(event) =>
                  onInvoiceDraftChange({
                    ...invoiceDraftValue,
                    invoiceNumber: event.target.value.toUpperCase()
                  })
                }
                disabled={isBusy}
                maxLength={40}
              />
            </label>
            <label className="form-field">
              <span>Issue</span>
              <input
                type="date"
                value={invoiceDraftValue.issueDate}
                onChange={(event) =>
                  onInvoiceDraftChange({
                    ...invoiceDraftValue,
                    issueDate: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Due</span>
              <input
                type="date"
                value={invoiceDraftValue.dueDate}
                onChange={(event) =>
                  onInvoiceDraftChange({
                    ...invoiceDraftValue,
                    dueDate: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Billing date</span>
              <input
                type="date"
                value={invoiceDraftValue.billingDate}
                onChange={(event) =>
                  onInvoiceDraftChange({
                    ...invoiceDraftValue,
                    billingDate: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Currency</span>
              <input
                value={invoiceDraftValue.currencyCode}
                onChange={(event) =>
                  onInvoiceDraftChange({
                    ...invoiceDraftValue,
                    currencyCode: event.target.value.toUpperCase()
                  })
                }
                disabled={isBusy}
                maxLength={3}
              />
            </label>
          </div>
          <div className="billing-action-row">
            <button className="icon-button primary" type="submit" disabled={isBusy} title="Generate invoice draft">
              <FilePlus2 size={16} />
              Draft
            </button>
          </div>
        </form>

        {invoiceDraft === null ? (
          <div className="client-empty-state">No invoice draft</div>
        ) : (
          <div className="billing-draft-result">
            <dl className="billing-result-facts">
              <div>
                <dt>Total</dt>
                <dd>{formatMoney(invoiceDraft.totalAmount, invoiceDraft.currencyCode)}</dd>
              </div>
              <div>
                <dt>Balance</dt>
                <dd>{formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}</dd>
              </div>
              <div>
                <dt>Due</dt>
                <dd>{formatDate(invoiceDraft.dueDate)}</dd>
              </div>
            </dl>

            <table className="billing-lines-table">
              <thead>
                <tr>
                  <th>Description</th>
                  <th className="numeric">Amount</th>
                </tr>
              </thead>
              <tbody>
                {invoiceDraft.lines.map((line, index) => (
                  <tr key={`${line.description}-${index}`}>
                    <td>{line.description}</td>
                    <td className="numeric">{formatMoney(line.amount, line.currencyCode)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <form
              className={`billing-subform issue${
                activeBillingStep === "issue" ? "" : " billing-step-hidden"
              }`}
              onSubmit={handleIssueInvoice}
            >
              <div className="billing-form-grid issue">
                <label className="form-field">
                  <span>Posting date</span>
                  <input
                    type="date"
                    value={issueInvoiceValue.postingDate}
                    onChange={(event) =>
                      onIssueInvoiceChange({
                        ...issueInvoiceValue,
                        postingDate: event.target.value
                      })
                    }
                    disabled={isBusy || issuedInvoice !== null}
                  />
                </label>
                <label className="form-field wide">
                  <span>AR override</span>
                  <input
                    value={issueInvoiceValue.accountsReceivableAccountId}
                    onChange={(event) =>
                      onIssueInvoiceChange({
                        ...issueInvoiceValue,
                        accountsReceivableAccountId: event.target.value
                      })
                    }
                    disabled={isBusy || issuedInvoice !== null}
                  />
                </label>
              </div>
              <div className="billing-action-row">
                <button
                  className="icon-button primary"
                  type="submit"
                  disabled={isBusy || issuedInvoice !== null}
                  title="Issue invoice"
                >
                  <FileCheck2 size={16} />
                  Issue
                </button>
              </div>
            </form>
          </div>
        )}

        {issuedInvoice !== null && (
          <dl
            className={`billing-result-facts issued${
              activeBillingStep === "issue" ? "" : " billing-step-hidden"
            }`}
          >
            <div>
              <dt>Issued</dt>
              <dd>{issuedInvoice.invoiceStatus}</dd>
            </div>
            <div>
              <dt>Journal</dt>
              <dd>{issuedInvoice.journalEntryStatus}</dd>
            </div>
            <div>
              <dt>Debit</dt>
              <dd>{formatMoney(issuedInvoice.totalDebit, issuedInvoice.currencyCode)}</dd>
            </div>
            <div>
              <dt>Credit</dt>
              <dd>{formatMoney(issuedInvoice.totalCredit, issuedInvoice.currencyCode)}</dd>
            </div>
          </dl>
        )}
      </div>
    </section>
  );
}

type BillingStepInput = {
  accountingProfile: ClientAccountingProfile | null;
  accountingProfileMissing: boolean;
  chargeCodes: ChargeCodeLookup[];
  latestChargeRule: ClientChargeRule | null;
  invoiceDraft: InvoiceDraft | null;
  issuedInvoice: IssuedInvoice | null;
};

function getBillingStepItems({
  accountingProfile,
  accountingProfileMissing,
  chargeCodes,
  latestChargeRule,
  invoiceDraft,
  issuedInvoice
}: BillingStepInput): BillingStepItem[] {
  return [
    {
      step: "accounting",
      label: "Accounting profile",
      summary: accountingProfileMissing || accountingProfile === null
        ? "Missing"
        : `${accountingProfile.defaultCurrencyCode} linked`,
      tone: accountingProfileMissing || accountingProfile === null ? "warning" : "ready",
      Icon: Banknote
    },
    {
      step: "rules",
      label: "Charge rules",
      summary: latestChargeRule === null
        ? `${chargeCodes.length} charge codes`
        : `${latestChargeRule.status} ${formatMoney(
            latestChargeRule.lineAmount,
            latestChargeRule.currencyCode
          )}`,
      tone: latestChargeRule === null ? "neutral" : "ready",
      Icon: CircleDollarSign
    },
    {
      step: "draft",
      label: "Invoice draft",
      summary: invoiceDraft === null
        ? "No draft"
        : `${invoiceDraft.status} ${formatMoney(invoiceDraft.balanceDue, invoiceDraft.currencyCode)}`,
      tone: invoiceDraft === null ? "warning" : "ready",
      Icon: FilePlus2
    },
    {
      step: "issue",
      label: "Invoice issue",
      summary: issuedInvoice === null ? "Not issued" : issuedInvoice.invoiceStatus,
      tone: issuedInvoice === null ? "neutral" : "ready",
      Icon: FileCheck2
    }
  ];
}

function chargeRulePatchForChargeCode(
  chargeCodeId: string,
  chargeCodes: ChargeCodeLookup[]
): Partial<ClientChargeRuleFormInput> {
  const chargeCode = chargeCodes.find((item) => item.chargeCodeId === chargeCodeId);

  if (chargeCode === undefined) {
    return {};
  }

  return {
    unitPriceAmount: chargeCode.defaultUnitPriceAmount.toFixed(2),
    currencyCode: chargeCode.currencyCode,
    descriptionOverride: chargeCode.name
  };
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(`${value}T00:00:00`));
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

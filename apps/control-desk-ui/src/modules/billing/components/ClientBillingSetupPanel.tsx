import {
  Banknote,
  CircleDollarSign,
  FileCheck2,
  FilePlus2,
  FileX2,
  Landmark,
  PlusCircle,
  ReceiptText,
  RefreshCw,
  Save,
  type LucideIcon
} from "lucide-react";
import { type FormEvent, useEffect, useState } from "react";
import type { ClientContract, ProductModule } from "../../contracts/types/contractTypes";
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
  IssueCreditNoteInput,
  IssuedCreditNote,
  InvoiceDraft,
  InvoiceDraftFormInput,
  IssueInvoiceFormInput,
  IssuedInvoice,
  LedgerAccountFormInput,
  VoidedInvoice,
  VoidInvoiceInput
} from "../types/billingTypes";

type BillingStep = "accounting" | "rules" | "draft" | "issue";

type ClientBillingSetupPanelProps = {
  client: ClientDetails | null;
  contracts: ClientContract[];
  productModules: ProductModule[];
  initialStep?: BillingStep;
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
  voidedInvoice: VoidedInvoice | null;
  issuedCreditNote: IssuedCreditNote | null;
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
  onVoidInvoice: (input: VoidInvoiceInput) => Promise<void>;
  onIssueCreditNote: (input: IssueCreditNoteInput) => Promise<void>;
};

type ModuleBillingSuggestion = {
  module: ProductModule;
  contract: ClientContract;
  existingChargeCode: ChargeCodeLookup | null;
};

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
  productModules,
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
  voidedInvoice,
  issuedCreditNote,
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
  onIssueInvoice,
  onVoidInvoice,
  onIssueCreditNote,
  initialStep = "accounting"
}: ClientBillingSetupPanelProps) {
  const [activeBillingStep, setActiveBillingStep] = useState<BillingStep>(initialStep);
  const [voidDate, setVoidDate] = useState(toDateInputValue(new Date()));
  const [voidReason, setVoidReason] = useState("");
  const [creditNoteNumber, setCreditNoteNumber] = useState("");
  const [creditDate, setCreditDate] = useState(toDateInputValue(new Date()));
  const [creditReason, setCreditReason] = useState("");

  useEffect(() => {
    setActiveBillingStep(initialStep);
  }, [initialStep]);

  useEffect(() => {
    if (invoiceDraft !== null) {
      setCreditNoteNumber(`CN-${invoiceDraft.invoiceNumber}`);
    }
  }, [invoiceDraft?.invoiceNumber]);

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

  async function handleVoidInvoice(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onVoidInvoice({
      voidDate,
      reason: voidReason
    });
  }

  async function handleIssueCreditNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onIssueCreditNote({
      creditNoteNumber,
      creditDate,
      reason: creditReason
    });
  }

  function handleApplyModuleBillingSuggestion(suggestion: ModuleBillingSuggestion) {
    const billingDefaults = suggestion.module.billingDefaults;

    if (billingDefaults === null || billingDefaults === undefined) {
      return;
    }

    onChargeCodeChange({
      ...chargeCodeValue,
      code: billingDefaults.chargeCode,
      name: billingDefaults.chargeName,
      description: billingDefaults.description,
      defaultUnitPriceAmount: billingDefaults.defaultUnitPriceAmount.toFixed(2),
      currencyCode: billingDefaults.currencyCode
    });
    onChargeRuleChange({
      ...chargeRuleValue,
      contractId: suggestion.contract.contractId,
      chargeCodeId: suggestion.existingChargeCode?.chargeCodeId ?? "",
      productModuleCode: suggestion.module.moduleCode,
      descriptionOverride: billingDefaults.description,
      unitPriceAmount: billingDefaults.defaultUnitPriceAmount.toFixed(2),
      currencyCode: billingDefaults.currencyCode,
      billingCycle: billingDefaults.billingCycle,
      billingDayOfMonth: suggestion.contract.billingDayOfMonth.toString(),
      effectiveStartsOn: suggestion.contract.startsOn,
      effectiveEndsOn: suggestion.contract.endsOn
    });
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
  const canCreateChargeCode =
    chargeCodeValue.code.trim() !== ""
    && chargeCodeValue.name.trim() !== ""
    && Number(chargeCodeValue.defaultUnitPriceAmount) >= 0
    && chargeCodeValue.currencyCode.trim().length === 3
    && chargeCodeValue.revenueAccountId.trim() !== "";
  const canCreateChargeRule =
    chargeRuleValue.contractId.trim() !== ""
    && chargeRuleValue.chargeCodeId.trim() !== ""
    && Number(chargeRuleValue.unitPriceAmount) >= 0
    && Number(chargeRuleValue.quantity) > 0
    && Number(chargeRuleValue.taxPercent) >= 0
    && Number(chargeRuleValue.taxPercent) <= 100
    && chargeRuleValue.currencyCode.trim().length === 3
    && chargeRuleValue.effectiveStartsOn !== ""
    && chargeRuleValue.effectiveEndsOn >= chargeRuleValue.effectiveStartsOn;
  const canGenerateInvoiceDraft =
    invoiceDraftValue.contractId.trim() !== ""
    && invoiceDraftValue.invoiceNumber.trim() !== ""
    && invoiceDraftValue.issueDate !== ""
    && invoiceDraftValue.dueDate >= invoiceDraftValue.issueDate
    && invoiceDraftValue.billingDate !== ""
    && invoiceDraftValue.currencyCode.trim().length === 3;
  const hasInvoiceIssueAccount =
    accountingProfile !== null || issueInvoiceValue.accountsReceivableAccountId.trim() !== "";
  const canIssueInvoice =
    invoiceDraft?.status === "Draft"
    && issuedInvoice === null
    && issueInvoiceValue.postingDate !== ""
    && hasInvoiceIssueAccount;
  const canVoidInvoice =
    issuedInvoice?.invoiceStatus === "Issued"
    && invoiceDraft?.status === "Issued"
    && invoiceDraft.balanceDue === invoiceDraft.totalAmount
    && voidedInvoice === null;
  const canSubmitVoidInvoice =
    canVoidInvoice
    && voidDate !== ""
    && voidReason.trim() !== "";
  const canIssueCreditNote =
    invoiceDraft?.status !== undefined
    && ["Paid", "PartiallyPaid"].includes(invoiceDraft.status)
    && issuedCreditNote === null;
  const canSubmitCreditNote =
    canIssueCreditNote
    && creditNoteNumber.trim() !== ""
    && creditDate !== ""
    && creditReason.trim() !== "";
  const moduleBillingSuggestions = getModuleBillingSuggestions(
    contracts,
    productModules,
    chargeCodes
  );
  const hasLinkedReceivableAccount =
    accountingProfileValue.accountsReceivableAccountId.trim() !== "";
  const hasLinkedRevenueAccount = chargeCodeValue.revenueAccountId.trim() !== "";
  const canCreateReceivableAccount =
    receivableAccountValue.code.trim() !== ""
    && receivableAccountValue.name.trim() !== ""
    && !hasLinkedReceivableAccount;
  const canCreateRevenueAccount =
    revenueAccountValue.code.trim() !== ""
    && revenueAccountValue.name.trim() !== ""
    && !hasLinkedRevenueAccount;
  const canSaveAccountingProfile =
    hasLinkedReceivableAccount
    && accountingProfileValue.defaultCurrencyCode.trim().length === 3
    && accountingProfileValue.cloudCustomerId.trim() !== "";

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
          <AccountDefaults account={receivableAccountValue} />
          <div className="billing-form-grid two">
            <label className="form-field">
              <span>Controlled code</span>
              <input
                value={receivableAccountValue.code}
                disabled={isBusy}
                maxLength={32}
                readOnly
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
            <button
              className="icon-button"
              type="submit"
              disabled={isBusy || !canCreateReceivableAccount}
              title="Create and link AR account"
            >
              <PlusCircle size={16} />
              Create and link
            </button>
            {hasLinkedReceivableAccount && (
              <span className="billing-small-fact">AR account linked</span>
            )}
          </div>
        </form>

        <form className="billing-subform" onSubmit={handleSaveAccountingProfile}>
          <div className="billing-subform-heading">
            <Save size={16} />
            <strong>Profile</strong>
          </div>
          <div className="billing-form-grid three">
            <label className="form-field wide">
              <span>Linked AR account ID</span>
              <input
                value={accountingProfileValue.accountsReceivableAccountId}
                disabled={isBusy}
                readOnly
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
            <button
              className="icon-button primary"
              type="submit"
              disabled={isBusy || !canSaveAccountingProfile}
              title="Save accounting profile"
            >
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
          <AccountDefaults account={revenueAccountValue} />
          <div className="billing-form-grid two">
            <label className="form-field">
              <span>Controlled code</span>
              <input
                value={revenueAccountValue.code}
                disabled={isBusy}
                maxLength={32}
                readOnly
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
            <button
              className="icon-button"
              type="submit"
              disabled={isBusy || !canCreateRevenueAccount}
              title="Create revenue account"
            >
              <PlusCircle size={16} />
              Create
            </button>
            {hasLinkedRevenueAccount && (
              <span className="billing-small-fact">Revenue account selected</span>
            )}
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
              <span>Tax account ID</span>
              <input
                value={chargeCodeValue.taxAccountId}
                onChange={(event) =>
                  onChargeCodeChange({
                    ...chargeCodeValue,
                    taxAccountId: event.target.value
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
            <button
              className="icon-button primary"
              type="submit"
              disabled={isBusy || !canCreateChargeCode}
              title="Create charge code"
            >
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
          <button
            className="icon-button primary"
            type="submit"
            disabled={isBusy || !canCreateChargeRule}
            title="Add charge rule"
          >
            <PlusCircle size={16} />
            Add
          </button>
        </div>

        {moduleBillingSuggestions.length > 0 && (
          <div className="billing-module-suggestions">
            {moduleBillingSuggestions.map((suggestion) => {
              const billingDefaults = suggestion.module.billingDefaults!;

              return (
                <button
                  className="billing-module-suggestion"
                  type="button"
                  key={suggestion.module.moduleCode}
                  onClick={() => handleApplyModuleBillingSuggestion(suggestion)}
                  disabled={isBusy}
                >
                  <FilePlus2 size={15} />
                  <span>
                    <strong>{suggestion.module.displayName}</strong>
                    <small>
                      {billingDefaults.chargeCode} - {formatMoney(
                        billingDefaults.defaultUnitPriceAmount,
                        billingDefaults.currencyCode
                      )} - {suggestion.existingChargeCode === null ? "new code" : "code ready"}
                    </small>
                  </span>
                </button>
              );
            })}
          </div>
        )}

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
            <span>Module</span>
            <input
              value={chargeRuleValue.productModuleCode}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  productModuleCode: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy}
              maxLength={64}
            />
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
            <span>Tax %</span>
            <input
              type="number"
              min="0"
              max="100"
              step="0.01"
              value={chargeRuleValue.taxPercent}
              onChange={(event) =>
                onChargeRuleChange({
                  ...chargeRuleValue,
                  taxPercent: event.target.value
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
            <div>
              <dt>Tax</dt>
              <dd>{formatMoney(latestChargeRule.taxAmount, latestChargeRule.currencyCode)}</dd>
            </div>
            <div>
              <dt>Total</dt>
              <dd>{formatMoney(latestChargeRule.totalLineAmount, latestChargeRule.currencyCode)}</dd>
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
            <button
              className="icon-button primary"
              type="submit"
              disabled={isBusy || !canGenerateInvoiceDraft}
              title="Generate invoice draft"
            >
              <FilePlus2 size={16} />
              Draft
            </button>
            <span className="billing-small-fact">
              {canGenerateInvoiceDraft ? "Ready" : "Contract, dates, and currency required"}
            </span>
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
                  <th>Type</th>
                  <th>Module</th>
                  <th>Description</th>
                  <th className="numeric">Amount</th>
                </tr>
              </thead>
              <tbody>
                {invoiceDraft.lines.map((line, index) => (
                  <tr key={`${line.description}-${index}`}>
                    <td>{line.lineType}</td>
                    <td>{line.productModuleCode ?? "-"}</td>
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
                    disabled={isBusy || issuedInvoice !== null || invoiceDraft.status !== "Draft"}
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
                    disabled={isBusy || issuedInvoice !== null || invoiceDraft.status !== "Draft"}
                  />
                </label>
              </div>
              <div className="billing-action-row">
                <button
                  className="icon-button primary"
                  type="submit"
                  disabled={!canIssueInvoice}
                  title="Issue invoice"
                >
                  <FileCheck2 size={16} />
                  Issue
                </button>
                <span className="billing-small-fact">
                  {invoiceDraft.status !== "Draft"
                    ? "Already issued"
                    : !hasInvoiceIssueAccount
                      ? "AR account required"
                      : "Posts AR, revenue, tax, and outbox"}
                </span>
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

        {canVoidInvoice && (
          <form
            className={`billing-subform billing-void-form${
              activeBillingStep === "issue" ? "" : " billing-step-hidden"
            }`}
            onSubmit={handleVoidInvoice}
          >
            <div className="billing-form-grid issue">
              <label className="form-field">
                <span>Void date</span>
                <input
                  type="date"
                  value={voidDate}
                  onChange={(event) => setVoidDate(event.target.value)}
                  disabled={isBusy}
                />
              </label>
              <label className="form-field wide">
                <span>Reason</span>
                <input
                  value={voidReason}
                  onChange={(event) => setVoidReason(event.target.value)}
                  disabled={isBusy}
                  maxLength={512}
                />
              </label>
            </div>
            <div className="billing-action-row">
              <button
                className="icon-button danger"
                type="submit"
                disabled={!canSubmitVoidInvoice}
                title="Void invoice"
              >
                <FileX2 size={16} />
                Void
              </button>
              <span className="billing-small-fact">
                {voidReason.trim() === "" ? "Reason required" : "Posts reversal journal"}
              </span>
            </div>
          </form>
        )}

        {voidedInvoice !== null && (
          <dl
            className={`billing-result-facts issued${
              activeBillingStep === "issue" ? "" : " billing-step-hidden"
            }`}
          >
            <div>
              <dt>Voided</dt>
              <dd>{voidedInvoice.invoiceStatus}</dd>
            </div>
            <div>
              <dt>Reversal</dt>
              <dd>{voidedInvoice.reversalJournalEntryStatus}</dd>
            </div>
            <div>
              <dt>Debit</dt>
              <dd>{formatMoney(voidedInvoice.totalDebit, voidedInvoice.currencyCode)}</dd>
            </div>
            <div>
              <dt>Credit</dt>
              <dd>{formatMoney(voidedInvoice.totalCredit, voidedInvoice.currencyCode)}</dd>
            </div>
          </dl>
        )}

        {canIssueCreditNote && (
          <form
            className={`billing-subform billing-credit-form${
              activeBillingStep === "issue" ? "" : " billing-step-hidden"
            }`}
            onSubmit={handleIssueCreditNote}
          >
            <div className="billing-form-grid issue">
              <label className="form-field">
                <span>Credit #</span>
                <input
                  value={creditNoteNumber}
                  onChange={(event) => setCreditNoteNumber(event.target.value.toUpperCase())}
                  disabled={isBusy}
                  maxLength={40}
                />
              </label>
              <label className="form-field">
                <span>Credit date</span>
                <input
                  type="date"
                  value={creditDate}
                  onChange={(event) => setCreditDate(event.target.value)}
                  disabled={isBusy}
                />
              </label>
              <label className="form-field wide">
                <span>Reason</span>
                <input
                  value={creditReason}
                  onChange={(event) => setCreditReason(event.target.value)}
                  disabled={isBusy}
                  maxLength={512}
                />
              </label>
            </div>
            <div className="billing-action-row">
              <button
                className="icon-button"
                type="submit"
                disabled={!canSubmitCreditNote}
                title="Issue credit note"
              >
                <Landmark size={16} />
                Credit
              </button>
              <span className="billing-small-fact">
                {creditReason.trim() === "" ? "Reason required" : "Posts credit-note journal"}
              </span>
            </div>
          </form>
        )}

        {issuedCreditNote !== null && (
          <dl
            className={`billing-result-facts issued${
              activeBillingStep === "issue" ? "" : " billing-step-hidden"
            }`}
          >
            <div>
              <dt>Credit note</dt>
              <dd>{issuedCreditNote.creditNoteStatus}</dd>
            </div>
            <div>
              <dt>Amount</dt>
              <dd>{formatMoney(issuedCreditNote.amount, issuedCreditNote.currencyCode)}</dd>
            </div>
            <div>
              <dt>Debit</dt>
              <dd>{formatMoney(issuedCreditNote.totalDebit, issuedCreditNote.currencyCode)}</dd>
            </div>
            <div>
              <dt>Credit</dt>
              <dd>{formatMoney(issuedCreditNote.totalCredit, issuedCreditNote.currencyCode)}</dd>
            </div>
          </dl>
        )}
      </div>
    </section>
  );
}

function AccountDefaults({ account }: { account: LedgerAccountFormInput }) {
  return (
    <div className="billing-account-defaults">
      <span>{formatLedgerAccountCode(account.code)}</span>
      <span>{account.type}</span>
      <span>{account.normalBalance}</span>
      <span>{account.isPostingAccount ? "Posting account" : "Header account"}</span>
    </div>
  );
}

function formatLedgerAccountCode(code: string): string {
  return /^\d{9}$/.test(code)
    ? `${code.slice(0, 5)}-${code.slice(5)}`
    : code;
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
            latestChargeRule.totalLineAmount,
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

function getModuleBillingSuggestions(
  contracts: ClientContract[],
  productModules: ProductModule[],
  chargeCodes: ChargeCodeLookup[]
): ModuleBillingSuggestion[] {
  const activeContract = getActiveContract(contracts);

  if (activeContract === null) {
    return [];
  }

  const enabledModuleCodes = new Set(
    activeContract.modules
      .filter((module) => module.isEnabled)
      .map((module) => module.moduleCode)
  );

  return productModules
    .filter((module) =>
      module.isActive
      && module.commercialMode === "PaidAddOn"
      && module.billingDefaults !== null
      && module.billingDefaults !== undefined
      && enabledModuleCodes.has(module.moduleCode))
    .map((module) => ({
      module,
      contract: activeContract,
      existingChargeCode: chargeCodes.find(
        (chargeCode) => chargeCode.code === module.billingDefaults!.chargeCode
      ) ?? null
    }));
}

function getActiveContract(contracts: ClientContract[]): ClientContract | null {
  return contracts.find((contract) => contract.status.toLowerCase() === "active")
    ?? contracts[0]
    ?? null;
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${amount.toFixed(2)} ${currencyCode}`;
}

function toDateInputValue(value: Date): string {
  return value.toISOString().slice(0, 10);
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

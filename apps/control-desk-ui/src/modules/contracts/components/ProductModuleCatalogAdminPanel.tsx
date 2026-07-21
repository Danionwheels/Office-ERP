import {
  AlertCircle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Edit3,
  PauseCircle,
  PlayCircle,
  Plus,
  Save
} from "lucide-react";
import { Fragment, useMemo, useState, type FormEvent } from "react";
import type {
  ProductAccessCatalog,
  ProductModule,
  ProductModuleBillingDefaults
} from "../types/contractTypes";

type ProductModuleCatalogAdminPanelProps = {
  catalog: ProductAccessCatalog;
  publishedModules: ProductModule[];
  requestedBy: string;
  changeReason: string;
  isBusy: boolean;
  error: string;
  message: string;
  onSaveCatalog: (catalog: ProductAccessCatalog, requestedBy: string) => Promise<boolean>;
};

type ProductModuleCommercialMode = "IncludedForAll" | "PaidAddOn";
type ProductModuleEditorMode = "create" | "edit";

type ProductModuleFormValue = {
  moduleCode: string;
  displayName: string;
  description: string;
  commercialMode: ProductModuleCommercialMode;
  hasBillingDefaults: boolean;
  chargeCode: string;
  chargeName: string;
  billingDescription: string;
  defaultUnitPriceAmount: string;
  currencyCode: string;
  billingCycle: string;
};

const billingCycles = ["Monthly", "Quarterly", "SemiAnnual", "Annual", "Manual"];

const emptyModuleForm: ProductModuleFormValue = {
  moduleCode: "",
  displayName: "",
  description: "",
  commercialMode: "PaidAddOn",
  hasBillingDefaults: true,
  chargeCode: "",
  chargeName: "",
  billingDescription: "",
  defaultUnitPriceAmount: "0.00",
  currencyCode: "PKR",
  billingCycle: "Monthly"
};

export function ProductModuleCatalogAdminPanel({
  catalog,
  publishedModules,
  requestedBy,
  changeReason,
  isBusy,
  error,
  message,
  onSaveCatalog
}: ProductModuleCatalogAdminPanelProps) {
  const [editorMode, setEditorMode] = useState<ProductModuleEditorMode>("create");
  const [form, setForm] = useState<ProductModuleFormValue>(emptyModuleForm);
  const [formError, setFormError] = useState("");
  const [expandedModuleCode, setExpandedModuleCode] = useState("");
  const displayedModules = useMemo(
    () => mergePublishedReferences(catalog.modules, publishedModules),
    [catalog.modules, publishedModules]
  );
  const editingModule = editorMode === "edit"
    ? catalog.modules.find((module) => moduleCodeEquals(module.moduleCode, form.moduleCode)) ?? null
    : null;

  function startCreate() {
    setEditorMode("create");
    setForm(emptyModuleForm);
    setFormError("");
  }

  function startEdit(module: ProductModule) {
    setEditorMode("edit");
    setForm(toModuleForm(module));
    setFormError("");
  }

  function updateForm(patch: Partial<ProductModuleFormValue>) {
    setForm((current) => ({ ...current, ...patch }));
    setFormError("");
  }

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const validationError = validateModuleForm(form, catalog.modules, editorMode);

    if (validationError !== null) {
      setFormError(validationError);
      return;
    }

    const nextModule = toProductModule(form, editingModule);
    const nextModules = editorMode === "create"
      ? [...catalog.modules, nextModule]
      : catalog.modules.map((module) =>
        moduleCodeEquals(module.moduleCode, nextModule.moduleCode) ? nextModule : module
      );

    if (await saveModules(nextModules)) {
      startCreate();
    }
  }

  async function handleStatusChange(module: ProductModule) {
    const nextModules = catalog.modules.map((item) =>
      moduleCodeEquals(item.moduleCode, module.moduleCode)
        ? { ...item, isActive: !item.isActive }
        : item
    );

    await saveModules(nextModules);
  }

  async function saveModules(modules: ProductModule[]): Promise<boolean> {
    if (changeReason.trim() === "") {
      setFormError("Change reason is required before saving the product catalog draft.");
      return false;
    }

    if (changeReason.trim().length > 1000) {
      setFormError("Change reason cannot exceed 1,000 characters.");
      return false;
    }

    return onSaveCatalog(
      {
        ...catalog,
        changeReason: changeReason.trim(),
        modules: sortModules(modules)
      },
      requestedBy.trim() === "" ? "Control Desk" : requestedBy.trim()
    );

  }

  return (
    <section className="setup-focus-panel setup-product-module-admin-panel">
      <div className="setup-panel-heading product-module-admin-heading">
        <div>
          <span>Commercial modules</span>
          <strong>{displayedModules.length} records</strong>
        </div>
        <button
          className="icon-button"
          disabled={isBusy}
          onClick={startCreate}
          type="button"
        >
          <Plus size={16} />
          New module
        </button>
      </div>

      <p className="product-module-admin-note">
        Module changes are saved to the mutable draft. Contract selection and billing suggestions
        continue to use the latest published revision until this catalog is published.
      </p>

      {error !== "" && (
        <div className="setup-inline-error" role="alert">
          <AlertCircle size={14} />
          <span>{error}</span>
        </div>
      )}
      {message !== "" && (
        <div className="setup-inline-success" role="status">
          <CheckCircle2 size={14} />
          <span>{message}</span>
        </div>
      )}

      <div className="product-module-admin-table-frame">
        <table className="product-module-admin-table">
          <thead>
            <tr>
              <th scope="col">Code</th>
              <th scope="col">Name</th>
              <th scope="col">Type</th>
              <th scope="col">Billing defaults</th>
              <th scope="col">Status</th>
              <th scope="col">Referenced by</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {displayedModules.length === 0 ? (
              <tr>
                <td className="product-module-admin-empty" colSpan={7}>
                  No product modules have been defined.
                </td>
              </tr>
            ) : (
              displayedModules.map((module) => {
                const isExpanded = expandedModuleCode === module.moduleCode;
                const referenceCount = module.referencedBy.length;

                return (
                  <Fragment key={module.moduleCode}>
                    <tr className={module.isActive ? "" : "inactive"}>
                      <td>
                        <strong className="product-module-code">{module.moduleCode}</strong>
                      </td>
                      <td>
                        <span className="product-module-name-cell">
                          <strong>{module.displayName}</strong>
                          <small>{module.description || "No description"}</small>
                        </span>
                      </td>
                      <td>{formatCommercialMode(module.commercialMode)}</td>
                      <td>{formatBillingDefaults(module.billingDefaults)}</td>
                      <td>
                        <span className={`status-pill ${module.isActive ? "active" : "inactive"}`}>
                          {module.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td>
                        <button
                          aria-expanded={isExpanded}
                          className="mini-button product-module-reference-button"
                          disabled={referenceCount === 0}
                          onClick={() => setExpandedModuleCode(isExpanded ? "" : module.moduleCode)}
                          type="button"
                        >
                          {isExpanded ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
                          {referenceCount === 0
                            ? "No active contracts"
                            : `${referenceCount} active ${referenceCount === 1 ? "contract" : "contracts"}`}
                        </button>
                      </td>
                      <td>
                        <div className="product-module-row-actions">
                          <button
                            className="mini-button"
                            disabled={isBusy}
                            onClick={() => startEdit(module)}
                            type="button"
                          >
                            <Edit3 size={13} />
                            Edit
                          </button>
                          <button
                            className="mini-button"
                            disabled={isBusy}
                            onClick={() => handleStatusChange(module)}
                            type="button"
                          >
                            {module.isActive ? <PauseCircle size={13} /> : <PlayCircle size={13} />}
                            {module.isActive ? "Deactivate" : "Activate"}
                          </button>
                        </div>
                      </td>
                    </tr>
                    {isExpanded && referenceCount > 0 && (
                      <tr className="product-module-reference-row">
                        <td colSpan={7}>
                          <div className="product-module-reference-list">
                            {module.referencedBy.map((reference) => (
                              <article key={reference.contractId}>
                                <span>
                                  <strong>{reference.contractNumber}</strong>
                                  <small>Contract revision #{reference.contractRevisionNumber}</small>
                                </span>
                                <small>Client {reference.clientId}</small>
                              </article>
                            ))}
                          </div>
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      <form className="product-module-editor" noValidate onSubmit={handleSave}>
        <div className="setup-form-heading product-module-editor-heading">
          <span>{editorMode === "create" ? "Create module" : "Edit module"}</span>
          <strong>{editorMode === "create" ? "New catalog record" : form.moduleCode}</strong>
        </div>

        {formError !== "" && (
          <div className="setup-inline-error product-module-editor-error" role="alert">
            <AlertCircle size={14} />
            <span>{formError}</span>
          </div>
        )}

        <div className="product-module-editor-grid">
          <label className="form-field">
            <span>Code</span>
            <input
              disabled={isBusy}
              maxLength={64}
              readOnly={editorMode === "edit"}
              value={form.moduleCode}
              onChange={(event) => updateForm({ moduleCode: event.target.value.toUpperCase() })}
            />
          </label>
          <label className="form-field">
            <span>Name</span>
            <input
              disabled={isBusy}
              maxLength={128}
              value={form.displayName}
              onChange={(event) => updateForm({ displayName: event.target.value })}
            />
          </label>
          <label className="form-field">
            <span>Type</span>
            <select
              disabled={isBusy || editorMode === "edit"}
              value={form.commercialMode}
              onChange={(event) => updateForm({
                commercialMode: event.target.value as ProductModuleCommercialMode
              })}
            >
              <option value="IncludedForAll">Included for all</option>
              <option value="PaidAddOn">Paid add-on</option>
            </select>
          </label>
          <label className="form-field product-module-description-field">
            <span>Description</span>
            <textarea
              disabled={isBusy}
              maxLength={1000}
              rows={3}
              value={form.description}
              onChange={(event) => updateForm({ description: event.target.value })}
            />
          </label>
        </div>

        <label className="checkbox-field product-module-billing-toggle">
          <input
            checked={form.hasBillingDefaults}
            disabled={isBusy}
            type="checkbox"
            onChange={(event) => updateForm({ hasBillingDefaults: event.target.checked })}
          />
          <span>Configure billing defaults</span>
        </label>

        {form.hasBillingDefaults && (
          <fieldset className="product-module-billing-fields" disabled={isBusy}>
            <legend>Billing defaults</legend>
            <label className="form-field">
              <span>Charge code</span>
              <input
                maxLength={32}
                value={form.chargeCode}
                onChange={(event) => updateForm({ chargeCode: event.target.value.toUpperCase() })}
              />
            </label>
            <label className="form-field">
              <span>Charge name</span>
              <input
                maxLength={128}
                value={form.chargeName}
                onChange={(event) => updateForm({ chargeName: event.target.value })}
              />
            </label>
            <label className="form-field">
              <span>Unit price</span>
              <input
                min="0"
                step="0.01"
                type="number"
                value={form.defaultUnitPriceAmount}
                onChange={(event) => updateForm({ defaultUnitPriceAmount: event.target.value })}
              />
            </label>
            <label className="form-field">
              <span>Currency</span>
              <input
                maxLength={3}
                value={form.currencyCode}
                onChange={(event) => updateForm({ currencyCode: event.target.value.toUpperCase() })}
              />
            </label>
            <label className="form-field">
              <span>Billing cycle</span>
              <select
                value={form.billingCycle}
                onChange={(event) => updateForm({ billingCycle: event.target.value })}
              >
                {billingCycles.map((cycle) => (
                  <option key={cycle} value={cycle}>{formatBillingCycle(cycle)}</option>
                ))}
              </select>
            </label>
            <label className="form-field product-module-billing-description-field">
              <span>Billing description</span>
              <textarea
                maxLength={256}
                rows={2}
                value={form.billingDescription}
                onChange={(event) => updateForm({ billingDescription: event.target.value })}
              />
            </label>
          </fieldset>
        )}

        <div className="product-module-editor-actions">
          <button className="icon-button primary" disabled={isBusy} type="submit">
            <Save size={16} />
            {editorMode === "create" ? "Create in draft" : "Save draft changes"}
          </button>
          {editorMode === "edit" && (
            <button className="icon-button" disabled={isBusy} onClick={startCreate} type="button">
              <Plus size={16} />
              New module
            </button>
          )}
        </div>
      </form>
    </section>
  );
}

function validateModuleForm(
  value: ProductModuleFormValue,
  modules: ProductModule[],
  mode: ProductModuleEditorMode
): string | null {
  const moduleCode = value.moduleCode.trim();
  const displayName = value.displayName.trim();

  if (moduleCode === "") {
    return "Module code is required.";
  }

  if (moduleCode.length > 64) {
    return "Module code cannot exceed 64 characters.";
  }

  if (mode === "create" && modules.some((module) => moduleCodeEquals(module.moduleCode, moduleCode))) {
    return `Module code ${moduleCode.toUpperCase()} already exists.`;
  }

  if (displayName === "") {
    return "Module name is required.";
  }

  if (displayName.length > 128) {
    return "Module name cannot exceed 128 characters.";
  }

  if (value.description.trim().length > 1000) {
    return "Module description cannot exceed 1,000 characters.";
  }

  if (!value.hasBillingDefaults) {
    return null;
  }

  const chargeCodeLength = value.chargeCode.trim().length;

  if (chargeCodeLength < 2 || chargeCodeLength > 32) {
    return "Billing charge code must be between 2 and 32 characters.";
  }

  if (value.chargeName.trim() === "") {
    return "Billing charge name is required.";
  }

  if (value.chargeName.trim().length > 128) {
    return "Billing charge name cannot exceed 128 characters.";
  }

  if (value.billingDescription.trim().length > 256) {
    return "Billing description cannot exceed 256 characters.";
  }

  const unitPrice = Number(value.defaultUnitPriceAmount);

  if (value.defaultUnitPriceAmount.trim() === "" || !Number.isFinite(unitPrice) || unitPrice < 0) {
    return "Billing default unit price must be zero or greater.";
  }

  if (value.currencyCode.trim().length !== 3) {
    return "Billing currency code must be exactly 3 characters.";
  }

  if (!billingCycles.includes(value.billingCycle)) {
    return "Billing cycle is not supported.";
  }

  return null;
}

function toProductModule(
  value: ProductModuleFormValue,
  existing: ProductModule | null
): ProductModule {
  return {
    moduleCode: existing?.moduleCode ?? value.moduleCode.trim().toUpperCase(),
    displayName: value.displayName.trim(),
    description: value.description.trim(),
    commercialMode: existing?.commercialMode ?? value.commercialMode,
    isActive: existing?.isActive ?? true,
    billingDefaults: value.hasBillingDefaults ? toBillingDefaults(value) : null,
    compatibility: existing?.compatibility ?? {
      minimumSafarSuiteVersion: null,
      minimumLocalServerVersion: null,
      supportedDeploymentModes: []
    },
    referencedBy: existing?.referencedBy ?? []
  };
}

function toBillingDefaults(value: ProductModuleFormValue): ProductModuleBillingDefaults {
  return {
    chargeCode: value.chargeCode.trim().toUpperCase(),
    chargeName: value.chargeName.trim(),
    description: value.billingDescription.trim(),
    defaultUnitPriceAmount: Number(value.defaultUnitPriceAmount),
    currencyCode: value.currencyCode.trim().toUpperCase(),
    billingCycle: value.billingCycle
  };
}

function toModuleForm(module: ProductModule): ProductModuleFormValue {
  const defaults = module.billingDefaults;

  return {
    moduleCode: module.moduleCode,
    displayName: module.displayName,
    description: module.description,
    commercialMode: module.commercialMode === "IncludedForAll" ? "IncludedForAll" : "PaidAddOn",
    hasBillingDefaults: defaults !== null && defaults !== undefined,
    chargeCode: defaults?.chargeCode ?? "",
    chargeName: defaults?.chargeName ?? "",
    billingDescription: defaults?.description ?? "",
    defaultUnitPriceAmount: defaults?.defaultUnitPriceAmount.toFixed(2) ?? "0.00",
    currencyCode: defaults?.currencyCode ?? "PKR",
    billingCycle: defaults?.billingCycle ?? "Monthly"
  };
}

function mergePublishedReferences(
  catalogModules: ProductModule[],
  publishedModules: ProductModule[]
): ProductModule[] {
  const publishedByCode = new Map(
    publishedModules.map((module) => [module.moduleCode.trim().toUpperCase(), module])
  );

  return sortModules(catalogModules).map((module) => ({
    ...module,
    referencedBy: publishedByCode.get(module.moduleCode.trim().toUpperCase())?.referencedBy
      ?? module.referencedBy
      ?? []
  }));
}

function sortModules(modules: ProductModule[]): ProductModule[] {
  return [...modules].sort((left, right) => left.moduleCode.localeCompare(right.moduleCode));
}

function moduleCodeEquals(left: string, right: string): boolean {
  return left.trim().localeCompare(right.trim(), undefined, { sensitivity: "accent" }) === 0;
}

function formatCommercialMode(value: string): string {
  if (value === "IncludedForAll") {
    return "Included for all";
  }

  if (value === "PaidAddOn") {
    return "Paid add-on";
  }

  return value;
}

function formatBillingDefaults(defaults: ProductModuleBillingDefaults | null | undefined): string {
  if (defaults === null || defaults === undefined) {
    return "Not configured";
  }

  return `${defaults.chargeCode} · ${formatAmount(defaults.defaultUnitPriceAmount)} ${defaults.currencyCode} · ${formatBillingCycle(defaults.billingCycle)}`;
}

function formatAmount(value: number): string {
  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0
  }).format(value);
}

function formatBillingCycle(value: string): string {
  return value === "SemiAnnual" ? "Semi-annual" : value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

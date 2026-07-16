import {
  CheckCircle2,
  Edit3,
  GitBranch,
  History,
  Package,
  PauseCircle,
  PlayCircle,
  Plus,
  RefreshCw,
  Save,
  Send,
  Trash2,
  Upload
} from "lucide-react";
import { type FormEvent, useState } from "react";
import type {
  ProductAccessCatalog,
  ProductAccessKind,
  ProductModule,
  ProductModuleGroup,
  ProductResource,
  PublishedProductAccessCatalogCommand,
  PublishProductAccessCatalogCommandInput
} from "../types/contractTypes";

type ProductAccessCatalogPanelProps = {
  catalog: ProductAccessCatalog | null;
  revisions: ProductAccessCatalog[];
  publishedCommand: PublishedProductAccessCatalogCommand | null;
  value: PublishProductAccessCatalogCommandInput;
  isBusy: boolean;
  onChange: (value: PublishProductAccessCatalogCommandInput) => void;
  onRefresh: () => Promise<void>;
  onSaveCatalog: (catalog: ProductAccessCatalog, requestedBy: string) => Promise<void>;
  onPublishRevision: () => Promise<void>;
  onPublish: () => Promise<void>;
};

type ProductModuleFormInput = {
  moduleCode: string;
  displayName: string;
  description: string;
  commercialMode: string;
  isActive: boolean;
  minimumSafarSuiteVersion: string;
  minimumLocalServerVersion: string;
  supportedDeploymentModes: string;
};

type ProductModuleGroupFormInput = {
  groupId: string;
  displayName: string;
  accessKind: ProductAccessKind;
  moduleCodes: string;
};

type ProductResourceFormInput = {
  resourceId: string;
  displayName: string;
  accessKind: ProductAccessKind;
  requiredGroupIds: string;
  requiredModuleCodes: string;
};

const emptyGroupForm: ProductModuleGroupFormInput = {
  groupId: "",
  displayName: "",
  accessKind: "PaidModule",
  moduleCodes: ""
};

const emptyModuleForm: ProductModuleFormInput = {
  moduleCode: "",
  displayName: "",
  description: "",
  commercialMode: "PaidAddOn",
  isActive: true,
  minimumSafarSuiteVersion: "",
  minimumLocalServerVersion: "",
  supportedDeploymentModes: ""
};

const emptyResourceForm: ProductResourceFormInput = {
  resourceId: "",
  displayName: "",
  accessKind: "PaidModule",
  requiredGroupIds: "",
  requiredModuleCodes: ""
};

export function ProductAccessCatalogPanel({
  catalog,
  revisions,
  publishedCommand,
  value,
  isBusy,
  onChange,
  onRefresh,
  onSaveCatalog,
  onPublishRevision,
  onPublish
}: ProductAccessCatalogPanelProps) {
  const [moduleForm, setModuleForm] = useState<ProductModuleFormInput>(emptyModuleForm);
  const [editingModuleCode, setEditingModuleCode] = useState("");
  const [groupForm, setGroupForm] = useState<ProductModuleGroupFormInput>(emptyGroupForm);
  const [resourceForm, setResourceForm] = useState<ProductResourceFormInput>(emptyResourceForm);
  const editableCatalog: ProductAccessCatalog = catalog ?? emptyCatalog();
  const modules = editableCatalog.modules;
  const groups = editableCatalog.moduleGroups;
  const resources = editableCatalog.resources;
  const moduleCount = modules.length;

  async function handlePublish(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onPublish();
  }

  async function handleSaveModule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const existing = modules.find((module) => equalsId(module.moduleCode, moduleForm.moduleCode));
    const module: ProductModule = {
      moduleCode: moduleForm.moduleCode.trim().toUpperCase(),
      displayName: moduleForm.displayName.trim(),
      description: moduleForm.description.trim(),
      commercialMode: existing?.commercialMode ?? moduleForm.commercialMode,
      isActive: moduleForm.isActive,
      billingDefaults: existing?.billingDefaults ?? null,
      compatibility: {
        minimumSafarSuiteVersion: nullIfEmpty(moduleForm.minimumSafarSuiteVersion),
        minimumLocalServerVersion: nullIfEmpty(moduleForm.minimumLocalServerVersion),
        supportedDeploymentModes: splitValues(moduleForm.supportedDeploymentModes)
      },
      referencedBy: existing?.referencedBy ?? []
    };
    const nextCatalog = withChangeReason(
      {
        ...editableCatalog,
        modules: upsertById(modules, module, (item) => item.moduleCode, module.moduleCode)
      },
      value.changeReason);

    await onSaveCatalog(nextCatalog, requestedBy(value));
    setModuleForm(emptyModuleForm);
    setEditingModuleCode("");
  }

  async function handleSaveGroup(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const group = {
      groupId: groupForm.groupId.trim(),
      displayName: groupForm.displayName.trim(),
      accessKind: groupForm.accessKind,
      moduleCodes: splitValues(groupForm.moduleCodes)
    };
    const nextCatalog = {
      ...editableCatalog,
      moduleGroups: upsertById(
        groups,
        group,
        (item) => item.groupId,
        group.groupId)
    };

    await onSaveCatalog(withChangeReason(nextCatalog, value.changeReason), requestedBy(value));
    setGroupForm(emptyGroupForm);
  }

  async function handleSaveResource(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const resource = {
      resourceId: resourceForm.resourceId.trim(),
      displayName: resourceForm.displayName.trim(),
      accessKind: resourceForm.accessKind,
      requiredGroupIds: splitValues(resourceForm.requiredGroupIds),
      requiredModuleCodes: splitValues(resourceForm.requiredModuleCodes),
      resolvedModuleCodes: []
    };
    const nextCatalog = {
      ...editableCatalog,
      resources: upsertById(
        resources,
        resource,
        (item) => item.resourceId,
        resource.resourceId)
    };

    await onSaveCatalog(withChangeReason(nextCatalog, value.changeReason), requestedBy(value));
    setResourceForm(emptyResourceForm);
  }

  async function handleRemoveGroup(groupId: string) {
    const nextCatalog = {
      ...editableCatalog,
      moduleGroups: groups.filter((group) => !equalsId(group.groupId, groupId)),
      resources: resources.map((resource) => ({
        ...resource,
        requiredGroupIds: resource.requiredGroupIds.filter((item) => !equalsId(item, groupId))
      }))
    };

    await onSaveCatalog(withChangeReason(nextCatalog, value.changeReason), requestedBy(value));
  }

  async function handleSetModuleStatus(moduleCode: string, isActive: boolean) {
    await onSaveCatalog(
      withChangeReason(
        {
          ...editableCatalog,
          modules: modules.map((module) =>
            equalsId(module.moduleCode, moduleCode) ? { ...module, isActive } : module
          )
        },
        value.changeReason),
      requestedBy(value));
  }

  async function handleRemoveResource(resourceId: string) {
    await onSaveCatalog(
      {
        ...withChangeReason(editableCatalog, value.changeReason),
        resources: resources.filter((resource) => !equalsId(resource.resourceId, resourceId))
      },
      requestedBy(value));
  }

  return (
    <section className="client-panel product-access-catalog-panel">
      <div className="client-panel-heading">
        <div>
          <span>Product access</span>
          <strong>Catalog</strong>
        </div>
        <div className="client-panel-actions">
          <button
            className="icon-button"
            type="button"
            disabled={isBusy}
            onClick={onRefresh}
            title="Refresh catalog"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>
      </div>

      {catalog !== null && (
        <div className="product-catalog-revision-bar">
          <div>
            <span className={`status-pill ${catalog.state.toLowerCase()}`}>{catalog.state}</span>
            <strong>{catalogRevisionLabel(catalog)}</strong>
            <small>
              {catalog.changedBy === "" ? "-" : catalog.changedBy}
              {catalog.changedAtUtc === null ? "" : ` / ${formatDateTime(catalog.changedAtUtc)}`}
            </small>
          </div>
          <label className="form-field product-catalog-reason-field">
            <span>Change reason</span>
            <input
              value={value.changeReason}
              onChange={(event) => onChange({ ...value, changeReason: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <button
            className="icon-button primary"
            type="button"
            disabled={isBusy || catalog.state.toLowerCase() !== "draft"}
            onClick={onPublishRevision}
            title="Publish immutable catalog revision"
          >
            <Upload size={16} />
            Publish revision
          </button>
        </div>
      )}

      <div className="product-access-summary-grid">
        <ProductAccessSummaryCard label="Groups" value={groups.length.toString()} />
        <ProductAccessSummaryCard label="Resources" value={resources.length.toString()} />
        <ProductAccessSummaryCard label="Modules" value={moduleCount.toString()} />
      </div>

      <div className="product-access-editor-grid">
        <form className="product-access-edit-form" onSubmit={handleSaveModule}>
          <div className="product-access-section-heading">
            <span>Commercial module</span>
            <strong>{moduleForm.moduleCode.trim() === "" ? "New" : "Edit"}</strong>
          </div>
          <label className="form-field">
            <span>Module code</span>
            <input
              value={moduleForm.moduleCode}
              onChange={(event) => setModuleForm({ ...moduleForm, moduleCode: event.target.value })}
              disabled={isBusy}
              readOnly={editingModuleCode !== ""}
            />
          </label>
          <label className="form-field">
            <span>Description</span>
            <textarea
              maxLength={1000}
              rows={2}
              value={moduleForm.description}
              onChange={(event) => setModuleForm({ ...moduleForm, description: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Name</span>
            <input
              value={moduleForm.displayName}
              onChange={(event) => setModuleForm({ ...moduleForm, displayName: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Commercial mode</span>
            <select
              value={moduleForm.commercialMode}
              onChange={(event) => setModuleForm({ ...moduleForm, commercialMode: event.target.value })}
              disabled={isBusy || editingModuleCode !== ""}
            >
              <option value="IncludedForAll">Included for all</option>
              <option value="PaidAddOn">Paid add-on</option>
            </select>
          </label>
          <label className="checkbox-field product-catalog-active-field">
            <input
              type="checkbox"
              checked={moduleForm.isActive}
              onChange={(event) => setModuleForm({ ...moduleForm, isActive: event.target.checked })}
              disabled={isBusy}
            />
            <span>Active</span>
          </label>
          <label className="form-field">
            <span>Minimum SafarSuite</span>
            <input
              value={moduleForm.minimumSafarSuiteVersion}
              onChange={(event) => setModuleForm({ ...moduleForm, minimumSafarSuiteVersion: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Minimum local server</span>
            <input
              value={moduleForm.minimumLocalServerVersion}
              onChange={(event) => setModuleForm({ ...moduleForm, minimumLocalServerVersion: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Deployment modes</span>
            <textarea
              className="product-access-textarea"
              rows={2}
              value={moduleForm.supportedDeploymentModes}
              onChange={(event) => setModuleForm({ ...moduleForm, supportedDeploymentModes: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <div className="product-access-form-actions">
            <button
              className="mini-button"
              type="button"
              onClick={() => {
                setModuleForm(emptyModuleForm);
                setEditingModuleCode("");
              }}
            >
              <Plus size={13} />
              New
            </button>
            <button className="mini-button" type="submit" disabled={isBusy}>
              <Save size={13} />
              Save draft
            </button>
          </div>
        </form>

        <form className="product-access-edit-form" onSubmit={handleSaveGroup}>
          <div className="product-access-section-heading">
            <span>Module group</span>
            <strong>{groupForm.groupId.trim() === "" ? "New" : "Edit"}</strong>
          </div>
          <label className="form-field">
            <span>Group id</span>
            <input
              value={groupForm.groupId}
              onChange={(event) => setGroupForm({ ...groupForm, groupId: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Name</span>
            <input
              value={groupForm.displayName}
              onChange={(event) => setGroupForm({ ...groupForm, displayName: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Access</span>
            <select
              value={groupForm.accessKind}
              onChange={(event) => setGroupForm({ ...groupForm, accessKind: event.target.value })}
              disabled={isBusy}
            >
              <option value="Public">Public</option>
              <option value="CoreIncluded">CoreIncluded</option>
              <option value="PaidModule">PaidModule</option>
            </select>
          </label>
          <label className="form-field">
            <span>Modules</span>
            <textarea
              className="product-access-textarea"
              rows={3}
              value={groupForm.moduleCodes}
              onChange={(event) => setGroupForm({ ...groupForm, moduleCodes: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <div className="product-access-form-actions">
            <button className="mini-button" type="button" onClick={() => setGroupForm(emptyGroupForm)}>
              <Plus size={13} />
              New
            </button>
            <button className="mini-button" type="submit" disabled={isBusy}>
              <Save size={13} />
              Save
            </button>
          </div>
        </form>

        <form className="product-access-edit-form" onSubmit={handleSaveResource}>
          <div className="product-access-section-heading">
            <span>Resource</span>
            <strong>{resourceForm.resourceId.trim() === "" ? "New" : "Edit"}</strong>
          </div>
          <label className="form-field">
            <span>Resource id</span>
            <input
              value={resourceForm.resourceId}
              onChange={(event) => setResourceForm({ ...resourceForm, resourceId: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Name</span>
            <input
              value={resourceForm.displayName}
              onChange={(event) => setResourceForm({ ...resourceForm, displayName: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Access</span>
            <select
              value={resourceForm.accessKind}
              onChange={(event) => setResourceForm({ ...resourceForm, accessKind: event.target.value })}
              disabled={isBusy}
            >
              <option value="Public">Public</option>
              <option value="CoreIncluded">CoreIncluded</option>
              <option value="PaidModule">PaidModule</option>
            </select>
          </label>
          <label className="form-field">
            <span>Groups</span>
            <textarea
              className="product-access-textarea"
              rows={2}
              value={resourceForm.requiredGroupIds}
              onChange={(event) => setResourceForm({ ...resourceForm, requiredGroupIds: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Direct modules</span>
            <textarea
              className="product-access-textarea"
              rows={2}
              value={resourceForm.requiredModuleCodes}
              onChange={(event) => setResourceForm({ ...resourceForm, requiredModuleCodes: event.target.value })}
              disabled={isBusy}
            />
          </label>
          <div className="product-access-form-actions">
            <button className="mini-button" type="button" onClick={() => setResourceForm(emptyResourceForm)}>
              <Plus size={13} />
              New
            </button>
            <button className="mini-button" type="submit" disabled={isBusy}>
              <Save size={13} />
              Save
            </button>
          </div>
        </form>
      </div>

      <div className="product-access-section-heading product-access-delivery-heading">
        <span>Server delivery</span>
        <strong>Published revision only</strong>
      </div>
      <form className="product-access-publish-form" onSubmit={handlePublish}>
        <label className="form-field">
          <span>Activation request id</span>
          <input
            value={value.activationRequestId}
            onChange={(event) => onChange({ ...value, activationRequestId: event.target.value })}
            disabled={isBusy}
          />
        </label>
        <label className="form-field">
          <span>Expiry hours</span>
          <input
            type="number"
            min="1"
            max="168"
            value={value.expiresInHours}
            onChange={(event) => onChange({ ...value, expiresInHours: event.target.value })}
            disabled={isBusy}
          />
        </label>
        <label className="form-field">
          <span>Requested by</span>
          <input
            value={value.requestedBy}
            onChange={(event) => onChange({ ...value, requestedBy: event.target.value })}
            disabled={isBusy}
          />
        </label>
        <button
          className="icon-button primary product-access-publish-button"
          type="submit"
          disabled={isBusy}
          title="Send published catalog to server"
        >
          <Send size={16} />
          Send to server
        </button>
      </form>

      {catalog === null ? (
        <div className="client-empty-state product-access-empty-state">
          <Package size={18} />
          <span>Catalog not loaded</span>
        </div>
      ) : (
        <div className="product-access-layout">
          <section className="product-access-list-panel" aria-label="Commercial modules">
            <div className="product-access-section-heading">
              <span>Commercial modules</span>
              <strong>{modules.length}</strong>
            </div>
            <div className="product-access-list">
              {modules.map((module) => (
                <ProductModuleItem
                  module={module}
                  key={module.moduleCode}
                  isBusy={isBusy}
                  onEdit={() => {
                    setModuleForm(toModuleForm(module));
                    setEditingModuleCode(module.moduleCode);
                  }}
                  onStatusChange={() => handleSetModuleStatus(module.moduleCode, !module.isActive)}
                />
              ))}
            </div>
          </section>

          <section className="product-access-list-panel" aria-label="Module groups">
            <div className="product-access-section-heading">
              <span>Module groups</span>
              <strong>{groups.length}</strong>
            </div>
            <div className="product-access-list">
              {groups.map((group) => (
                <ProductModuleGroupItem
                  group={group}
                  key={group.groupId}
                  isBusy={isBusy}
                  onEdit={() => setGroupForm(toGroupForm(group))}
                  onRemove={() => handleRemoveGroup(group.groupId)}
                />
              ))}
            </div>
          </section>

          <section className="product-access-list-panel" aria-label="Resources">
            <div className="product-access-section-heading">
              <span>Resources</span>
              <strong>{resources.length}</strong>
            </div>
            <div className="product-access-list">
              {resources.map((resource) => (
                <ProductResourceItem
                  resource={resource}
                  key={resource.resourceId}
                  isBusy={isBusy}
                  onEdit={() => setResourceForm(toResourceForm(resource))}
                  onRemove={() => handleRemoveResource(resource.resourceId)}
                />
              ))}
            </div>
          </section>
        </div>
      )}

      <section className="product-catalog-history" aria-label="Published catalog history">
        <div className="product-access-section-heading">
          <span>Published history</span>
          <strong>{revisions.length}</strong>
        </div>
        {revisions.length === 0 ? (
          <div className="client-empty-state product-access-empty-state">
            <History size={18} />
            <span>No published revisions</span>
          </div>
        ) : (
          <div className="product-catalog-history-list">
            {revisions.map((revision) => (
              <article
                className="product-catalog-history-item"
                key={revision.catalogRevisionId ?? `revision-${revision.revisionNumber ?? "unknown"}`}
              >
                <GitBranch size={15} />
                <span>
                  <strong>Revision #{revision.revisionNumber ?? "-"}</strong>
                  <small>{revision.changeReason || "No change reason recorded"}</small>
                </span>
                <em>{revision.changedAtUtc === null ? "-" : formatDateTime(revision.changedAtUtc)}</em>
              </article>
            ))}
          </div>
        )}
      </section>

      {publishedCommand !== null && (
        <section className="product-access-command-result">
          <div className="product-access-section-heading">
            <span>Last command</span>
            <strong>{publishedCommand.commandType}</strong>
          </div>
          <dl className="product-access-command-facts">
            <div>
              <dt>Command id</dt>
              <dd>{publishedCommand.commandId}</dd>
            </div>
            <div>
              <dt>Server installation</dt>
              <dd>{publishedCommand.serverInstallationId}</dd>
            </div>
            <div>
              <dt>Signing key</dt>
              <dd>{publishedCommand.signingKeyId}</dd>
            </div>
            <div>
              <dt>Expires</dt>
              <dd>{formatDateTime(publishedCommand.expiresAt)}</dd>
            </div>
          </dl>
          <label className="form-field product-access-command-field">
            <span>Product kernel command</span>
            <textarea rows={3} readOnly value={publishedCommand.productKernelCommand} />
          </label>
          <label className="form-field product-access-command-field">
            <span>Signature</span>
            <textarea rows={2} readOnly value={publishedCommand.signature} />
          </label>
        </section>
      )}
    </section>
  );
}

function ProductAccessSummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="product-access-summary-card">
      <CheckCircle2 size={17} />
      <span>
        <strong>{value}</strong>
        <small>{label}</small>
      </span>
    </article>
  );
}

function ProductModuleItem({
  module,
  isBusy,
  onEdit,
  onStatusChange
}: {
  module: ProductModule;
  isBusy: boolean;
  onEdit: () => void;
  onStatusChange: () => void;
}) {
  const compatibility = module.compatibility ?? {
    minimumSafarSuiteVersion: null,
    minimumLocalServerVersion: null,
    supportedDeploymentModes: []
  };

  return (
    <article className="product-access-item">
      <header>
        <span>
          <strong>{module.displayName}</strong>
          <small>{module.moduleCode}</small>
        </span>
        <em className={`product-access-kind ${module.isActive ? "coreincluded" : "public"}`}>
          {module.isActive ? "Active" : "Inactive"}
        </em>
      </header>
      <dl className="product-access-resource-facts">
        <div>
          <dt>Commercial</dt>
          <dd>{formatAccessKind(module.commercialMode)}</dd>
        </div>
        <div>
          <dt>SafarSuite</dt>
          <dd>{compatibility.minimumSafarSuiteVersion || "Any"}</dd>
        </div>
        <div>
          <dt>Local server</dt>
          <dd>{compatibility.minimumLocalServerVersion || "Any"}</dd>
        </div>
        <div>
          <dt>Deployments</dt>
          <dd>{joinValues(compatibility.supportedDeploymentModes)}</dd>
        </div>
      </dl>
      {module.billingDefaults !== null && module.billingDefaults !== undefined && (
        <p>
          {module.billingDefaults.chargeCode} / {module.billingDefaults.currencyCode}{" "}
          {module.billingDefaults.defaultUnitPriceAmount.toLocaleString()}
        </p>
      )}
      <div className="product-access-item-actions">
        <button className="mini-button" type="button" disabled={isBusy} onClick={onEdit}>
          <Edit3 size={13} />
          Edit
        </button>
        <button className="mini-button" type="button" disabled={isBusy} onClick={onStatusChange}>
          {module.isActive ? <PauseCircle size={13} /> : <PlayCircle size={13} />}
          {module.isActive ? "Deactivate" : "Activate"}
        </button>
      </div>
    </article>
  );
}

function ProductModuleGroupItem({
  group,
  isBusy,
  onEdit,
  onRemove
}: {
  group: ProductModuleGroup;
  isBusy: boolean;
  onEdit: () => void;
  onRemove: () => void;
}) {
  return (
    <article className="product-access-item">
      <header>
        <span>
          <strong>{group.displayName}</strong>
          <small>{group.groupId}</small>
        </span>
        <em className={`product-access-kind ${accessKindClass(group.accessKind)}`}>
          {formatAccessKind(group.accessKind)}
        </em>
      </header>
      <p>{joinValues(group.moduleCodes)}</p>
      <div className="product-access-item-actions">
        <button className="mini-button" type="button" disabled={isBusy} onClick={onEdit}>
          <Edit3 size={13} />
          Edit
        </button>
        <button className="mini-button" type="button" disabled={isBusy} onClick={onRemove}>
          <Trash2 size={13} />
          Remove
        </button>
      </div>
    </article>
  );
}

function ProductResourceItem({
  resource,
  isBusy,
  onEdit,
  onRemove
}: {
  resource: ProductResource;
  isBusy: boolean;
  onEdit: () => void;
  onRemove: () => void;
}) {
  return (
    <article className="product-access-item">
      <header>
        <span>
          <strong>{resource.displayName}</strong>
          <small>{resource.resourceId}</small>
        </span>
        <em className={`product-access-kind ${accessKindClass(resource.accessKind)}`}>
          {formatAccessKind(resource.accessKind)}
        </em>
      </header>
      <dl className="product-access-resource-facts">
        <div>
          <dt>Groups</dt>
          <dd>{joinValues(resource.requiredGroupIds)}</dd>
        </div>
        <div>
          <dt>Direct modules</dt>
          <dd>{joinValues(resource.requiredModuleCodes)}</dd>
        </div>
        <div>
          <dt>Resolved modules</dt>
          <dd>{joinValues(resource.resolvedModuleCodes)}</dd>
        </div>
      </dl>
      <div className="product-access-item-actions">
        <button className="mini-button" type="button" disabled={isBusy} onClick={onEdit}>
          <Edit3 size={13} />
          Edit
        </button>
        <button className="mini-button" type="button" disabled={isBusy} onClick={onRemove}>
          <Trash2 size={13} />
          Remove
        </button>
      </div>
    </article>
  );
}

function toGroupForm(group: ProductModuleGroup): ProductModuleGroupFormInput {
  return {
    groupId: group.groupId,
    displayName: group.displayName,
    accessKind: group.accessKind,
    moduleCodes: group.moduleCodes.join(", ")
  };
}

function toModuleForm(module: ProductModule): ProductModuleFormInput {
  const compatibility = module.compatibility ?? {
    minimumSafarSuiteVersion: null,
    minimumLocalServerVersion: null,
    supportedDeploymentModes: []
  };

  return {
    moduleCode: module.moduleCode,
    displayName: module.displayName,
    description: module.description,
    commercialMode: module.commercialMode,
    isActive: module.isActive,
    minimumSafarSuiteVersion: compatibility.minimumSafarSuiteVersion ?? "",
    minimumLocalServerVersion: compatibility.minimumLocalServerVersion ?? "",
    supportedDeploymentModes: compatibility.supportedDeploymentModes.join(", ")
  };
}

function toResourceForm(resource: ProductResource): ProductResourceFormInput {
  return {
    resourceId: resource.resourceId,
    displayName: resource.displayName,
    accessKind: resource.accessKind,
    requiredGroupIds: resource.requiredGroupIds.join(", "),
    requiredModuleCodes: resource.requiredModuleCodes.join(", ")
  };
}

function requestedBy(value: PublishProductAccessCatalogCommandInput): string {
  const normalized = value.requestedBy.trim();

  return normalized === "" ? "Control Desk" : normalized;
}

function withChangeReason(
  catalog: ProductAccessCatalog,
  changeReason: string
): ProductAccessCatalog {
  return {
    ...catalog,
    changeReason: changeReason.trim()
  };
}

function nullIfEmpty(value: string): string | null {
  const normalized = value.trim();
  return normalized === "" ? null : normalized;
}

function catalogRevisionLabel(catalog: ProductAccessCatalog): string {
  if (catalog.state.toLowerCase() === "draft") {
    return `Draft from revision #${catalog.baseCatalogRevisionNumber ?? "-"}`;
  }

  return `Revision #${catalog.revisionNumber ?? "-"}`;
}

function emptyCatalog(): ProductAccessCatalog {
  return {
    state: "Published",
    catalogRevisionId: null,
    revisionNumber: null,
    supersedesCatalogRevisionId: null,
    draftId: null,
    baseCatalogRevisionId: null,
    baseCatalogRevisionNumber: null,
    changeReason: "",
    changedBy: "",
    changedAtUtc: null,
    modules: [],
    moduleGroups: [],
    resources: []
  };
}

function upsertById<T>(
  items: T[],
  nextItem: T,
  getId: (item: T) => string,
  nextId: string
): T[] {
  const exists = items.some((item) => equalsId(getId(item), nextId));

  if (!exists) {
    return [...items, nextItem];
  }

  return items.map((item) => equalsId(getId(item), nextId) ? nextItem : item);
}

function splitValues(value: string): string[] {
  const seen = new Set<string>();

  return value
    .split(/[\n,]/)
    .map((item) => item.trim())
    .filter((item) => {
      if (item === "" || seen.has(item.toLowerCase())) {
        return false;
      }

      seen.add(item.toLowerCase());
      return true;
    });
}

function equalsId(left: string, right: string): boolean {
  return left.trim().toLowerCase() === right.trim().toLowerCase();
}

function joinValues(values: string[]): string {
  return values.length === 0 ? "-" : values.join(", ");
}

function accessKindClass(accessKind: ProductAccessKind): string {
  return accessKind.replace(/[^a-z0-9]/gi, "").toLowerCase();
}

function formatAccessKind(accessKind: ProductAccessKind): string {
  return accessKind.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/_/g, " ");
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

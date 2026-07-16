# Product Module Catalog Boundary

Date added: 2026-07-02

Revised: 2026-07-13

Canonical authority: `docs/architecture/product-charter-2026-07-11.md`

## Decision

The Office Control System owns one versioned product definition. A published revision contains both:

- commercial module definitions used by contracts, billing setup, and entitlements
- SafarSuite runtime access groups and protected-resource mappings used by product-kernel delivery

These are related parts of one product definition, but their code spaces are not assumed to be identical. A commercial module such as `PAYROLL` and a runtime module such as `accounting` keep their existing meaning. Publishing them together supplies one provenance boundary without inventing an unsafe one-to-one mapping.

## Revision Model

```text
ProductCatalogDraft (mutable singleton)
  -> publish approval
ProductCatalogRevision (append-only global history)
  -> ClientContract revision
  -> ClientAccessRevision
  -> EntitlementSnapshotIssued event
  -> Control Cloud signed bundle
  -> SafarSuite Server verified cache
```

A published revision records:

- globally unique revision ID and monotonically increasing number
- predecessor revision ID
- commercial modules and lifecycle state
- operator-facing module names and descriptions
- minimum SafarSuite and LocalServer versions plus supported deployment modes
- optional billing defaults
- runtime module groups and protected resources
- publication actor, timestamp, and reason

Saving edits changes only the draft. Contracts and server delivery continue to use the latest published revision until an operator explicitly publishes the draft. Published rows are protected by both application behavior and a PostgreSQL update/delete trigger.

## Contract Binding

Every contract revision stores both `ProductCatalogRevisionId` and `ProductCatalogRevisionNumber`. PostgreSQL uses the pair as a composite foreign key, so a valid ID cannot be stored beside the wrong number.

Module selection is evaluated against that published revision. The resulting allowances and catalog identity are stored together on the immutable contract. A later catalog publication never reinterprets an older contract.

Every approved client access revision copies the exact catalog pair from its paid invoice's contract. Event version 6 and signed bundle version 5 carry that provenance, the approved effective instant, user allowances, and feature limits through Control Cloud bundle audit and installation status into the SafarSuite Server cache.

## Feature-Limit Boundary

The product catalog defines which commercial modules exist and can be selected. A feature-limit row is a client-specific quantity under one enabled module, identified by `(module code, feature code)` and carrying a nonnegative integer value plus a unit.

The module code must be enabled in the same immutable contract or access revision. Feature codes are currently stable module-owned identifiers rather than another global catalog. This avoids inventing a second product-definition hierarchy before real feature semantics are known. If feature metadata later needs lifecycle, compatibility, display names, or billing defaults, it should be added to the same versioned product catalog and existing revisions must retain their original interpretation.

## Configuration Boundary

`ProductModules` configuration is bootstrap input only. It creates revision 1 when no published catalog exists in an in-memory environment. PostgreSQL migration creates the same baseline revision and retains any module codes already present on historical contracts.

Once revision 1 exists, changing `appsettings` does not change Office truth. Product changes must use draft and publish operations.

The current seed entries remain temporary:

- `CONTROL_DESK` - included for all
- `PAYROLL` - paid add-on
- `TOUR` - paid add-on

The versioning model is accepted; the initial real commercial lineup remains a product decision.

## API Boundary

| Endpoint | Meaning |
| --- | --- |
| `GET /api/v1/contracts/product-modules` | Latest published commercial modules and catalog revision |
| `GET /api/v1/contracts/product-access-catalog` | Current draft when present, otherwise latest published revision |
| `PUT /api/v1/contracts/product-access-catalog` | Save or replace the mutable draft only |
| `POST /api/v1/contracts/product-access-catalog/publish-revision` | Publish the draft as the next immutable revision |
| `GET /api/v1/contracts/product-access-catalog/revisions` | Read append-only published history |
| `POST /api/v1/contracts/product-access-catalog/product-kernel-command` | Send the latest published runtime access definition to one server activation request |

Catalog publication and server delivery are separate actions. A published Office decision can be delivered repeatedly without creating another product revision.

## Billing And Compatibility

Billing defaults remain setup suggestions, not posting shortcuts. A contract or charge rule stores its approved commercial values independently while retaining the module code and catalog provenance that informed the decision.

Compatibility metadata describes which already-deployed SafarSuite and LocalServer versions can expose a module. Entitlement enables compatible executable capability; it does not deploy code.

## Guardrails

- At least one module definition is required in every draft and published revision.
- Contract creation accepts only active commercial modules from the selected published revision.
- Active `IncludedForAll` modules are auto-enabled.
- A commercial module code and commercial type are immutable after the module enters a draft; retire a module by deactivating it rather than deleting or renaming it.
- Catalog administration may show current active-contract references, but those references do not rewrite the immutable contract or catalog revision that created them.
- Published revision number and predecessor lineage are unique and linear.
- Draft publication fails if its base revision is stale.
- Legacy migration records unknown history explicitly and retains discovered contract module codes.
- Runtime resource mappings are resolved and validated before draft persistence.
- Contracts and entitlement artifacts remain immutable after approval or issue.

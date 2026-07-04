# SafarSuite Product Access Catalog Requirements

Date: 2026-07-04

Purpose: define the required owner-controlled behavior for product packaging, module groups, and resource access before the app and Control Desk drift into hardcoded module checks.

## Product Owner Requirement

The Product Owner must be able to manage product packaging dynamically:

- create a module group such as `Foundation Core`, `Accounting Ledger`, `Reporting`, `Travel`, `Tour`, `Payroll`, or a future package name
- add or remove modules from a group without changing endpoint guard code
- mark a group as `Public`, `CoreIncluded`, or `PaidModule`
- create another group later and decide whether it is core, paid, disabled, or otherwise controlled by entitlement policy
- map app resources to groups, not directly to one-off hardcoded module checks

Examples:

| Group | Access kind | Example modules |
| --- | --- | --- |
| Foundation Core | `CoreIncluded` | `platform`, `identity-access`, `tenant-branch`, `module-registry`, `entitlements`, `notifications`, `audit` |
| Reporting | `PaidModule` | `reporting-core` |
| Accounting Ledger | `PaidModule` | `accounting` |
| Travel | `PaidModule` | `travel`, `ticket-stock` |
| Payroll | `PaidModule` | `payroll`, `payroll-reports` |

## Required Access Model

Use four separate concepts:

| Concept | Meaning |
| --- | --- |
| Module | A runnable app capability such as `accounting`, `travel`, `payroll`, or `reporting-core`. |
| Module group | A product package/bundle controlled by the Product Owner. |
| Resource | A protected app action or surface such as `reports.execute`, `reports.catalog`, `accounting.write`, or `payroll.run`. |
| Entitlement | The installation's paid/current access state for modules or groups, evaluated through the local server. |

Endpoints, menus, jobs, and screens should require resources:

```text
RequireResource("reports.execute")
```

The access system then resolves:

```text
reports.execute -> Reporting group -> reporting-core module -> local module gateway decision
```

This is the required behavior. Do not spread direct checks such as `RequireModule("reporting-core")` across app endpoints when a stable resource id can be used instead.

## Access Kinds

Initial access kinds:

| Access kind | Behavior |
| --- | --- |
| `Public` | Available without paid module entitlement. Suitable for local status, discovery, diagnostics, and product-kernel state. |
| `CoreIncluded` | Included with any active installation/package but still controlled by local server activation and entitlement health. |
| `PaidModule` | Requires the relevant module/group entitlement through the local module gateway. |

Future access kinds can be added only when a real product rule needs them. Do not add speculative states before the billing and entitlement behavior is defined.

## App Enforcement Rules

The app must enforce resource access in backend/API entry points and not only in menus.

Required behavior:

- app endpoints call `RequireResource(resourceId)` where possible
- menu/window visibility may use the same resource catalog, but UI hiding is only a convenience
- paid resources fail closed when the gateway denies access or the local server cannot make a trusted decision
- public resources such as product-kernel state and diagnostics remain readable when paid modules are denied
- read-only/restricted access must be explicit per resource; do not silently allow writes in restricted mode
- module gateway date math, expiry, grace, offline validity, replay, and signature rules remain in the local server

## Current App-Side Seeded Catalog

The SafarSuite app workspace has started with a seeded catalog in code. This is an implementation stepping stone, not the final owner UI.

Seeded groups:

| Group id | Access kind | Modules |
| --- | --- | --- |
| `foundation-core` | `CoreIncluded` | `platform`, `identity-access`, `tenant-branch`, `module-registry`, `entitlements`, `notifications`, `audit` |
| `accounting-ledger` | `PaidModule` | `accounting` |
| `reporting` | `PaidModule` | `reporting-core` |
| `clients-parties` | `PaidModule` | `clients-parties` |
| `travel` | `PaidModule` | `travel`, `ticket-stock` |
| `tour` | `PaidModule` | `tour`, `visa`, `hotels`, `transport` |
| `connectivity` | `PaidModule` | `cloud-sync`, `owner-cloud-dashboard`, `cloud-backup`, `cloud-consolidated-reports`, `remote-monitoring` |

Seeded resources:

| Resource id | Access kind | Required group |
| --- | --- | --- |
| `product-kernel.state` | `Public` | none |
| `product-kernel.modules` | `CoreIncluded` | `foundation-core` |
| `reports.catalog` | `PaidModule` | `reporting` |
| `reports.execute` | `PaidModule` | `reporting` |
| `reports.audit` | `PaidModule` | `reporting` |
| `accounting.write` | `PaidModule` | `accounting-ledger` |

## Control Desk Responsibilities

Control Desk should become the source of truth for owner-managed packaging when this moves beyond seeded code.

Required future Control Desk behavior:

- maintain product module groups and group membership
- mark each group as core/included, paid, disabled, or another explicit product access kind
- publish the catalog to the local server/app through signed product-kernel or entitlement commands
- keep module ids stable once they appear in entitlements, billing rules, app resources, or gateway contracts
- support new modules such as Payroll or future Travel/Tour packages by catalog data first, then app implementation

## Control Desk First Read Surface

Implemented first owner-side slice on 2026-07-04:

- `GET /api/v1/contracts/product-access-catalog` exposes module groups and resource mappings from Control Desk.
- `ProductModules:AccessCatalog:ModuleGroups` and `ProductModules:AccessCatalog:Resources` are the config-backed source for the first slice.
- If no groups/resources are configured yet, Control Desk emits defaults that mirror the current app-side seeded catalog.
- Catalog validation fails startup/API resolution for duplicate group ids, duplicate resource ids, unsupported access kinds, unknown group references, and non-public resources that do not resolve to any module code.
- The response uses owner-facing module code fields (`ModuleCodes`, `RequiredModuleCodes`, `ResolvedModuleCodes`) while preserving the same concept as the app-side module ids. A later publisher/importer can translate between owner module codes and app module ids when needed.

## Control Desk Publish Command Surface

Implemented second owner-side slice on 2026-07-04:

- `POST /api/v1/contracts/product-access-catalog/product-kernel-command` builds the current owner catalog and asks the SafarSuite app cloud server to issue a signed `SetProductAccessCatalog` product-kernel command.
- Control Desk uses `SafarSuiteApp:ProductKernelCommands` configuration for the app cloud server base URL, owner API key, and owner actor.
- The publish handler maps owner-facing `ModuleCodes` into the app command payload field `ModuleIds`, preserving group/resource ids and access kinds.
- Publish requests default to a 2-hour command lifetime when `ExpiresInHours` is omitted and reject lifetimes outside 1-168 hours.
- The response returns the signed command (`ProductKernelCommand`, `Signature`, `SigningKeyId`, `CommandId`) plus the catalog snapshot used to issue it.
- The local SafarSuite app still applies the signed command through its existing `/api/product-kernel/vendor-commands` endpoint, keeping verification, replay protection, and catalog validation inside the app local server.

## Control Desk Owner Console Surface

Implemented first owner-facing UI slice on 2026-07-04:

- The Control Desk Contracts workspace now has an `Access catalog` view.
- The view reads `GET /api/v1/contracts/product-access-catalog` and shows module groups, resources, access kinds, and resolved module codes.
- The view can publish the current catalog through `POST /api/v1/contracts/product-access-catalog/product-kernel-command` when an app CloudServer activation request id is supplied.
- The publish result shows command id, server installation id, signing key, expiry, product-kernel command, and signature for review/import workflows.

Implemented first persisted owner-management slice on 2026-07-04:

- `PUT /api/v1/contracts/product-access-catalog` saves the owner-managed catalog groups and resources.
- PostgreSQL stores the active catalog in `control.product_access_catalogs`; the in-memory provider keeps the same behavior for dev/test runs without Postgres.
- `GET /api/v1/contracts/product-access-catalog` and publish now use the saved catalog when present, falling back to config/default catalog only before the owner saves changes.
- The Control Desk `Access catalog` view can add, update, and remove module groups and resources, then save the catalog before publishing it to the app product kernel.
- Saved catalogs keep module-code ownership in Control Desk, so future modules such as Payroll, Travel, Tour, or other paid/core groups can be introduced through catalog data before app-specific module implementation catches up.

Implemented repeatable persistence smoke on 2026-07-05:

- `tools\SafarSuite.ControlDesk.ProductAccessCatalogSmoke` reads the current catalog, saves a temporary owner-managed Payroll group/resource, verifies persisted readback and resolved module codes, then restores the original catalog by default.
- The smoke can optionally publish the current catalog through `POST /api/v1/contracts/product-access-catalog/product-kernel-command` and import the signed command into app LocalServer when supplied an app CloudServer activation request id and LocalServer URL.

## Verification Record

Verified app workspace on 2026-07-04:

```text
C:\Users\Daniyal\Documents\Codex\2026-06-09\hello-there-2
```

Verified behavior:

- product-kernel state exposes module groups and resource mappings
- `/api/product-kernel/access-catalog` exposes the same catalog behind platform module read permission
- reporting endpoints require resources (`reports.catalog`, `reports.execute`, `reports.audit`) instead of hardcoded module access in each endpoint
- disabling `module.reporting-core` blocks reporting catalog, execution, and audit while product-kernel state remains readable
- Modules / Subscription screen displays current module groups and resource mappings

Passed checks:

- `tests/ProductKernelGuardSmoke` passed 35 checks
- `tests/ReportExecutionSmoke` passed 30 checks
- `tests/ReadOnlyEnforcementSmoke` passed 24 checks
- Windows client production build passed with the existing Vite large-chunk warning

Verified Control Desk workspace on 2026-07-04:

```text
C:\Users\Daniyal\Documents\Codex\provider-office-erp
```

Passed check:

- `dotnet build src\SafarSuite.ControlDesk.Api\SafarSuite.ControlDesk.Api.csproj` passed with 0 warnings and 0 errors
- After adding the publish command surface, the same build passed again with 0 warnings and 0 errors

Verified cross-workspace publish path on 2026-07-04:

```text
Control Desk API: http://127.0.0.1:5188
App CloudServer:  http://127.0.0.1:5281
App LocalServer:  http://127.0.0.1:5290
```

Verified behavior:

- app LocalServer created a temporary activation request for the live smoke
- app CloudServer approved that activation and issued a signed activation token
- app LocalServer imported the activation token as `Active`
- Control Desk `POST /api/v1/contracts/product-access-catalog/product-kernel-command` requested an app-cloud-signed `SetProductAccessCatalog` command
- app LocalServer applied the returned signed command through `POST /api/product-kernel/vendor-commands`
- app LocalServer state readback from `GET /api/product-kernel/state` showed 7 catalog groups and 6 resources, including the `reporting` group and `reports.execute` resource

Passed check:

- `tests\ProductKernelGuardSmoke` in the app workspace passed 37 checks, including app CloudServer-issued product access catalog command import

## Do Not Drift

- Do not treat `Reports` as a special one-off guard pattern.
- Do not require code changes for every future package decision when catalog data can express it.
- Do not let a new module such as Payroll bypass the product access catalog.
- Do not duplicate entitlement expiry/grace/offline date math in app screens.
- Do not make Control Desk UI labels the contract; stable ids are the contract.

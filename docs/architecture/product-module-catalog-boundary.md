# Product Module Catalog Boundary

Date added: 2026-07-02

SafarSuite is module-composed. The final module list is not fixed yet, and the product must support both modules that every customer receives and paid add-ons that can be combined per client plan.

## Decision

Do not hard-code the final module lineup inside contracts, billing, entitlements, or the UI.

SafarSuite Control Desk now has a small product module catalog boundary:

| Layer | Responsibility |
| --- | --- |
| Domain | Defines `ProductModuleCatalogItem` and `ProductModuleCommercialMode` |
| Application | Exposes `IProductModuleCatalog` and `ListProductModulesHandler` |
| Infrastructure | Provides a configuration-backed catalog through `ProductModules:Modules` |
| API | Exposes `GET /api/v1/contracts/product-modules` |
| Frontend | Contract setup loads the catalog and shows module checkboxes when active entries exist; if the catalog is empty, the existing manual module-code field remains available |

The catalog can stay empty while the business decides the final lineup. When ready, entries can be added with:

```json
{
  "ProductModules": {
    "Modules": [
      {
        "ModuleCode": "CORE",
        "DisplayName": "Core Desk",
        "CommercialMode": "IncludedForAll",
        "IsActive": true
      },
      {
        "ModuleCode": "TRAVEL",
        "DisplayName": "Travel",
        "CommercialMode": "PaidAddOn",
        "IsActive": true
      }
    ]
  }
}
```

Current development config includes temporary dummy entries:

- `CONTROL_DESK` - Control Desk (Dummy) - `IncludedForAll`
- `PAYROLL` - Payroll (Dummy) - `PaidAddOn`
- `TOUR` - Tour (Dummy) - `PaidAddOn`

## Commercial Modes

| Mode | Meaning |
| --- | --- |
| `IncludedForAll` | Baseline module every valid customer should receive once enabled by policy |
| `PaidAddOn` | Optional module that must be present and enabled on the client contract/entitlement before the deployed app can expose it |

## Billing Defaults

Catalog entries may carry optional billing defaults:

| Field | Purpose |
| --- | --- |
| `ChargeCode` | Suggested billing charge code for the module |
| `ChargeName` | Suggested charge-code name |
| `Description` | Suggested invoice/rule description |
| `DefaultUnitPriceAmount` | Suggested module price |
| `CurrencyCode` | Suggested billing currency |
| `BillingCycle` | Suggested charge-rule cycle |

The billing setup screen uses active paid add-ons from the selected contract to prefill charge-code and charge-rule forms. This is a setup helper, not a posting shortcut; the user still creates the charge code/rule through the existing billing actions.

When a module-backed charge rule is saved, its `ProductModuleCode` is persisted with the rule. Invoice draft generation copies that module code onto the generated charge and tax invoice lines so later reports, portal views, and entitlement/payment audits can explain which billed module produced each amount.

## Guardrail

Contracts and entitlement snapshots still require at least one enabled module. The signed entitlement bundle remains the runtime source of truth for the deployed SafarSuite local server/app.

When the catalog has active entries, contract creation and replacement reject module codes that are not in the active catalog. Active `IncludedForAll` modules are auto-enabled even when the request omits them.

Future slices can add catalog management screens, richer module pricing policies, and publish the selected module plan to SafarSuite Control Cloud.

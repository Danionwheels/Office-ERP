# SafarSuite App Integration Handoff

Date: 2026-07-03

Purpose: record the exact handoff from the Control Desk/Control Cloud/local-server workspace into the real SafarSuite app workspace, so module enforcement is implemented once, through the local module gateway, without duplicating license rules inside the app.

## Current Readiness

The Control Desk side is ready for a deliberate workspace switch after this handoff is accepted.

Already available in this workspace:

- Control Cloud bootstrap packages with Docker Compose, environment template, runtime service manifest, and optional `safarsuite-app` profile slot
- local-server API host with bootstrap, entitlement pull/import, heartbeat, command processing, diagnostics, and module-gateway endpoints
- signed entitlement bundle import/cache rules with replay, version, signature, and clock-rollback protection
- module access decisions for active, warning, grace, restricted, expired, missing, disabled, and installation mismatch states
- repeatable smoke proof in `tools/SafarSuite.LocalServer.EntitlementSmoke`

Not in this workspace:

- the real SafarSuite app image
- the app's module menus/screens/routes
- the app's own runtime health endpoint and app-specific logs
- the app-side module enforcement middleware/components

## Workspace Boundary

Discovered app workspace:

```text
C:\Users\Daniyal\Documents\Codex\2026-06-09\hello-there-2
```

Codex saved-project label:

```text
hello-there-2
```

This workspace owns the control plane:

- commercial client/accounting source of truth
- Control Cloud setup, bootstrap, entitlement, heartbeat, command, diagnostics, and audit contracts
- local-server module gateway, cache, diagnostics, and runtime templates
- product module catalog and entitlement vocabulary

The SafarSuite app workspace must own the customer app runtime:

- app image build and versioning
- module UI/API enforcement
- customer-facing screens and local business data
- app health endpoint and app logs
- app-side route/menu/module-code mapping

Do not implement SafarSuite app screens in this workspace. Do not duplicate entitlement date math in the SafarSuite app workspace.

## Runtime Contract

The app container should accept these environment variables from the generated local-server bootstrap package:

| Variable | Required | Notes |
| --- | --- | --- |
| `SAFARSUITE_APP_VERSION` | Yes | Selected app/runtime version |
| `SAFARSUITE_APP_IMAGE` | Yes | Full container image reference used by Compose |
| `SAFARSUITE_APP_HTTP_PORT` | Yes | Host-exposed app port; container should document its internal port |
| `SAFARSUITE_LOCAL_API_BASE_URL` | Yes | Internal local-server API URL, default `http://local-api:8080` |
| `SAFARSUITE_MODULE_GATEWAY_URL` | Yes | Internal module-gateway base URL, default `http://local-api:8080` |
| `SAFARSUITE_RUNTIME_MANIFEST_PATH` | Yes | Path to deployed `runtime-services.manifest.json` |
| `SAFARSUITE_CLIENT_DEPLOYMENT_MODE` | Yes | `OfflineLocal`, `BranchToHqSync`, `CloudSyncMultiBranch`, or `HostedSaas` |
| `SAFARSUITE_SITE_ID` | Yes | Stable site/runtime identity |
| `SAFARSUITE_SITE_ROLE` | Yes | `Standalone`, `Hq`, `Branch`, or `Hosted` |

The app image should expose `GET /health` and emit logs that make module-denial and local-server connectivity failures visible to local-server diagnostics.

## Module Gateway Contract

Shared contract source:

```text
src/SafarSuite.ControlDesk.Contracts/ControlCloud/V1/LocalServerModuleGatewayContracts.cs
```

Response format version:

```text
safarsuite-local-module-gateway-v1
```

App-facing endpoints:

```text
GET  /api/v1/local-server/modules/{moduleCode}/access?asOfDate=YYYY-MM-DD&requestedBy=safarsuite-app
POST /api/v1/local-server/modules/access
```

Prefer the `GET` endpoint for app screens/routes after bootstrap is imported because the local server reads the registered installation id from its persisted bootstrap configuration. Use `POST` when the caller explicitly owns the installation id and needs to pass it.

Request body for `POST`:

```json
{
  "installationId": "office-main",
  "moduleCode": "Accounting",
  "asOfDate": "2026-09-15",
  "requestedBy": "safarsuite-app"
}
```

Response:

```json
{
  "formatVersion": "safarsuite-local-module-gateway-v1",
  "installationId": "office-main",
  "moduleCode": "Accounting",
  "isAllowed": true,
  "accessState": "Active",
  "reason": "Entitlement is active.",
  "entitlementVersion": 101,
  "paidUntil": "2026-09-30",
  "warningStartsAt": "2026-09-23",
  "graceUntil": "2026-10-07",
  "offlineValidUntil": "2026-10-14",
  "checkedAtUtc": "2026-08-01T10:00:01+00:00"
}
```

## App Behavior Rules

The SafarSuite app must treat `isAllowed` as authoritative.

Allowed states:

- `Active`: allow normal module use
- `Warning`: allow module use and show renewal warning
- `Grace`: allow module use and show stronger renewal/grace warning

Denied or restricted states:

- `Restricted`: block write-heavy or full module use according to the restricted-mode screen
- `Expired`: block module use and guide renewal
- `ModuleDisabled`: hide or lock the module because the client did not pay for it
- `Missing`: local server has no usable entitlement cache
- `NotYetValid`: entitlement is not valid yet
- `InstallationMismatch`: cached entitlement belongs to another installation
- `StatusInactive`: cloud-issued entitlement is not active

If the local-server module gateway is unreachable, the app should fail closed for paid module entry after a small retry. The app may still show a local status/diagnostics page, but it must not silently unlock paid modules.

The app should not calculate warning, grace, expiry, offline validity, replay, or signature rules. Those stay in the local server.

## Initial Module-Code Proof

The current smoke-proven examples are:

| Module code | Expected proof behavior |
| --- | --- |
| `Accounting` | allowed while active/warning/grace and denied after expiry |
| `Reports` | denied with `ModuleDisabled` when not enabled in the entitlement |

Future real app module names such as Travel, Tour, Payroll (Dummy), and other add-ons must be mapped from the app menu/routes to the product module catalog before they are enforced. The module list is intentionally flexible; final modules are not fixed yet.

## App Workspace Checklist

When we open the SafarSuite app workspace:

1. Find the real app's module menu, route, and backend endpoint boundaries.
2. Create a module-code map that aligns app modules to Control Desk product-module catalog codes.
3. Add a small module-gateway client using `SAFARSUITE_MODULE_GATEWAY_URL`.
4. Gate module navigation and backend entry points with the gateway result.
5. Add denied-state UI for `Expired`, `Restricted`, `ModuleDisabled`, and local-server-unreachable cases.
6. Add renewal warning/grace banners for `Warning` and `Grace`.
7. Add `GET /health` if missing.
8. Emit structured logs for module checks, denials, and local-server connectivity failures.
9. Build/publish the real SafarSuite app image or a controlled placeholder image.
10. Return to this workspace to update bootstrap defaults for app image, version, port, diagnostics, and any new module catalog codes.

## First App-Side Slice

The SafarSuite app workspace already has a Tauri/React Windows client, a local server, a product-kernel endpoint, module registry, entitlement snapshot, menu access policy, and module/subscription screen. The first integration should bridge those existing pieces to the Control Desk module-gateway v1 contract instead of replacing them.

Recommended first files to touch in the app workspace:

| File | Purpose |
| --- | --- |
| `src/Shared/Contracts/ProductKernelContracts.cs` or a new shared contract file | Add `LocalServerModuleGatewayFormat`, `LocalServerModuleAccessRequest`, and `LocalServerModuleAccessResponse` matching this workspace's contract |
| `src/LocalServer/Modules/Platform/Api/PlatformEndpoints.cs` | Expose `GET /api/v1/local-server/modules/{moduleCode}/access` and `POST /api/v1/local-server/modules/access` |
| `src/LocalServer/Modules/Platform/ModuleRegistry/ModuleRegistry.cs` | Reuse existing module definitions and module-id lookup for module-code mapping |
| `src/LocalServer/Modules/Platform/Entitlements/EntitlementSnapshotStore.cs` | Reuse existing entitlement snapshot/status/writes logic without duplicating date math in the client |
| `apps/windows-client/src/api/localApi.ts` | Add typed local module-gateway fetch functions |
| `apps/windows-client/src/app/accessPolicy.ts` | Let existing menu/window guards honor the module-gateway decision when present |
| `apps/windows-client/src/modules/platform/screens/ModulesSubscriptionScreen.tsx` | Show gateway access state beside current module/subscription state |
| `tests/ProductKernelGuardSmoke/Program.cs` | Add smoke assertions for the v1 module-gateway endpoint |

First implementation behavior:

- keep the existing `PRODUCT_MODULES` ids for app code such as `accounting`, `travel`, `tour`, and `reporting-core`
- map gateway module codes such as `Accounting`, `Travel`, `Tour`, and `Reports` onto those existing ids
- return the v1 response shape from the local server
- keep `Active`, `Warning`, and `Grace` usable
- deny disabled/missing/expired modules through the same menu/window access policy already used by the Windows client
- keep backend write guards in place through the existing local write policy

## Do Not Drift

- Do not bypass the local module gateway for paid add-ons.
- Do not let menu visibility be the only protection; backend/module entry points need enforcement too.
- Do not put operational branch/HQ/cloud business-data sync into the Control Cloud billing/license channel.
- Do not make the app depend on Control Cloud directly for runtime access; app access should go through local server.
- Do not rename module codes casually once they have appeared in contracts, billing rules, entitlement bundles, or app gates.

## Local Proof Command

Before and after app workspace changes, run the local-server entitlement smoke from this workspace:

```powershell
dotnet run --project tools\SafarSuite.LocalServer.EntitlementSmoke\SafarSuite.LocalServer.EntitlementSmoke.csproj --no-build
```

Expected proof fields include:

```json
{
  "status": "Passed",
  "moduleGatewayAccountingAllowed": true,
  "moduleGatewayReportsState": "ModuleDisabled",
  "moduleGatewayExpiredState": "Expired"
}
```

## Workspace Switch Gate

Switch to the SafarSuite app workspace only when:

- this handoff is accepted
- the local-server entitlement smoke passes
- the app workspace path is known
- we are ready to change app menu/routes/API behavior rather than Control Desk scaffolding

Return to this workspace after the first app-side gate is working so the bootstrap/runtime tracker can be updated with the real image, health route, and module-code map.

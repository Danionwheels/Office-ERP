# SafarSuite Milestone Stabilization Runbook

Date started: 2026-07-05

Purpose: freeze the current platform spine into a repeatable checkpoint before adding the next product feature slice.

## Stabilization Goal

Make the current SafarSuite control path boringly repeatable:

- Control Desk can own and persist product access catalog groups/resources.
- Control Desk can publish catalog decisions to the real SafarSuite app CloudServer.
- The app LocalServer can apply signed product-kernel commands and enforce module-gateway decisions.
- Runtime packaging exposes health/profile/log context that Control-side diagnostics can trust.
- The worktree is clean enough to commit without editor caches, temporary artifacts, or unrelated UI work mixed into the platform spine.

## Current Confidence

Solid:

- Control Desk API health is responding on `http://localhost:5188`.
- Product access catalog persistence smoke exists at `tools/SafarSuite.ControlDesk.ProductAccessCatalogSmoke`.
- App LocalServer runtime/profile/module-gateway verification has passed through `ProductKernelGuardSmoke`, `ReadOnlyEnforcementSmoke`, `ReportExecutionSmoke`, Compose config, and runtime health/profile probes.
- Cross-workspace catalog publish has been proven with Control Desk publishing a signed `SetProductAccessCatalog` command and app LocalServer applying it.
- Docker runtime packaging, local migrations, GHCR image tag/push proof, anonymous pull proof, and runtime env-template contract are recorded.

Needs stabilization before commit:

- App repo contains a tracked `.vs` design-time cache modification; this should not be part of the release checkpoint.
- Control Desk repo currently includes product-catalog smoke changes and a broader client-register UI redesign. Keep those review lanes separate.
- app CloudServer (`5281`) and app LocalServer live smoke (`5290`) are not currently running; the runtime container LocalServer (`5280`) is healthy.
- The golden path needs one final scripted/manual pass after any tree cleanup.

## Keep In Platform Spine Checkpoint

App workspace:

- Local module-gateway v1 contracts and endpoints.
- Backend `LocalWritePolicy` and read-side report gateway enforcement.
- Product access catalog persistence in app ProductKernel.
- Runtime profile/health contracts and structured gateway log context.
- Dockerfile, Compose runtime wrapper, env template, image build/tag scripts, and runtime docs.
- Product/kernel/report/runtime smoke coverage.

Control Desk workspace:

- Product access catalog read/save/publish backend and persistence.
- Owner catalog UI slice if it is already part of the catalog-management work.
- `SafarSuite.ControlDesk.ProductAccessCatalogSmoke`.
- Project tracker and product access catalog requirements updates.

Keep separate until intentionally accepted:

- Control Desk client-register/profile UI redesign files.
- Any Visual Studio `.vs` cache changes.
- Generated build artifacts under `artifacts`, `bin`, `obj`, or frontend `dist` unless explicitly required.

## Golden Verification Checklist

Run from the app workspace:

```powershell
dotnet build --no-restore src\LocalServer\LocalServer.csproj
dotnet run --project tests\ProductKernelGuardSmoke\ProductKernelGuardSmoke.csproj
dotnet run --project tests\ReadOnlyEnforcementSmoke\ReadOnlyEnforcementSmoke.csproj
dotnet run --project tests\ReportExecutionSmoke\ReportExecutionSmoke.csproj
docker compose -f docker-compose.runtime.yml config
git diff --check
```

Run from the Control Desk workspace:

```powershell
dotnet build --no-restore src\SafarSuite.ControlDesk.Api\SafarSuite.ControlDesk.Api.csproj
npm run build --prefix apps\control-desk-ui
dotnet run --no-build --project tools\SafarSuite.ControlDesk.ProductAccessCatalogSmoke\SafarSuite.ControlDesk.ProductAccessCatalogSmoke.csproj
git diff --check
```

Live golden path, when all three services are running:

1. `GET http://localhost:5188/health` returns healthy Control Desk API.
2. `GET http://localhost:5281/health` returns healthy app CloudServer.
3. `GET http://localhost:5290/health` returns healthy app LocalServer smoke instance.
4. Create/approve an app activation request.
5. Import activation token into app LocalServer.
6. Save/publish Control Desk product access catalog.
7. Import signed `SetProductAccessCatalog` command into app LocalServer.
8. `GET /api/product-kernel/state` shows expected catalog groups/resources.
9. `GET /api/v1/local-server/modules/Reports/access?asOfDate=YYYY-MM-DD&requestedBy=safarsuite-app` returns `Active` and `isAllowed=true`.
10. `GET /api/v1/local-server/runtime/profile` returns `safarsuite-local-runtime-profile-v1`.

## Exit Criteria

- All golden verification checks pass or failures are documented as unrelated and reproducible.
- Dirty-tree audit clearly separates platform-spine changes, catalog-management changes, UI redesign changes, and local/editor noise.
- A checkpoint note records exact commands, dates, and any service ports used.
- The release checkpoint can be committed without `.vs` cache churn or temporary build output.

## Stabilization Pass 1

Completed on 2026-07-05.

App workspace:

- `dotnet build --no-restore src\LocalServer\LocalServer.csproj` passed.
- `dotnet restore --ignore-failed-sources tests\ProductKernelGuardSmoke\ProductKernelGuardSmoke.csproj` was needed because sandboxed NuGet source access failed even though packages were locally cached.
- `dotnet run --no-restore --project tests\ProductKernelGuardSmoke\ProductKernelGuardSmoke.csproj` passed 40 checks.
- `dotnet run --no-restore --project tests\ReadOnlyEnforcementSmoke\ReadOnlyEnforcementSmoke.csproj` passed 24 checks.
- `dotnet run --no-restore --project tests\ReportExecutionSmoke\ReportExecutionSmoke.csproj` passed 30 checks.
- `docker compose -f docker-compose.runtime.yml config` passed; Docker warned that `C:\Users\Daniyal\.docker\config.json` could not be read in the sandbox.
- `git diff --check` passed.

Control Desk workspace:

- `dotnet build --no-restore src\SafarSuite.ControlDesk.Api\SafarSuite.ControlDesk.Api.csproj -p:OutDir=...\artifacts\codex\stabilization-control-desk-api-build\` passed with 0 warnings and 0 errors.
- The generated alternate build output folder was removed after validation.
- `npm run build` in `apps\control-desk-ui` passed after rerunning with filesystem approval; Vite reported the known large-chunk warning.
- `dotnet run --no-build --project tools\SafarSuite.ControlDesk.ProductAccessCatalogSmoke\SafarSuite.ControlDesk.ProductAccessCatalogSmoke.csproj` passed 4 checks against the live Control Desk API.
- `git diff --check` passed, with only LF-to-CRLF working-copy warnings.

Open cleanup lanes:

- None currently open for the platform-spine stabilization pass. The remaining UI work is tracked separately.

Completed cleanup:

- Restored the tracked app `.vs/SafarSuite/DesignTimeBuild/.dtbcache.v2` cache file and confirmed app `.vs` status is clean.
- Re-ran app `git diff --check`; it passed after the cache cleanup.
- Ran the full live three-service golden path with Control Desk API (`5188`), app CloudServer (`5281`), and app LocalServer smoke instance (`5290`).
- Control Desk published signed `SetProductAccessCatalog` command `44ff965f-b934-4ba1-8677-0f0b59a7cb1f`; app LocalServer applied it, read back 7 groups and 6 resources, returned `Reports` as `Active`/allowed, and exposed `safarsuite-local-runtime-profile-v1`.
- Split the Control Desk operator-register UI work into `docs/planning/control-desk-operator-register-ui-review-2026-07-05.md`; its UI build and targeted `git diff --check` passed.

## Next Product Path After Stabilization

Client-facing subscription/package management:

- connect owner catalog groups to contract package selection
- generate billing rules from selected paid module groups
- issue entitlement snapshots from saved package choices
- show client-facing package status without exposing internal module ids first

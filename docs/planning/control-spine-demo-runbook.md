# Control Spine Demo Runbook

Purpose: make the current Control Desk -> Control Cloud -> Local Server spine repeatable before packaging and enabling the real SafarSuite runtime image.

## Local Ports

| Service | URL |
| --- | --- |
| PostgreSQL | `localhost:54329` |
| SafarSuite Control Desk API | `http://localhost:5188` |
| SafarSuite Control Cloud API | `http://localhost:5127` |
| SafarSuite Local Server API | `http://localhost:51046` |
| Control Desk UI | `http://127.0.0.1:5173` |
| Client Portal preview | `http://localhost:5127/client-portal/index.html` |

Note: Vite may select the next free port, such as `http://127.0.0.1:5174`, if `5173` is already occupied.

## Last Validated

This runbook was executed successfully on 2026-07-03 against the local PostgreSQL services:

- Live accounting smoke published `7` messages to the Control Cloud receiver.
- Installation `codex-demo-20260702212826` registered through bootstrap, pulled an active entitlement, reported heartbeat, processed `request_diagnostics` and `refresh_entitlement`, uploaded diagnostics, and returned active `BILLING` module access.
- The proof exposed and fixed command-signing round-trip issues caused by PostgreSQL `jsonb` payload ordering and timestamp precision.
- The local-server command diagnostics smoke now verifies that command-triggered diagnostics collect `4` runtime services, Docker/Compose availability, live Compose state for local services, recent warning/error log-tail lines, and the optional `safarsuite-app` profile slot with signed manifest intent for image env, host port env, and health URL.
- The app runtime probe self-test now verifies the shared module-gateway v1 contract for an allowed module and a module-disabled response.

The app workspace module-gateway enforcement slice was verified on 2026-07-04 in `C:\Users\Daniyal\Documents\Codex\2026-06-09\hello-there-2`:

- `tests\ProductKernelGuardSmoke` passed 30 checks.
- `tests\ReadOnlyEnforcementSmoke` passed 24 checks.
- `tests\ReportExecutionSmoke` passed 28 checks.
- Windows client production build passed.
- Local-server Release publish passed into `artifacts\codex\localserver-publish`.
- Runtime Compose config validation passed for `docker-compose.runtime.yml`.
- Published LocalServer internal healthcheck passed against `http://127.0.0.1:5299/health`.
- App local-server GET/POST module-gateway routes, Windows client menu/window access, and backend write enforcement all consume the gateway decision.
- Reporting execution/audit consume the gateway decision for `reporting-core`, with explicit read-only `Restricted` access still allowed.
- `Dockerfile.localserver`, `.dockerignore`, `docker-compose.runtime.yml`, and `docker/localserver.env.template` now define the first app runtime image wrapper. Docker build, pushed image `ghcr.io/danionwheels/localserver:0.1.0`, anonymous pull, Compose startup, container `/health`, 39 local migrations, and v1 module-gateway probes passed on 2026-07-04.

## Preflight

Run these checks sequentially. Parallel .NET builds/smokes can hit compiler file locks on Windows.

```powershell
dotnet build --no-restore SafarSuite.ControlDesk.sln
dotnet run --no-restore --project tools/SafarSuite.ControlDesk.AccountingSmoke/SafarSuite.ControlDesk.AccountingSmoke.csproj
dotnet run --no-restore --project tools/SafarSuite.LocalServer.EntitlementSmoke/SafarSuite.LocalServer.EntitlementSmoke.csproj
dotnet run --no-restore --project tools/SafarSuite.AppRuntimeProbe/SafarSuite.AppRuntimeProbe.csproj -- --self-test
```

```powershell
Push-Location apps/control-desk-ui
npm run build
Pop-Location
```

Expected: solution build passes, accounting smoke passes, local-server entitlement smoke reports command processing applied/acknowledged, app runtime probe self-test passes, and the UI build passes.

When validating app-workspace enforcement, run these in `C:\Users\Daniyal\Documents\Codex\2026-06-09\hello-there-2`:

```powershell
dotnet run --no-restore --project tests\ProductKernelGuardSmoke\ProductKernelGuardSmoke.csproj
dotnet run --no-restore --project tests\ReadOnlyEnforcementSmoke\ReadOnlyEnforcementSmoke.csproj
dotnet run --no-restore --project tests\ReportExecutionSmoke\ReportExecutionSmoke.csproj
dotnet publish --no-restore -c Release src\LocalServer\LocalServer.csproj -o artifacts\codex\localserver-publish
docker compose -f docker-compose.runtime.yml config
```

## Database Setup

```powershell
dotnet tool restore
docker compose up -d safarsuite-control-desk-postgres
dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlDesk.Infrastructure --startup-project src/SafarSuite.ControlDesk.Api --context ControlDeskDbContext
dotnet tool run dotnet-ef database update --project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --startup-project src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj --context ControlCloudDbContext
```

Both APIs use the same local PostgreSQL server in development, with Control Cloud data isolated under its own cloud persistence slice.

Use the workspace compose service `safarsuite-control-desk-postgres` on `localhost:54329`. A Docker Desktop compose app still trying to start `safarsuite-postgres` on `55432` is stale for this workspace and can fail before Postgres starts because Windows refuses that host port.

## Start Services

Use separate terminals.

```powershell
dotnet run --project src/SafarSuite.ControlCloud.Api/SafarSuite.ControlCloud.Api.csproj --launch-profile SafarSuite.ControlCloud.Api
```

```powershell
dotnet run --project src/SafarSuite.ControlDesk.Api/SafarSuite.ControlDesk.Api.csproj --launch-profile http
```

```powershell
dotnet run --no-launch-profile --project src/SafarSuite.LocalServer.Api/SafarSuite.LocalServer.Api.csproj -- --urls http://localhost:51046 --environment Development
```

```powershell
Push-Location apps/control-desk-ui
npm run dev
Pop-Location
```

Health checks:

```powershell
Invoke-RestMethod http://localhost:5127/health
Invoke-RestMethod http://localhost:5188/health
Invoke-RestMethod http://localhost:51046/health
```

## Demo Path

1. Open `http://127.0.0.1:5173`.
2. Create or select a client.
3. Add contacts, a support note, and an accounting profile.
4. Create required ledger accounts, charge codes, and client charge rules.
5. Create or replace the active contract with at least one enabled product module.
6. Generate an invoice draft and issue it.
7. Record a payment. For a bank transfer, approve it from the pending review flow.
8. Issue or refresh the entitlement snapshot from the paid invoice. The default dev endpoint can do this directly when you have the paid invoice id:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5188/api/v1/entitlements/snapshots/from-paid-invoice/defaults" -ContentType "application/json" -Body '{"invoiceId":"<paid-invoice-id>"}'
```

9. Publish pending outbox messages to Control Cloud:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5188/api/v1/control-cloud/outbox-messages/publish?batchSize=100"
```

10. In the Control Desk Cloud tab, create/save the installation deployment profile, create a setup token, and generate a bootstrap package/install command.
11. Register or import the bootstrap package into the local-server flow, then pull the latest entitlement and report heartbeat.
12. Queue `request_diagnostics` and `refresh_entitlement` support commands from Control Desk.
13. Process commands from the Local Server API:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:51046/api/v1/local-server/commands/process"
```

14. Refresh Control Desk installation status, audit history, and latest diagnostics.
15. Open the Client Portal preview and verify commercial/license/installation status from the cloud-owned projection.

## Proof Checklist

- Control Desk can maintain the client, contract, billing, payment, entitlement, deployment profile, and support command surfaces.
- Control Desk outbox messages are accepted by Control Cloud, not only marked locally.
- Control Cloud status shows installation identity, latest entitlement, heartbeat, pending command count, and latest acknowledgement summary.
- Local Server can import or pull entitlement, evaluate module access, report heartbeat, upload diagnostics, and acknowledge both low-risk commands.
- Command-triggered diagnostics include runtime/bootstrap/service facts from the signed bootstrap runtime manifest, Docker/Compose availability, live Compose service state, each service's manifest intent, recent warning/error log-tail lines, and the dormant `safarsuite-app` profile slot.
- The placeholder app runtime probe can consume the same local module-gateway v1 response shape that the real SafarSuite app workspace must use.
- The real app workspace smokes prove menu/window access and backend writes fail closed on module-gateway denial.
- The real app workspace smokes prove report execution/audit fail closed on `ModuleDisabled` while read-only report access survives `Restricted`.
- The real app workspace runtime wrapper validates through publish, Compose config, and internal healthcheck before the Docker image is built/pushed.
- Client Portal preview reads cloud-owned commercial/license/installation state.

## Known Limits During Demo

- The SafarSuite app image/profile slot points at the pushed `ghcr.io/danionwheels/localserver:0.1.0` image, and the app now exposes runtime health/profile/log evidence; a production-shaped generated-bootstrap proof with the optional app profile is still pending.
- Portal payment UI and real payment provider callbacks are still pending.
- Provider/admin auth still uses the local provider-key boundary; real roles, MFA, password reset, and production mail retry remain pending.
- The local-server background worker is disabled by default, so manual API calls or smoke tools are still useful during proof runs.

## Exit Criteria For This Runbook

The runbook is strong enough when a fresh dev database can be taken from zero to: paid client, published cloud projection, registered installation, pulled entitlement, heartbeat, diagnostics upload, support command acknowledgement, and visible status in Control Desk plus the portal preview.

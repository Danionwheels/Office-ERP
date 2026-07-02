# SafarSuite Runtime Integration Boundary

This note records how SafarSuite Control Cloud, the SafarSuite local server, and the deployed SafarSuite app/runtime should meet when we move from bootstrap scaffolding to a real client installation.

## Workspace Rule

The real deployed SafarSuite app is not in this workspace.

This workspace owns:

- SafarSuite Control Desk commercial/accounting source of truth
- SafarSuite Control Cloud setup, bootstrap, entitlement, heartbeat, command, diagnostics, and audit contracts
- SafarSuite local-server shared licensing, cache, diagnostics, and deployment templates
- deployment/data-sync vocabulary and boundary decisions in `docs/architecture/client-deployment-and-data-sync-boundary.md`

The SafarSuite app workspace must own:

- the real customer-facing app image
- module UI/runtime enforcement
- module-specific local business data and screens
- real service health endpoints
- any app-specific database migrations or workers

## Current Bootstrap Topology

Control Cloud now signs a bootstrap package that includes:

- `docker-compose.yml`
- `local-server.env.template`
- `runtime-services.manifest.json`

The Compose topology contains:

| Service | Starts by default | Role |
| --- | --- | --- |
| `local-db` | Yes | Local PostgreSQL store for local-server/runtime state |
| `local-api` | Yes | Local entitlement, diagnostics, and module-gateway API |
| `local-worker` | Yes | Background entitlement pull, heartbeat, and command processing |
| `local-agent` | Yes | Host/runtime diagnostics and support command bridge |
| `safarsuite-app` | No, `app-runtime` profile | Customer-facing SafarSuite app runtime |

The `safarsuite-app` service is intentionally present but disabled by default. It becomes active only when the real app image exists and deployment can run:

```bash
docker compose --profile app-runtime --env-file local-server.env up -d
```

## App Runtime Contract

The SafarSuite app image should accept these environment variables:

| Variable | Meaning |
| --- | --- |
| `SAFARSUITE_APP_VERSION` | App image/runtime version selected by Control Cloud bootstrap |
| `SAFARSUITE_APP_IMAGE` | Full container image reference |
| `SAFARSUITE_APP_HTTP_PORT` | Host port for app UI/API exposure |
| `SAFARSUITE_LOCAL_API_BASE_URL` | Internal local-server API URL, default `http://local-api:8080` |
| `SAFARSUITE_MODULE_GATEWAY_URL` | Internal module entitlement/gateway URL, default `http://local-api:8080` |
| `SAFARSUITE_RUNTIME_MANIFEST_PATH` | Path to the signed/deployed runtime service manifest |
| `SAFARSUITE_CLIENT_DEPLOYMENT_MODE` | Client runtime mode such as `OfflineLocal`, `BranchToHqSync`, `CloudSyncMultiBranch`, or `HostedSaas` |
| `SAFARSUITE_SITE_ID` | Stable site/runtime identity such as standalone site, HQ, branch, or hosted tenant |
| `SAFARSUITE_SITE_ROLE` | Site role such as `Standalone`, `Hq`, `Branch`, or `Hosted` |

The app image should expose:

- `GET /health`
- module-gated UI/API startup that reads the local entitlement state through `SAFARSUITE_MODULE_GATEWAY_URL`
- a clear restricted mode when the local server denies a module
- later branch/site behavior from an installation/deployment profile, without putting operational business-data sync into the Control Cloud billing/license channel

## Module Gateway Contract

The shared contract lives in:

```text
src/SafarSuite.ControlDesk.Contracts/ControlCloud/V1/LocalServerModuleGatewayContracts.cs
```

Current app-facing request shape:

```text
LocalServerModuleAccessRequest
  installationId
  moduleCode
  asOfDate
  requestedBy
```

Current app-facing response shape:

```text
LocalServerModuleAccessResponse
  formatVersion = safarsuite-local-module-gateway-v1
  installationId
  moduleCode
  isAllowed
  accessState
  reason
  entitlementVersion
  paidUntil
  warningStartsAt
  graceUntil
  offlineValidUntil
  checkedAtUtc
```

The local-server application handler exists at:

```text
src/SafarSuite.LocalServer.Application/ModuleGateway/EvaluateModuleAccess
```

The local API host now exposes this handler through local-server endpoints for the app runtime:

```text
POST /api/v1/local-server/modules/access
GET  /api/v1/local-server/modules/{moduleCode}/access
```

The SafarSuite app workspace should point module-gated screens and APIs at `SAFARSUITE_MODULE_GATEWAY_URL`, call one of these endpoints, and treat `isAllowed = false` as authoritative. The app should show a restricted/renewal/module-disabled state based on `accessState` instead of reimplementing entitlement rules.

## Local Server Contract

The local server image should provide:

- `safarsuite-local-api`
- `safarsuite-local-worker`
- `safarsuite-local-agent`
- `GET /health` on the local API
- a module-gateway endpoint for the real app to ask whether a module is enabled
- bootstrap import/status endpoints for the downloaded signed bootstrap bundle
- manual and background hooks for entitlement pull and heartbeat reporting
- diagnostics that include runtime services, bootstrap checksums, import audit, and recent errors

Current local API endpoints in this workspace:

```text
GET  /health
GET  /api/v1/local-server/bootstrap
POST /api/v1/local-server/bootstrap-package/import
POST /api/v1/local-server/entitlement/pull
POST /api/v1/local-server/heartbeat
POST /api/v1/local-server/modules/access
GET  /api/v1/local-server/modules/{moduleCode}/access
```

## Handoff Checklist

Before we enable `safarsuite-app` by default:

- confirm the real app image name and registry
- confirm app container port and health route
- wire the SafarSuite app workspace to the local-server module-gateway endpoint
- confirm whether the app owns its own database or uses local-server APIs only
- consume the explicit installation/deployment profile before implementing branch/HQ/cloud data-sync behavior
- add the required changes in the separate SafarSuite app workspace
- add runtime diagnostics for the real app container state

# Control Cloud Receiver Boundary

SafarSuite Control Cloud now has a minimal receiver skeleton for Control Desk outbox envelopes.

## Current Boundary

Control Desk publishes signed `ControlCloudEnvelope` messages.

Control Cloud receives them here:

```text
POST /api/v1/control-desk/messages
```

The receiver currently validates:

- envelope version `1`
- required message, subject, source, and idempotency fields
- payload SHA-256 against the received payload body
- HMAC-SHA256 signature using the configured key id and secret
- duplicate idempotency keys

Successful, duplicate, and rejected receipt attempts are persisted through the cloud-side repository boundary. Development now uses PostgreSQL under the `cloud` schema; the file-backed receiver remains available only as a lightweight fallback provider.

Accepted commercial messages are also projected into a cloud-owned client portal read model:

```text
GET /api/v1/client-portal/clients/{clientId}/commercial-summary
```

The projection currently covers:

- issued and voided invoices
- recorded and reversed payments
- client paid-status changes
- issued credit notes
- issued client refunds
- client credit applications
- entitlement snapshots

The portal summary reports total invoiced, paid, credited, refunded, applied credit, gross invoice balance due, remaining available credit, invoice rows, payment rows, credit rows, refund rows, credit application rows, and entitlement snapshots.

The Client Portal can request a signed entitlement bundle from the latest projected entitlement snapshot after a client-scoped portal session is validated:

```text
GET /api/v1/client-portal/clients/{clientId}/entitlement-bundle?installationId={installationId}
```

The local server uses the machine-facing pull route instead of a human portal session:

```text
GET /api/v1/local-server/installations/{installationId}/entitlement-bundle?clientId={clientId}
```

`installationId` is required. The signing boundary returns deterministic `payloadJson`, readable payload fields, `bundleIssueId`, `installationId`, monotonic `entitlementVersion`, `HMAC-SHA256` signature metadata, signing `keyId`, `payloadSha256`, paid-until, warning-start, grace, offline-valid-until, module states, device limits, and branch limits.

The current hardening slice persists installation registry state and every signed bundle issue inside the same cloud transaction. Installation ids are bound to one client, and older projected entitlement versions are rejected for an installation that has already received a newer bundle. Key rotation metadata beyond `keyId`, portal visibility, and offline renewal fallback remain upcoming entitlement slices.

Control Cloud can queue signed commands for registered installations:

```text
POST /api/v1/control-cloud/clients/{clientId}/installations/{installationId}/commands
```

Local servers can pull pending commands and acknowledge results:

```text
GET /api/v1/local-server/installations/{installationId}/commands/pending
POST /api/v1/local-server/installations/{installationId}/commands/{commandId}/acknowledgement
```

Each command has an idempotency key, monotonic per-installation command version, expiry, payload hash, signing key id, HMAC signature, and persisted acknowledgement result.

The local-server entitlement verification foundation now exists as separate `SafarSuite.LocalServer` layers. It can pull the latest signed bundle from Control Cloud through `GET /api/v1/local-server/installations/{installationId}/entitlement-bundle?clientId={clientId}`, import and cache signed entitlement bundles, verify HMAC signatures offline, reject older entitlement versions, and gate modules through active, warning, grace, restricted, expired, and module-disabled states.

Control Cloud now accepts local-server heartbeats through `POST /api/v1/local-server/installations/{installationId}/heartbeat`. The heartbeat record stores receipt status separately from the reported license state, entitlement version, paid/grace/offline dates, and local-server version.

Control Cloud also exposes a shared installation status endpoint for Control Desk and Client Portal screens:

```text
GET /api/v1/control-cloud/clients/{clientId}/installations/{installationId}/status
```

It returns installation identity, latest heartbeat, reported license state, latest signed entitlement issue, pending command count, and latest command acknowledgement summary.

## Local Runtime

Run the cloud receiver:

```powershell
dotnet run --project src\SafarSuite.ControlCloud.Api\SafarSuite.ControlCloud.Api.csproj --urls http://localhost:5127
```

Control Desk development config is wired to publish to:

```text
http://localhost:5127/api/v1/control-desk/messages
```

The default development signing key matches the Control Desk publisher development key:

```text
KeyId: local-dev
Secret: local-development-signing-secret-change-before-cloud
```

Development Control Cloud uses PostgreSQL:

```json
{
  "Persistence": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "ControlCloud": "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password"
  }
}
```

Development entitlement signing uses:

```text
ControlCloud:EntitlementSigning:ActiveKeyId = local-entitlement-dev
ControlCloud:EntitlementSigning:Issuer = SafarSuite.ControlCloud
ControlCloud:EntitlementSigning:Audience = SafarSuite.ClientPortal
```

Apply the cloud receiver schema:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src\SafarSuite.ControlCloud.Infrastructure\SafarSuite.ControlCloud.Infrastructure.csproj --startup-project src\SafarSuite.ControlCloud.Infrastructure\SafarSuite.ControlCloud.Infrastructure.csproj --context ControlCloudDbContext
```

The PostgreSQL-backed implementation stores:

```text
cloud.control_desk_envelope_receipts
cloud.client_commercial_projections
cloud.client_installations
cloud.entitlement_bundle_issues
cloud.installation_commands
cloud.installation_command_acknowledgements
```

Accepted idempotency keys are protected by a filtered unique index:

```text
ux_control_desk_envelope_receipts_accepted_idempotency_key
```

The file fallback stores receipts and projections under the configured receiver paths:

```text
ReceiptStorePath: App_Data/control-cloud-receipts-dev.jsonl
ProjectionStorePath: App_Data/control-cloud-client-projections-dev.json
```

The receiver returns `X-SafarSuite-Cloud-Message-Id`. Duplicate idempotency keys return the same cloud reference with status `Duplicate`.

Run the local end-to-end smoke after the receiver is running:

```powershell
dotnet run --project tools\SafarSuite.ControlDesk.AccountingSmoke\SafarSuite.ControlDesk.AccountingSmoke.csproj --no-build -- --cloud-receiver-url http://localhost:5127/api/v1/control-desk/messages
```

This creates the accounting chain in Control Desk, enqueues real outbox rows, publishes them through the HTTP publisher, and marks the rows sent after the Control Cloud receiver accepts them.

## Next Slice

Connect the shared installation status endpoint into portal and Control Desk screens for license state, heartbeat state, pending commands, latest entitlement, and audit. After that, add offline renewal file import/export as a fallback path.

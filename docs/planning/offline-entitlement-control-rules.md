# Offline Entitlement Control Rules

Date decided: 2026-07-01

These rules define how SafarSuite Control Desk, SafarSuite Control Cloud, the Client Portal, and deployed SafarSuite local servers should handle renewals, revocation, and offline operation.

## Control Chain

```text
SafarSuite Control Desk
  source of truth for clients, contracts, invoices, payments, renewals, product access
    -> publishes approved commercial/control events
SafarSuite Control Cloud + Client Portal
  cloud authority for portal state, payment callbacks, signed entitlements, device/product commands
    -> local servers pull updates when internet is available
SafarSuite client local server
  caches signed entitlement bundle and enforces access locally
    -> office users keep working on LAN during allowed offline periods
```

The local server should use outbound internet only. The cloud should not need inbound access into a client office.

## Core Product Rule

Heartbeat status and license validity are separate.

```text
Heartbeat status
  tells us whether the deployed local server is communicating with Control Cloud

License validity
  tells the local server whether it is allowed to keep running
```

For a paid one-month offline-capable subscription, the local server must work normally for the paid month even if it cannot reach the internet during that month.

```text
paid_until: 2026-08-01
local entitlement valid until: 2026-08-01
heartbeat: expected whenever internet is available
warning: starts close to paid_until, for example 7 days before expiry
restriction: starts only after expiry plus any configured grace period
```

Heartbeat failure alone must not disturb the client while the signed entitlement is still valid.

## Signed Entitlement Bundle

Control Cloud should issue a signed bundle that the local server can verify without internet.

Minimum fields:

```json
{
  "client_id": "client_123",
  "installation_id": "machine_abc",
  "license_version": 42,
  "issued_at": "2026-07-01T10:00:00Z",
  "valid_from": "2026-07-01",
  "paid_until": "2026-08-01",
  "warning_starts_at": "2026-07-25",
  "grace_until": "2026-08-07",
  "status": "active",
  "products": [
    {
      "code": "billing",
      "status": "active",
      "allowed_users": 10,
      "allowed_devices": 3,
      "allowed_branches": 1
    }
  ],
  "signature": "..."
}
```

The local server must reject unsigned bundles, invalid signatures, wrong installation IDs, and older `license_version` values than the latest accepted version.

## Runtime Behavior

Recommended monthly subscription behavior:

```text
Day 1 through warning_starts_at - 1
  full access, no warning, heartbeat attempts silently when internet exists

warning_starts_at through paid_until
  full access, renewal warning to admin/owner users only

paid_until through grace_until
  grace mode, stronger renewal warnings

after grace_until
  restricted or read-only mode by default

severe abuse or explicit provider decision
  locked mode may be used, but it is not the normal non-payment default
```

Restriction should protect business control without trapping a real office's data. Prefer read-only, no-new-transactions, export, backup, and renewal screens before full lock.

## Renewal Flow

Normal renewal:

```text
Control Desk records invoice/payment
  -> Control Desk publishes paid/renewed status
  -> Control Cloud issues a newer signed entitlement bundle
  -> local server receives it on heartbeat
  -> paid_until, warning_starts_at, and grace_until move forward
```

Offline fallback renewal:

```text
client pays or office approves renewal
  -> Control Cloud or Control Desk generates a signed renewal file
  -> client transfers file to the offline local server
  -> local server imports the file and verifies signature/version/installation
  -> access extends without direct internet on that machine
```

This fallback is mandatory for clients whose local server cannot reach the internet near expiry.

## Revocation Rule

Revocation while the local server is offline cannot be immediate unless the entitlement lease is shorter than the paid period.

Default trusted-client rule:

```text
Issue offline entitlement through the paid subscription period.
Accept that revocation is enforced on next heartbeat or next renewal boundary.
```

High-risk-client rule:

```text
Use shorter leases such as 3 to 7 days, still tied to a paid subscription.
Require more frequent heartbeat or offline renewal file import.
```

Do not punish normal monthly clients with short leases unless there is a business reason.

## Product Access Rule

Entitlements must be product/module based, not only global-license based.

Example:

```text
Client status: active
Billing: active until 2026-08-01
Inventory: revoked
Reporting: active, 10 users
AI Assistant: trial until 2026-07-15
```

The local app gates features by product entitlement and limit, while still showing global subscription and payment state.

## Command Queue Rule

Control Cloud keeps pending commands for each installation. The local server pulls them on heartbeat and acknowledges each applied command.

Command examples:

```text
renew_license
revoke_product
suspend_client
change_user_limit
change_device_limit
enable_trial
request_diagnostics
force_sync
```

Every command must have an idempotency key, a monotonic version, a signed payload, and an acknowledgement/audit result.

Basic implementation now exists in Control Cloud: commands are queued per registered installation, signed with `HMAC-SHA256`, versioned monotonically per installation, and acknowledged by the local-server endpoint with an audit row.

## Clock And Replay Protection

Offline licensing depends on local time, so the local server must track:

- last accepted entitlement version
- last successful cloud time
- last local check time
- clock moved backwards flag
- entitlement issue time and expiry time

Clock tampering should trigger warnings and support review. It should not instantly destroy access for a paid client unless the policy explicitly marks the installation as high risk.

## Support Safety

Support must be able to issue audited emergency help:

- short-lived offline unlock code
- short-lived signed renewal file
- installation-specific support override
- diagnostics export from the local server

Each support action needs reason, actor, client, installation, expiry, and audit trail.

## Audit Rule

Track every commercial and entitlement action:

- contract changed
- invoice issued
- payment recorded, approved, rejected, reversed
- entitlement bundle issued
- product/module enabled, suspended, revoked
- device/installation activated or revoked
- heartbeat received
- command queued
- command acknowledged or failed
- offline renewal file generated/imported
- emergency unlock generated/imported

The audit trail is part of the product, not optional debug logging.

## Implementation Path

1. Keep Control Desk as the commercial source of truth.
2. Persist contract product/module allowances, device/branch/user limits, paid-until, warning, grace, and trust level.
3. Publish accepted commercial events to Control Cloud through the outbox.
4. Basic done: add the Control Cloud signing boundary for latest projected entitlement bundles.
5. Basic done: persist signed-bundle issue audit, require installation ids, bind installations to clients, and reject older entitlement versions for the same installation.
6. Basic done: add cloud-owned command queues and acknowledgement records with signed command payloads, idempotency, monotonic installation command versions, and persisted acknowledgement audit.
7. Basic done: add local-server entitlement import, direct Control Cloud pull, HMAC signature verification, file cache, older-version rejection, and module feature-gating for active, warning, grace, restricted, expired, and module-disabled states.
8. Basic done: add heartbeat endpoint and local-server heartbeat state reporting, keeping heartbeat receipt status separate from reported license validity.
9. Basic done: add portal and Control Desk visibility for license state, heartbeat state, pending commands, and latest entitlement.
10. Basic done: add client portal identity/session boundaries while keeping local-server entitlement pull on a machine-facing endpoint.
11. Next: add offline renewal file import/export as the fallback path for sites that cannot connect near expiry.
12. Add clock/replay protection, richer audit views, and diagnostics.

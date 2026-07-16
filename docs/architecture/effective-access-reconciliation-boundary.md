# Effective Access And Reconciliation Boundary

Reviewed: 2026-07-12

## Decision

The Office Control System approves desired access with one explicit `EffectiveFromUtc` instant. Approval and effectiveness are separate facts: a revision can be approved and published now while becoming enforceable later.

Omitted or past effective input is normalized to the approval instant for immediate access. A future effective instant cannot fall after `PaidUntil`. Changing the schedule or values requires another immutable revision; production cancellation rules for an already-approved future revision remain a separate decision.

## Delivery Policy

Control Desk publishes event version 6 immediately, including `EffectiveFromUtc`. Control Cloud accepts and projects that future desired version so operators can see the upcoming state.

Before the effective instant, Control Cloud does not issue the new installation-bound bundle. It returns `EntitlementScheduled`, keeps the prior signed version as delivered state, and reports the version comparison as `Scheduled`. This prevents a future revision from replacing the server's currently enforceable cache early.

At or after the effective instant, the normal LocalServer pull signs bundle version 5. The bundle retains both the exact effective instant and the date-level `ValidFrom` used by runtime access policy. Older bundles remain readable by treating `ValidFrom` at UTC midnight as the best available effective evidence.

## Observed Evidence

Heartbeat reporting includes the verified entitlement values currently cached by SafarSuite Server:

- entitlement version, status, and effective instant
- paid, warning, grace, and offline-valid dates
- device, branch, named-user, and concurrent-user allowances
- enabled and disabled module states
- normalized module feature limits

Control Cloud stores this canonical observed state with the heartbeat as JSON evidence. This is an observation, not a second source of truth.

## Reconciliation States

Installation status joins three independently evidenced states:

| State | Source |
| --- | --- |
| Desired | Latest Office-issued access projection |
| Delivered | Latest installation-bound signed payload |
| Observed | Latest LocalServer heartbeat cache report |

The read model returns canonical values plus field-level differences and one summary state:

- `Scheduled`: future desired state is intentionally not signed yet.
- `DeliveryPending`: effective desired version has not been signed.
- `ApplyPending`: signed desired version has not been observed.
- `DeliveryDrift`: signed values do not match Office desired values.
- `ObservedDrift`: server-reported values do not match desired values.
- `Ahead`: delivered or observed version is newer than current desired state.
- `InSync`: desired, delivered, and observed canonical values match.
- `Unknown`: Office desired state is unavailable.

Version equality alone is not sufficient for `InSync`; the compared access values must also match.

## Runtime Boundary

No central scheduler mutates server state. Existing LocalServer polling obtains the newly eligible bundle at or after the effective instant. Runtime modules continue to enforce only through the verified LocalServer cache and gateway.

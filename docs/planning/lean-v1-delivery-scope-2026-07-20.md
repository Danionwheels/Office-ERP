# SafarSuite Lean V1 Delivery Scope

Date accepted: 2026-07-20  
Status: Active scope and execution guardrail  
Deployment authority: [`final-system-requirements-and-deployment-contract.md`](../architecture/final-system-requirements-and-deployment-contract.md)

## Outcome

Deliver a functional, reliable SafarSuite control chain without restarting the repository or turning V1 into a complete ERP.

SafarSuite Control Desk V1 installs on one dedicated Windows PC. Its setup provisions the local UI, Office Control API Windows service, native PostgreSQL Windows service, database, migrations, protected configuration, operator bootstrap, shortcuts, diagnostics, and recovery entry points. Routine office use does not require Linux, Docker, DNS, SMTP, Node.js, the .NET SDK, or manual PostgreSQL administration.

Control Cloud and SafarSuite Local Server remain separate deployments. The SafarSuite Client Portal is not required to prove Lean V1.

## Required Chain

```text
Control Desk on one office Windows PC
  client + simple contract + commercial approval
  -> invoice/payment status needed for access approval
  -> immutable desired-access revision
  -> durable outbound publication

Control Cloud
  validate + accept idempotently
  -> sign and distribute entitlement

SafarSuite Local Server
  verify + apply deterministically
  -> acknowledge and report observed state

Control Desk
  show desired, delivered, and observed state separately
```

The release proof must retain correlated IDs from the client through the final observed-state acknowledgement.

## V1 Keep Boundary

| Capability | Lean V1 requirement |
| --- | --- |
| Clients | Client/company record, primary contact, status, and support note. |
| Contracts | One approved current revision with dates, price, modules, device/branch limits, and a change reason. |
| Billing and payments | Issue the invoice needed by the access decision and record/approve a payment or explicit payment status. Corrections remain auditable. |
| Accounting | Generate the minimum balanced provider journal evidence automatically. A general accounting workspace is not a Lean V1 release dependency. |
| Entitlements | Approve immutable desired access, version it, publish durably, and reconcile delivered and observed state. |
| Control Cloud | Receive office authority idempotently, sign entitlements, distribute them, and retain acknowledgements. |
| Local Server | Verify signature, binding, validity, monotonic version, and replay rules before applying access; report observed state. |
| Security and audit | Authenticated operators, explicit scopes, protected secrets, and actor/time/reason evidence for protected changes. |
| One-PC operation | One setup, automatic services, loopback-only listeners, backup/restore, actionable diagnostics, and safe rerun/uninstall behavior. |

## Explicitly Deferred

These items are not deleted merely because they are deferred:

- manual journals, a general ledger workbench, broad chart-of-accounts administration, financial statements, and advanced accounting reports;
- advanced pricing engines, large catalog administration surfaces, and optional commercial automation not needed by the required chain;
- the SafarSuite Client Portal, invitations, SMTP/Brevo, portal MFA, self-service, and public portal deployment;
- multi-PC Control Desk hosting, public Control Desk access, Linux-hosted Control Desk, and microservice extraction;
- automatic signed update/rollback UX beyond the minimum safe versioned pilot install procedure;
- production-scale cloud operations until the installed Lean V1 chain passes with demo data.

Deferred code may remain compiled and tested. It must not expand the Lean V1 acceptance path or block routine use.

## Dead-Code Rule

Do not restart from scratch and do not delete modules by intuition.

A candidate is removable only when an inventory proves all of the following:

1. it is outside the V1 keep boundary;
2. no required API, migration, outbox contract, entitlement rule, or audit trail depends on it;
3. repository search and tests identify its callers and persisted-data impact;
4. removal has a rollback commit and leaves the connected acceptance proof green.

Until then, classify it as `KEEP`, `DEFER`, or `REMOVE-CANDIDATE`. Deferred UI routes should be hidden from the Lean V1 operator path before backend history is removed.

## Small Execution Queue

Only one item is `CURRENT`. Every item ends with a concrete proof.

| ID | Status | Small outcome | Completion proof |
| --- | --- | --- | --- |
| `L1-01` | `DONE` | Finish the native PostgreSQL password/bootstrap boundary. | Commit `e1a05a5`; hosted run [`29712897392`](https://github.com/Danionwheels/Office-ERP/actions/runs/29712897392) completed authenticated provisioning with managed passfile isolation. |
| `L1-02` | `DONE` | Complete the native PostgreSQL lifecycle. | The same sealed-package run passed fresh install, interruption recovery, migration, drift repair, concurrency, uninstall-preserve, reinstall, cleanup, evidence validation, and immutable-package verification. A persistent physical reboot remains part of `L1-15`, not this hosted proof. |
| `L1-03` | `DONE` | Inventory operator-facing routes and backend use cases as `KEEP`, `DEFER`, or `REMOVE-CANDIDATE`. | [`lean-v1-feature-inventory-2026-07-20.md`](lean-v1-feature-inventory-2026-07-20.md) links every visible route to its API/use case and V1 requirement. |
| `L1-04` | `DONE` | Hide deferred workflows from the Lean V1 navigation. | Commit `b513368`; production UI build and production-bundle route smoke passed with six required routes retained and deferred Admin/Reports routes absent. No backend deletion occurred. |
| `L1-05` | `CURRENT` | Persist the first operator and machine secrets safely. | Elevated no-echo bootstrap, normal-user denial, login, scope, and recovery tests pass. |
| `L1-06` | `PENDING` | Install the Office Control API as an automatic Windows service. | Service uses the approved identity, depends on PostgreSQL, binds loopback, and recovers after stop/start. |
| `L1-07` | `PENDING` | Add the routine Control Desk launcher/shortcut. | A normal operator opens the app, signs in, and sees authorized health without a terminal. |
| `L1-08` | `PENDING` | Compose the single setup entry through database, secrets, API, and launcher. | Fresh setup and safe rerun reach Ready without external office infrastructure. |
| `L1-09` | `PENDING` | Prove the minimal Control Desk commercial workflow. | Client, contract, invoice/payment status, balanced journal evidence, and desired access retain correlated IDs. |
| `L1-10` | `PENDING` | Prove durable Control Desk publication during outage/recovery. | Work remains local while cloud is off; reconnect publishes once without duplication. |
| `L1-11` | `PENDING` | Deploy the minimal demo-data Control Cloud receiver/signing path. | Signed office message is accepted idempotently and produces one signed entitlement. |
| `L1-12` | `PENDING` | Prove SafarSuite Local Server entitlement enforcement. | Valid bundle applies; invalid signature, binding, replay, and expired/offline cases fail deterministically. |
| `L1-13` | `PENDING` | Close the installed desired/delivered/observed loop. | Control Desk displays the final acknowledgement as `InSync` with zero differences. |
| `L1-14` | `PENDING` | Automate and restore a Control Desk backup. | Scheduled backup and clean replacement-PC restore retain the correlated chain IDs. |
| `L1-15` | `PENDING` | Run one clean-PC Lean V1 acceptance rehearsal. | One setup, reboot, login, offline work, reconnect, complete chain, backup/restore, and listener audit pass. |
| `L1-16` | `PENDING` | Make the pilot go/no-go decision. | Evidence is reviewed; remaining production controls are explicit and no deferred item is presented as complete. |

## Release Meaning

`Lean V1 pilot-ready` means the one-PC installation and required chain work reliably with demo data on controlled machines. It does not mean the deferred Client Portal, advanced accounting product, or full production cloud operations are complete.

`Production-ready` remains a separate decision after pilot evidence, real secret/key custody, monitored cloud hosting, backup/restore rehearsals, and accepted operating procedures.

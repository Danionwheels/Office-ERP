# Existing Work Disposition

Date reviewed: 2026-07-11

Purpose: preserve useful work while resetting product direction. This document authorizes no deletion by itself.

## Keep And Build On

| Area | Why It Remains Valuable |
| --- | --- |
| Client, contact, contract, pricing, billing, payment, and accounting domains | These are part of the authoritative Office Control System |
| PostgreSQL and transaction boundaries | Required for durable commercial and accounting truth |
| Durable outbox and idempotent Control Cloud receiver | Correct boundary for asynchronous approved-state delivery |
| Entitlement versioning, signing, offline validity, and replay protection | Core to trustworthy dynamic control |
| Installation registry, heartbeat, command acknowledgement, and audit | Required to distinguish desired from observed state |
| SafarSuite Server module gateway and local enforcement | Core endpoint of the connected control chain |
| Focused smoke and integration proofs | Valuable when tied to product acceptance gates |
| Provider accounting research and implementation | Keep as the explainable financial backbone, not as legacy parity work |

## Rework Against The Charter

| Area | Required Change |
| --- | --- |
| Control Desk application shell | Replace architecture-shaped navigation with tested office workflows |
| Client Desk / Client 360 concepts | Preserve useful client context, but avoid generic CRM framing and duplicated workspaces |
| Command Center | Reintroduce only after real operator queues and priorities are demonstrated |
| Setup workspace | Separate stable product/company definitions from client-specific decisions without creating a catch-all screen |
| Cloud installation panel | Show business-level setup and desired/delivered/observed status first; move low-level proof detail behind support access |
| Local Control Desk authentication | Align with the authenticated one-PC Office Control API and authorized-operator boundary |
| Local PostgreSQL lifecycle | Package, start, back up, restore, upgrade, and recover PostgreSQL on the dedicated office PC without a separate server |
| Large frontend modules | Split by accepted workflow and public module boundaries after the target flow is stable |

## Park Without Deleting

| Area | Reason |
| --- | --- |
| Survey/FAS and Travel form cloning | Research evidence only; no legacy parity objective |
| SurveyValuation implementation | Outside the SafarSuite provider-control product goal |
| Further pairing abuse-control expansion | Existing foundation is enough until connected V1 risks require more |
| Additional deployment proof machinery | Current proofs are substantial; product chain and operator workflow now take priority |
| Staging deployment expansion | Resume after the Office Control topology and connected acceptance chain are settled |
| Generic dashboard and reporting expansion | Derive from demonstrated operating questions and read models |
| Client operational data synchronization | Separate future client-data product/data plane |

## Candidate Retirement After Replacement

Nothing is deleted during the reset. After replacement workflows pass acceptance tests, review:

- duplicate or superseded UI shells
- local-only development publisher paths
- proof-only endpoints and configuration
- obsolete legacy route registrations and dormant modules
- historical planning notes that still appear authoritative

Retirement requires a replacement reference, migration impact review, and focused verification.

## Immediate Engineering Audit

The next implementation task is a read-only architecture and data-model audit answering:

1. Which existing aggregate represents desired client access?
2. Where can contract state and entitlement state currently diverge?
3. Can one client have multiple installations without duplicating commercial truth?
4. Are entitlement versions immutable and traceable to contract/payment approval?
5. Can Control Desk show desired, delivered, and observed versions separately?
6. Which APIs load unbounded registers or histories?
7. Which persistence assumptions bind authority to one desktop workstation?
8. Which proof/security surfaces are outside the new active roadmap?

The audit should produce a small gap map before source-code restructuring begins.

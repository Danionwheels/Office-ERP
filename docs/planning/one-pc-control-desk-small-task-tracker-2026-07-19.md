# SafarSuite Control Desk Small-Task Execution Tracker

Date: 2026-07-19  
Status: Active execution view  
Canonical requirements: [`final-system-requirements-and-deployment-contract.md`](../architecture/final-system-requirements-and-deployment-contract.md)  
Parent plan: [`one-pc-control-desk-deployment-plan-2026-07-18.md`](one-pc-control-desk-deployment-plan-2026-07-18.md)

Lean V1 scope: [`lean-v1-delivery-scope-2026-07-20.md`](lean-v1-delivery-scope-2026-07-20.md)

> Execution note (2026-07-20): the Lean V1 scope and its 16-item queue now govern active delivery. The larger ticket inventory below is retained as deferred/reference work so completed evidence and future controls are not lost; it is not the current release checklist.

## Purpose And Authority

This tracker retains detailed office, cloud, client-runtime, and final-acceptance history. Active work is selected from the Lean V1 queue linked above. It does not redefine requirements or physical topology. If this tracker conflicts with the canonical deployment contract, Lean V1 scope, or parent plan, those documents win and this tracker must be corrected.

The Control Desk setup installs every office-local dependency on one dedicated Windows PC: UI, local API, PostgreSQL, prerequisites, migrations, protected configuration, Windows services, recovery tools, and operator entry points. Control Cloud and the Client Portal remain separate cloud deployments. SafarSuite Local Server remains a separate client-premises deployment.

## Execution Rules

1. Only one office-critical-path ticket may be `CURRENT` at a time.
2. Before coding a ticket, add a current task card with three to seven concrete steps, named files or components, tests, evidence, and a stopping condition.
3. If a ticket cannot be completed and verified in roughly one working session, split it before implementation.
4. A build passing is not enough to close a ticket. The ticket's stated evidence must exist.
5. Update this tracker when the code/evidence changes, preferably in the same commit.
6. A waiting external action is marked `WAITING`; an unblocked design ticket may proceed without pretending the waiting gate passed.
7. Cloud and client-runtime lanes may proceed separately, but they must never move Control Desk onto Linux or into the public cloud.

Status values:

- `DONE` — implementation and required evidence are complete.
- `CURRENT` — the one active office-critical-path ticket.
- `WAITING` — blocked on an explicit user, credential, machine, or external-state action.
- `PENDING` — not started or dependency not satisfied.
- `PROVEN` — functionally proven in the integration lab; installed/physical evidence remains.

## Current Position

| Foundation | Status | Evidence |
| --- | --- | --- |
| Canonical one-PC boundary | `DONE` | Requirements and topology are locked. |
| `OFFICE-P0-01` combined local UI/API package | `DONE` | Local and clean GitHub Windows package proof passed. |
| `OFFICE-P0-02` readiness, diagnostics, logging, and automatic outbox | `DONE` | Local and clean GitHub operational gates passed. |
| Functional Control Desk to Cloud to Local Server chain | `PROVEN` | Integration-lab runner: 135 assertions, final `InSync`, zero differences; installed physical acceptance remains pending. |
| `OFFICE-P0-03` hosted native PostgreSQL engineering | `PROVEN` | Commit `e1a05a5`; run [`29712897392`](https://github.com/Danionwheels/Office-ERP/actions/runs/29712897392) passed the sealed-package native lifecycle, cleanup, evidence validation, immutable verification, and artifact upload. Persistent physical reboot evidence remains pending under `DB03-05`/`L1-15`. |

Implementation checkpoint `b513368` is present on the remote PR branch and in this detached evidence worktree. Hosted Windows evidence covers the complete disposable native database lifecycle, and the Lean V1 navigation no longer exposes the deferred Admin Desk, broad Reports, or standalone Voucher Register. The next active slice is first-operator and machine-secret custody.

## Work Map

The 114 rows below are intentionally small delivery/proof tickets, not 114 new product features. We expose the count so work cannot disappear inside a broad milestone.

| Lane | Tickets | Outcome |
| --- | ---: | --- |
| Native PostgreSQL | 21 | Hosted native proof, lifecycle decision, and persistent reboot evidence. |
| Operator security and secret custody | 13 | Persisted operators, protected machine secrets, recovery, and policy enforcement. |
| API service and one setup | 17 | One Windows setup installs and operates every office-local component. |
| Backup and replacement restore | 11 | Scheduled verified backups and a measured clean-PC restore. |
| Signed update and rollback | 12 | Integrity-checked application/database updates with recovery. |
| Installed product coverage | 7 | Every office-owned workflow and protected-change audit trail proven installed. |
| Control Cloud and Client Portal | 16 | Separate cloud deployment, identity, custody, recovery, and compatibility proof. |
| SafarSuite Local Server | 4 | Client-side enforcement, boundary audit, and independent restore. |
| Physical and connected acceptance | 12 | Clean-PC, outage, chain, failure, reinstall, and final go/no-go proof. |
| Existing integration-lab chain | 1 | Retained functional baseline while installed proof is built. |

## Now Queue

Work this queue in order unless a ticket is explicitly marked as an independent parallel lane:

1. `SEC04-01` / `L1-05` — freeze and implement the first-operator and machine-secret custody boundary.
2. `SVC05-01` / `L1-06` — install the Office Control API as a loopback-only automatic Windows service.
3. `L1-07` — add the normal-operator launcher and shortcut.
4. `L1-08` — compose the single setup entry through database, secrets, API, and launcher.
5. `L1-09` — prove the minimal installed commercial workflow after the one-PC entry path exists.

Do not skip to the top-level installer before the operator and secret-custody boundary is fixed.

## Completed Task Card — `DB03-F09`

Objective: identify the one lifecycle mutation that changes a loader-ready sealed PostgreSQL runtime into an `initdb.exe` `0xC0000135` failure, without applying a corrective lifecycle change and without exposing arguments, stdin, credentials, unrestricted output, or full paths.

| Step | Action | Pass condition |
| --- | --- | --- |
| `DB03-F09.1` | Retain the passing pre-lifecycle raw-runtime result from `DB03-F08` as the baseline. | Exact package/runtime hashes, runner identity, seven roots, 20 inspected images, 212 dependency edges, and both version probes remain tied to run `29698364363`. |
| `DB03-F09.2` | Capture only the installed VC++ runtime version and the signed prerequisite install result/reboot classification during `StageRuntime`. | Evidence distinguishes skipped, success, already-installed, or reboot-required without retaining installer output or environment data. |
| `DB03-F09.3` | After the lifecycle attempt, extract the same sealed runtime into a fresh GUID-owned temporary directory and rerun only the version probes. | A second `0xC0000135` isolates a machine-wide prerequisite transition; another pass rules that transition out. |
| `DB03-F09.4` | If the fresh post-lifecycle runtime still passes, run the same version-only probes against the staged runtime immediately before and after its restricted ACL, with an explicit packaged-bin working directory. | The first exact stage/ACL/working-directory boundary that changes the exit classification is retained without arguments, secrets, or raw output. |
| `DB03-F09.5` | Retain one exact-SHA Actions artifact and root-cause statement. | One boundary is identified before any production lifecycle correction is proposed. |
| `DB03-F09.6` | Permit the real probe-set builder to bind its intentionally empty typed list and add a Windows PowerShell 5.1 regression that starts from zero probes. | The real builder adds exactly four safe version probes; the hermetic suite, package gates, validator, and exact rerun pass far enough to retain the 12-probe matrix. |

Stopping condition: prerequisite transition/reboot versus staged-runtime ACL/working-directory behavior is known at one exact boundary. Any corrective lifecycle change requires fresh approval.

Diagnostic evidence: run [29698364363](https://github.com/Danionwheels/Office-ERP/actions/runs/29698364363), head SHA `5d51a289`, passed backend, UI, deployment, vendor verification, hermetic proof, package build/seal, same-origin smoke, and the new pre-mutation runtime probe. The exact sealed package produced successful `initdb.exe --version` and `postgres.exe --version` results; all seven manifest-required roots were traversed across 20 PE images and 212 dependency edges with zero unresolved imports, zero external-PATH resolutions, zero tool failures, and outcome `Ready`. The immediately following lifecycle invocation still failed at `initdb.exe` with exit `-1073741515` / `0xC0000135`. This disproves a missing DLL in the sealed PostgreSQL package and confines the next diagnostic to lifecycle mutation after the raw probe.

DB03-F09 attempt evidence: run [29701315735](https://github.com/Danionwheels/Office-ERP/actions/runs/29701315735), head SHA `c62125a1`, passed UI, backend, deployment, boundary hermetic proof, package build/seal/smoke, raw runtime classification, cleanup, boundary-evidence validation, and database-evidence upload. The native lifecycle remained red at `initdb.exe` / `0xC0000135`. Its invocation-bound, source/archive-bound artifact safely reported `DiagnosticFailed` with no transitions or probes. An exact local call through the real module reproduced the internal failure before probe one: Windows PowerShell 5.1 rejected the mandatory `List[object]` `Probes` parameter because the intentionally new list was empty. No PostgreSQL lifecycle correction is authorized or implemented.

DB03-F09 completion evidence: commit `d14cb06f`, run [29702108045](https://github.com/Danionwheels/Office-ERP/actions/runs/29702108045), passed UI, backend, deployment, boundary hermetic proof, package build/seal/smoke, raw dependency classification, cleanup, evidence validation, and evidence upload. Both recorded VC++ transitions were `AlreadySatisfied` at version `14.51.36247.0`. All 12 exact version probes passed with exit `0`: fresh runtime before ACL, fresh runtime after restricted ACL, and installed runtime after lifecycle, each for `initdb.exe` and `postgres.exe` with inherited and runtime-bin working directories. The validated outcome is `FullInitdbInvocationBoundary` with issue `VersionProbesPassButLifecycleFails`; prerequisites, package integrity, ACL mutation, working directory, and installed-runtime corruption are ruled out.

## Completed Task Card — `DB03-F10`

Objective: distinguish a child-process or dynamic-DLL search-path failure during full cluster initialization from another full-`initdb`-only failure, without changing production lifecycle behavior or retaining arguments, credentials, raw output, environment values, or full paths.

| Step | Action | Pass condition |
| --- | --- | --- |
| `DB03-F10.1` | Reuse the exact sealed runtime and full disposable initialization inputs from the native proof. | Package/source/hash bindings match the accepted DB03-F09 evidence; no production path is touched. |
| `DB03-F10.2` | Run one full initialization in a fresh GUID-owned directory with the current lifecycle environment. | The baseline reproduces the exact safe exit classification without retaining arguments, paths, output, or password material. |
| `DB03-F10.3` | Run the same disposable initialization with only the manifest-approved PostgreSQL runtime roots in the child process search path and the runtime-bin working directory. | A pass identifies the child/dynamic-loader search boundary; another `0xC0000135` rules that hypothesis out. |
| `DB03-F10.4` | Retain a finite, invocation-bound comparison artifact and validate it before upload. | Evidence contains only package/run identities, boolean completion, numeric/hex exits, and an allowlisted outcome. |
| `DB03-F10.5` | Produce one exact-SHA root-cause statement and stop. | No production lifecycle correction is implemented until the diagnostic result and proposed fix receive fresh approval. |

Stopping condition met: exact run [29702851828](https://github.com/Danionwheels/Office-ERP/actions/runs/29702851828), commit `fb466db2`, completed all three trials. `InheritedCwd`, `RuntimeBinCwd`, and `ApprovedRuntimeRoots` each returned `-1073741515` / `0xC0000135`; outcome `ApprovedRuntimeRootsHypothesisDisproved`. Cleanup, evidence validation, and upload passed. Any production lifecycle correction still requires fresh approval.

## Completed Task Card — `DB03-F11`

Objective: determine whether the full initialization failure belongs to the top-level `initdb.exe` process or its spawned PostgreSQL backend, without persisting raw output, arguments, paths, environment values, or secrets.

| Step | Action | Pass condition |
| --- | --- | --- |
| `DB03-F11.1` | Capture the disposable baseline trial's native output only in memory. | Nothing new is printed or persisted; existing cleanup and secret deletion remain unchanged. |
| `DB03-F11.2` | Parse only fixed PostgreSQL initialization stage markers and the allowlisted child-exception signature. | Evidence retains a finite stage enum, child-failure boolean, and safe exit classification only. |
| `DB03-F11.3` | Extend the strict validator and Windows PowerShell 5.1 hermetic cases. | Raw text, paths, arguments, environment material, and credentials are rejected before upload. |
| `DB03-F11.4` | Run one exact hosted proof and stop. | The failure is classified as top-level, spawned-backend, or inconclusive before any corrective change is proposed. |

Stopping condition met: exact run [29703454889](https://github.com/Danionwheels/Office-ERP/actions/runs/29703454889), commit `736af6a`, retained `TopLevelInitdb` and `NotObserved`. All three full trials returned direct process exit `-1073741515` / `0xC0000135`, with no child-exception signature and no initialization stage marker. Cleanup, evidence validation, and upload passed. Any corrective lifecycle change still requires fresh approval.

## Completed Task Card — `DB03-F12`

Objective: identify the smallest `initdb` activation profile that changes from a normal finite exit into the direct `0xC0000135` exception, without changing production lifecycle behavior.

| Step | Action | Pass condition |
| --- | --- | --- |
| `DB03-F12.1` | Reuse the exact sealed runtime and fresh GUID-owned disposable directories. | Every probe is source/package bound and touches no production path. |
| `DB03-F12.2` | Run an allowlisted ladder: help/option parsing, missing-data validation, minimal trust-based cluster initialization, then the exact SCRAM/password-file initialization. | The first profile that changes into `0xC0000135` identifies whether the boundary is general cluster activation or authentication/password handling. |
| `DB03-F12.3` | Retain only profile enum, completion, numeric/hex exit, and a finite outcome. | No arguments, native output, paths, environment values, or credentials enter evidence. |
| `DB03-F12.4` | Extend the strict validator and hermetic suite, run one exact hosted proof, and stop. | The activation boundary is reproducible before any loader-level trace or lifecycle correction is proposed. |

Stopping condition met: exact run [29703926235](https://github.com/Danionwheels/Office-ERP/actions/runs/29703926235), commit `766aae0`, retained outcome `PreActivationBoundary`. `HelpOnly` exited `0`; `MissingDataValidation`, `MinimalTrust`, and `ExactScram` each exited `-1073741515` / `0xC0000135`. PostgreSQL calls `get_restricted_token()` after help/version handling and before data setup, re-executing without the Administrators SID. Any lifecycle correction still requires fresh approval.

## Completed Task Card — `DB03-F13`

Objective: allow PostgreSQL's required restricted-token `initdb` child to access only the packaged runtime, disposable staging data, and one bootstrap file during initialization, then restore the canonical hardened ACLs.

| Step | Action | Pass condition |
| --- | --- | --- |
| `DB03-F13.1` | Resolve the invoking Windows user SID and add a temporary, explicit least-privilege ACL bridge: runtime read/execute, staging data modify, bootstrap file read. | No `Users`/`Everyone` grant and no production network or topology change. |
| `DB03-F13.2` | Run the existing exact SCRAM `initdb` invocation without bypassing PostgreSQL's restricted token. | Initialization passes through the restricted re-execution normally. |
| `DB03-F13.3` | In `finally`, delete the bootstrap file and reapply canonical runtime/data ACLs, including failure paths. | No temporary invoking-user ACE remains after success or failure. |
| `DB03-F13.4` | Add Windows PowerShell 5.1 ACL regressions and run one exact hosted proof. | Native lifecycle, cleanup, final ACL audit, migrations, and immutable-package verification pass. |

Stopping condition met: runs `29704538158`, `29704994477`, and `29705225703` passed restricted-token initialization and advanced through service start with canonical ACL verification. Corrective implementation was approved when the user delegated the next safe step on 2026-07-20.

## Completed Task Card — `DB03-F15` (`DONE`)

Objective: identify which finite authenticated provisioning phase returns `psql.exe` exit `2`, then correct only the proven connection/authentication defect.

| Step | Action | Pass condition |
| --- | --- | --- |
| `DB03-F15.1` | Retain only `AdminBootstrap`, `MigratorDefaults`, or `OwnershipConvergence` with the existing executable and numeric exit. | No SQL, native output, paths, role names, environment values, or credentials enter logs. |
| `DB03-F15.2` | Run one exact hosted proof at commit `d55ada1`. | One provisioning phase is identified after initialization, service registration, start, and interruption recovery pass. |
| `DB03-F15.3` | Apply the smallest connection/authentication correction and add a focused regression. | The diagnosed phase succeeds without weakening SCRAM, loopback-only networking, passfile custody, or the virtual service account. |
| `DB03-F15.4` | Run the full native lifecycle proof. | Provisioning, migrations, drift repair, uninstall preservation, reinstall, ACL audit, and immutable package verification pass. |

Completion evidence: commits `096941f`, `bcc7f9f`, `a340ab8`, `4698c30`, `4e1988c`, `f85df80`, and `e1a05a5` progressively isolated ambient PostgreSQL credential precedence, one-time service-SID ACL convergence, routine data-directory ACL scope, and finite mutation-case expectations. Exact hosted run [`29712897392`](https://github.com/Danionwheels/Office-ERP/actions/runs/29712897392) passed all four release gates. Its Windows package job passed native initialization, authenticated provisioning, exact migrations, service/configuration/ACL/security repair, migration prefix/conflict/divergence cases, unavailable-credential fail-closed behavior, concurrency, stopped/missing service recovery, uninstall-preserve, reinstall, cleanup, boundary-evidence validation, sealed-package immutability, and artifact upload.

## Office Critical Path

### `OFFICE-P0-03` — Native PostgreSQL Remote Proof

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `DB03-01` | `DONE` | Authenticate/push through the available Git credential path and start exact-commit CI. | None; local proof already retained | Remote SHA `8bb34cf`; Actions run `29688706012`. |
| `DB03-F01` | `DONE` | Retrieve the failed native-step log and produce a root-cause report. | Failed run `29688706012`; authenticated `gh` | `File.Replace` null-binding defect reproduced and focused fix approved. |
| `DB03-F02` | `DONE` | Implement the approved focused fix and run only its relevant local gates. | `DB03-F01`; explicit approval | Commit `3f4b712`; independent review and Windows PowerShell 5.1 hermetic proof passed. |
| `DB03-F03` | `DONE` | Push the replacement commit and start one exact-commit rerun. | `DB03-F02` | SHA `3f4b712`; run `29690475562` reached a distinct native DLL-loading failure. |
| `DB03-F04` | `DONE` | Retrieve and classify the replacement-run native failure. | `DB03-F03` | Exit `-1073741515` mapped to `0xC0000135` (`DLL_NOT_FOUND`); exact executable is absent from current evidence. |
| `DB03-F05` | `DONE` | Add secret-safe executable identity and hexadecimal exit-code diagnostics. | `DB03-F04`; explicit approval | Commit `679a901`; hermetic proof and independent Windows PowerShell 5.1 review passed with no secret-marker disclosure. |
| `DB03-F06` | `DONE` | Push the diagnostic commit and identify the exact missing-prerequisite executable. | `DB03-F05` | SHA `679a901`; run `29692163870` identified `initdb.exe` / `0xC0000135` on `windows-2025-vs2026` image `20260714.173.1`. |
| `DB03-F07` | `DONE` | Pin only the Office Windows proof to the PostgreSQL-17-tested runner and verify one exact rerun. | `DB03-F06`; explicit approval | Commit `7f6d1ef`; run `29693519086` reproduced `initdb.exe` / `0xC0000135`, disproving Server 2025 drift as the cause. |
| `DB03-F08` | `DONE` | Probe the sealed runtime before lifecycle mutation and identify unresolved DLL imports. | `DB03-F07`; explicit approval received 2026-07-19 | Commits `96d2aa1`, `2579b85`, and `5d51a289`; run `29698364363`: seven required roots, 20 PE images, 212 edges, both version probes exit `0`, zero unresolved/external/tool failures, then lifecycle `initdb.exe` fails `0xC0000135`. Sealed-package dependency defect disproved. |
| `DB03-F09` | `DONE` | Isolate prerequisite-transition/reboot behavior from staged-runtime ACL or working-directory behavior. | `DB03-F08`; initial diagnostic approved 2026-07-19; focused empty-list binding repair approved 2026-07-20 | Commit `d14cb06f`, run `29702108045`: two VC++ transitions already satisfied; all 12 pre/post-ACL, working-directory, and installed-runtime probes exit `0`; outcome `FullInitdbInvocationBoundary`. |
| `DB03-F10` | `DONE` | Isolate the full-`initdb` child/dynamic-DLL search-path boundary without changing production lifecycle behavior. | `DB03-F09`; diagnostic approved 2026-07-20 | Commit `fb466db2`, run `29702851828`: all three full trials return `0xC0000135`; approved runtime search roots do not change the result. |
| `DB03-F11` | `DONE` | Classify the full-initialization failure as top-level `initdb` versus its spawned PostgreSQL backend and retain the last allowlisted stage. | `DB03-F10`; diagnostic approved 2026-07-20 | Commit `736af6a`, run `29703454889`: direct `initdb.exe` `0xC0000135`, stage `NotObserved`, no spawned-backend exception. |
| `DB03-F12` | `DONE` | Isolate the exact transition from safe option parsing into the crashing cluster-initialization path. | `DB03-F11`; diagnostic approved 2026-07-20 | Commit `766aae0`, run `29703926235`: help exits `0`; every normal-startup profile exits direct `0xC0000135`; outcome `PreActivationBoundary`. |
| `DB03-F13` | `DONE` | Bridge PostgreSQL restricted-token initialization with temporary least-privilege invoking-user ACLs, then restore hardened ACLs. | `DB03-F12`; corrective implementation approved 2026-07-20 | Commit `15cdb44`; later runs pass initdb and canonical ACL checks. |
| `DB03-F14` | `DONE` | Configure the PostgreSQL virtual service account with a null password pointer and make dependency verification array-safe in Windows PowerShell 5.1. | `DB03-F13` | Commits `7d954be`, `f684750`, `7794932`; run `29705225703` passes service registration, start, and interruption classification. |
| `DB03-F15` | `DONE` | Classify and correct the first authenticated provisioning connection failure. | `DB03-F14` | Final commit `e1a05a5`; exact run `29712897392` passed the full native lifecycle. |
| `DB03-02` | `DONE` | Verify the immutable package and vendor supply-chain gate. | `DB03-F09` | Run `29712897392` passed reviewed PostgreSQL/VC++ acquisition, SHA/signature checks, package seal, and final immutable-package verification. |
| `DB03-03` | `DONE` | Verify native install, interruption, ACL/security drift, migration failure, serialization, and reinstall drills. | `DB03-02` | Run `29712897392` uploaded validated native Windows lifecycle evidence with every hosted drill green. |
| `DB03-04` | `DONE` | Record the hosted engineering checkpoint and replacement hashes. | `DB03-03` | Exact checkpoint `e1a05a507d70892e8cd9a41ff452fe5a4ad0e8e9`, run `29712897392`; physical reboot and cross-milestone acceptance remain separate tickets. |
| `DB03-05` | `PENDING` | Attach persistent two-service cold-reboot evidence to P0-03. | `DB03-04`, `SVC05-12` | New boot ID, both services, exact schema, Ready, and loopback listeners. |
| `DB03-06` | `PENDING` | Accept the PostgreSQL install, minor/major upgrade, repair, backup, and rollback policy. | `DB03-04` | Dated lifecycle decision names the supported paths; the executable upgrade proof is retained by `UPD07-09`. |

### `OFFICE-P0-04` — Operator Bootstrap And Secret Custody

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `SEC04-01` | `DONE` | Freeze the storage contract: operators in Control Desk PostgreSQL; machine secrets in versioned DPAPI-protected files with exact ACLs. | `DB03-04` | [`control-desk-operator-and-machine-secret-storage-contract.md`](../architecture/control-desk-operator-and-machine-secret-storage-contract.md) fixes the schema, ProgramData path, service-SID ACL, bootstrap boundary, and preserve/regenerate/reissue rules. |
| `SEC04-02A` | `DONE` | Add the local-operator aggregate, status, role/scope rules, and normalized-email invariant. | `SEC04-01` | Domain model covers canonical email identity, active/disabled state, allowlisted roles/scopes, Administrator/admin-scope pairing, and monotonic session-invalidating security versions; 41/41 domain tests pass. |
| `SEC04-02B1` | `DONE` | Add the persisted-operator authentication port/use case and password-free session principal. | `SEC04-02A`, `SEC04-03` | Focused tests prove normalized lookup, valid authentication, generic unknown/wrong/invalid/blank credential failure, disabled-operator denial, and no password field in the principal. |
| `SEC04-02B2` | `DONE` | Add the administrator guard plus create-operator application use case. | `SEC04-02B1` | Active-admin authorization, normalized-email conflict handling, canonical access, password hashing, explicit transaction/save, forbidden mapping, and password-free results pass focused coverage. |
| `SEC04-02B3` | `DONE` | Add disable and permission-change application use cases with last-active-administrator protection. | `SEC04-02B2` | Serialized administrator mutations, authorization, missing-target handling, canonical/idempotent access changes, session-version changes, and last-admin refusal pass focused coverage. |
| `SEC04-02B4` | `DONE` | Add the offline password-recovery application use case with required actor and reason. | `SEC04-02B3` | ID/email resolution, required actor/reason validation, missing/ambiguous target refusal, password replacement, disabled-operator recovery, session invalidation, and secret-free results pass focused coverage. |
| `SEC04-02C1` | `DONE` | Map the operator aggregate to the authoritative Auth schema and three-table relational shape. | `SEC04-02B4` | Domain-owned role/scope grants preserve behavior; EF metadata proves table/schema names, composite keys, cascade ownership, allowlist/status/version checks, and the unique normalized-email index; 41/41 domain and 81/81 API tests pass. |
| `SEC04-02C2` | `DONE` | Implement the EF operator repository, including serialized Administrator mutations. | `SEC04-02C1` | Repository tracks the complete aggregate, registers only for PostgreSQL, requires a transaction before taking its versioned advisory lock, and translates the other-active-Administrator query through both grant tables. |
| `SEC04-02C3` | `DONE` | Generate the Auth-schema migration and prove migration/model parity against PostgreSQL. | `SEC04-02C2` | Migration `20260720035506_AddLocalOperatorAuthentication` has exact model parity; disposable PostgreSQL CI run `29715822966` proves migration application, aggregate round-trip, normalized lookup, unique-email rejection, active-admin queries, and the real advisory transaction lock. |
| `SEC04-03` | `DONE` | Extract the PBKDF2-SHA256 codec from the HTTP endpoint without changing the accepted format. | `SEC04-01` | Application-owned codec port and Infrastructure PBKDF2-SHA256 implementation preserve bounded legacy verification, emit the reviewed 120,000-iteration canonical format, and pass golden, tamper, malformed, authorization, and diagnostics coverage; 53/53 API tests pass. |
| `SEC04-04` | `DONE` | Make login/session authorization read active persisted operators instead of packaged `IOptions` users. | `SEC04-02C3`, `SEC04-03` | HTTP login uses the Application authentication handler; V2 sessions carry operator/security versions; every request reloads the operator and compares active status, identity, version, roles, and scopes. Repository-only login plus password/status/permission invalidation pass with 88/88 API tests. |
| `SEC04-05` | `CURRENT` | Remove packaged Development operators and session secrets from non-Development settings. | `SEC04-04` | Published settings are clean; Production fails closed before bootstrap. |
| `SEC04-06A` | `PENDING` | Implement versioned DPAPI envelopes with atomic replacement, lifecycle mutex, tamper detection, and non-secret fingerprints. | `SEC04-01` | Hermetic success, interruption, concurrent-write, and tamper tests pass. |
| `SEC04-06B` | `PENDING` | Converge machine-secret directory/file ACLs and expose secrets only through the installed configuration provider. | `SEC04-06A` | Exact ACL audit and normal-Windows-account denial proof pass. |
| `SEC04-07A` | `PENDING` | Implement elevated, no-echo first-operator provisioning. | `SEC04-02C`, `SEC04-03`, `SEC04-06B` | Bootstrap is single-use, refuses overwrite, and emits only secret-free evidence. |
| `SEC04-07B` | `PENDING` | Implement offline operator recovery, machine-secret replacement/reissue, and session invalidation. | `SEC04-07A`, `SEC04-04` | Recovery requires actor/reason, invalidates old sessions, and proves preserve-versus-reissue rules. |
| `SEC04-08` | `PENDING` | Inventory every installed API route and bind each protected route to an explicit role/scope policy. | `SEC04-04` | Automated inventory fails on anonymous or mismatched route policy; intended public health/login routes are allowlisted. |
| `SEC04-09` | `PENDING` | Run the installed auth, ACL, redaction, recovery, and reinstall security matrix. | `SEC04-05`, `SEC04-07B`, `SEC04-08` | `401`, `403`, valid login, ACL denial, operator preservation, recovery, and secret scan pass. |

There will be no unauthenticated first-operator web endpoint. Bootstrap and emergency recovery are elevated local setup ceremonies.

### `OFFICE-P0-05` — API Service, Operator Entry, And One Setup

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `SVC05-01` | `PENDING` | Decide the installer/bootstrapper mechanism and freeze the installation manifest. | `SEC04-09` | Approved `OPEN-001` decision and manifest of every office-local component. |
| `SVC05-02` | `PENDING` | Define API installed paths, virtual service identity, receipts, ownership checks, and preservation rules. | `SVC05-01` | Reviewed lifecycle contract and collision rules. |
| `SVC05-03` | `PENDING` | Atomically install and verify the API/UI payload under `%ProgramFiles%`. | `SVC05-02` | Hash/reparse/interruption tests pass. |
| `SVC05-04` | `PENDING` | Register the API service in demand-start mode and apply least-privilege binary, configuration, log, and pgpass ACLs. | `SVC05-03`, `SEC04-06B` | Exact service identity/command and ACL audit pass. |
| `SVC05-05A` | `PENDING` | Generate installed non-secret Production configuration and bind protected values through the machine-secret provider. | `SVC05-04` | No secret appears in JSON, registry, command line, receipt, or log. |
| `SVC05-05B` | `PENDING` | Configure the PostgreSQL service dependency, delayed automatic start, and bounded recovery actions. | `SVC05-05A` | Exact SCM dependency/start/recovery settings pass. |
| `SVC05-06A` | `PENDING` | Activate the API service only after database, migrations, first operator, and `/ready` succeed. | `SVC05-05B`, `SEC04-07A` | Failed prerequisite/readiness leaves the service stopped with an actionable reason. |
| `SVC05-06B` | `PENDING` | Add narrowly classified API-service repair operations and ownership checks. | `SVC05-06A` | Repair mutation/no-mutation matrix passes without overwriting foreign state. |
| `SVC05-07` | `PENDING` | Add the readiness-aware launcher with operator-friendly recovery guidance. | `SVC05-06B` | It opens only `127.0.0.1:5188` after readiness without requiring a terminal. |
| `SVC05-08` | `PENDING` | Add owned Start-menu entry and optional desktop shortcut with idempotent repair. | `SVC05-07` | Exact shortcut target/arguments and repair pass. |
| `SVC05-09` | `PENDING` | Implement API/shortcut/binary uninstall that preserves all office data and protected state. | `SVC05-04`, `SVC05-08` | Uninstall/reinstall reopens the same operator and database IDs. |
| `SVC05-10A` | `PENDING` | Compose one elevated setup entry for preflight, VC++, PostgreSQL, and migrations. | `SVC05-01`, `DB03-04`, `SVC05-02` | One invocation and idempotent rerun pass through the database-ready checkpoint. |
| `SVC05-10B` | `PENDING` | Extend the same setup entry through machine secrets and no-echo first-operator provisioning. | `SEC04-07A`, `SVC05-10A` | One invocation reaches protected operator-ready state; rerun refuses destructive overwrite. |
| `SVC05-10C` | `PENDING` | Extend the same setup entry through API service, launcher/shortcut, and final readiness. | `SVC05-03`, `SVC05-05B`, `SVC05-06B`, `SVC05-08`, `SVC05-10B` | One invocation reaches routine operator entry with explicit rollback boundaries. |
| `SVC05-11A` | `PENDING` | Add the complete one-setup lifecycle to the hermetic Windows gate. | `SVC05-10C`, `SVC05-09` | Interruption, recovery, ACL drift, and reinstall simulations pass. |
| `SVC05-11B` | `PENDING` | Run the complete setup/service lifecycle on a disposable native Windows host. | `SVC05-11A` | Real stop/start, interruption, recovery, ACL, uninstall, and reinstall evidence pass. |
| `SVC05-12` | `PENDING` | Run a persistent reference-PC cold reboot with both real services and launcher login. | `SVC05-11B` | New boot ID, exact migrations, Ready, loopback listeners, and normal login. This also supplies `DB03-05`. |

### `OFFICE-P0-06` — Backup And Replacement-PC Restore

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `BAK06-01` | `PENDING` | Accept RPO, RTO, off-PC destination class, retention, custody, and failure policy. | `SVC05-12` | Dated decision; four-hour and 14-local/30-day-off-PC values remain proposals until accepted. |
| `BAK06-02` | `PENDING` | Define the backup artifact/manifest and redaction contract. | `BAK06-01`, `SEC04-06B` | Schema, golden fixture, and redaction test. |
| `BAK06-03` | `PENDING` | Implement atomic custom-format `pg_dump` backup with checksum and lifecycle serialization. | `BAK06-02` | Manual backup log, dump, checksum, and manifest. |
| `BAK06-04` | `PENDING` | Build the protected configuration/operator recovery bundle. | `BAK06-02`, `SEC04-07B` | Exact ACL audit, manifest, and secret scan. |
| `BAK06-05` | `PENDING` | Implement verified atomic off-PC copy and visible destination-loss handling. | `BAK06-03`, `BAK06-04` | Success, partial-copy rejection, disconnect, retry, and checksum proof. |
| `BAK06-06A` | `PENDING` | Add the automated local schedule and callable pre-update backup hook. | `BAK06-05` | Schedule export, success/failure run history, and pre-update invocation evidence. |
| `BAK06-06B` | `PENDING` | Implement local/off-PC retention that never deletes the last verified backup. | `BAK06-06A` | Age/count/space-pressure pruning matrix passes. |
| `BAK06-07` | `PENDING` | Implement restore preflight and guarded database/configuration restore. | `BAK06-03`, `BAK06-04` | Empty target succeeds; incompatible/non-empty target fails closed without confirmation. |
| `BAK06-08A` | `PENDING` | Automate a disposable restore and reconcile required record classes and protected configuration. | `BAK06-05`, `BAK06-07` | Database IDs, operators, outbox, configuration identity, and checksums reconcile. |
| `BAK06-08B` | `PENDING` | Expose backup freshness, destination, last verification, and outage state in diagnostics. | `BAK06-06B`, `BAK06-08A` | Destination outage is visible and secret-free; recovery clears the warning. |
| `BAK06-09` | `PENDING` | Rehearse restore on a clean replacement PC and measure RPO/RTO. | `BAK06-08B` | Independent restore, retained IDs, timings, and reconciliation report. |

### `OFFICE-P0-07` — Signed Update And Rollback

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `UPD07-01` | `PENDING` | Accept signing authority, key custody, release channel, downgrade/revocation, and supported-version policy. | `BAK06-09` | Dated `OPEN-004` decision. |
| `UPD07-02` | `PENDING` | Define the release manifest and produce the signed immutable artifact in CI. | `UPD07-01` | Clean SHA/tag, complete hashes, signature metadata, compatibility/migration set. |
| `UPD07-03A` | `PENDING` | Implement fail-closed release-signature, manifest, and payload-hash verification. | `UPD07-02` | Valid, unsigned, wrong-key, altered-manifest, and altered-payload vectors pass. |
| `UPD07-03B` | `PENDING` | Implement version, compatibility, disk-space, and migration preflight before service changes. | `UPD07-03A` | Negative preflight matrix leaves services and installed files unchanged. |
| `UPD07-04` | `PENDING` | Add a durable update journal and mandatory fresh local/off-PC backup gate. | `UPD07-03B`, `BAK06-09` | Fault-injection phase matrix and bound backup IDs/checksums. |
| `UPD07-05` | `PENDING` | Drain services, stage the verified payload, and atomically switch application binaries. | `UPD07-04`, `SVC05-12` | Exact old/new hashes and service-state evidence. |
| `UPD07-06A` | `PENDING` | Run post-update readiness and a minimal authenticated business-workflow smoke. | `UPD07-05` | Success finalizes activation only after readiness and workflow proof. |
| `UPD07-06B` | `PENDING` | Roll back a failed binary activation to the previous healthy payload. | `UPD07-05` | Forced startup failure restores exact prior hashes and readiness. |
| `UPD07-07` | `PENDING` | Recover an incompatible schema change from the bound pre-update backup. | `UPD07-06A`, `UPD07-06B`, `BAK06-09` | Application and data return to the last verified pair. |
| `UPD07-08A` | `PENDING` | Prove accepted, unsigned, wrong-key, and tampered release cases end to end. | `UPD07-03B`, `UPD07-06A` | Secret-free acceptance/rejection evidence retains installed state on rejection. |
| `UPD07-08B` | `PENDING` | Prove startup failure, migration failure, power interruption, and resume/rollback cases. | `UPD07-06B`, `UPD07-07` | Every interruption reaches a declared healthy old or new state. |
| `UPD07-09` | `PENDING` | Prove the accepted PostgreSQL minor/major upgrade path with mandatory backup and recovery. | `DB03-06`, `BAK06-09`, `UPD07-08B` | Cluster identity/data are retained on success; failed upgrade restores the last verified database/application pair. |

## Product And Connected Lanes

These lanes preserve the proven integration-lab chain and prepare installed-system acceptance. They do not install cloud or client components through the Control Desk setup.

### Integration And Installed Product Coverage

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `CHAIN-01` | `PROVEN` | Reproduce the functional integration-lab Control Desk to Cloud to Local Server chain. | None | Committed runner: 135 assertions, balanced journals, final `InSync`, and zero differences. |
| `PROD-01A` | `PENDING` | Run installed client, contact, support-history, and audit-navigation workflows. | `SVC05-12` | Retained workflow IDs, screenshots/logs, and expected audit records. |
| `PROD-01B` | `PENDING` | Run installed contract, custom-pricing, product-catalog, and desired-access workflows. | `SVC05-12` | Retained contract/revision/catalog/access IDs and expected audit records. |
| `PROD-01C` | `PENDING` | Run installed invoice, payment, allocation, receipt, and accounting workflows. | `SVC05-12` | Balanced journal and retained invoice/payment/allocation/receipt IDs. |
| `PROD-01D` | `PENDING` | Run installed durable-outbox, retry, delivery, and reconciliation workflows. | `PROD-01A`, `PROD-01B`, `PROD-01C` | Desired, queued, delivered, and observed IDs reconcile without unexplained differences. |
| `PROD-01E` | `PENDING` | Seal the installed office-function coverage matrix and record any explicit exclusions. | `PROD-01D` | Every `CD-002` function has pass evidence or an approved exclusion. |
| `PROD-02` | `PENDING` | Verify reconciliation UX, business terminology, and authority/data boundaries in the installed package. | `PROD-01E` | Desired/delivered/observed remain distinct; no infrastructure-led workflow or client operational-data leakage. |
| `PROD-03` | `PENDING` | Verify actor, time, reason, source document, and revision evidence for every protected commercial/access change. | `PROD-01E`, `SEC04-09` | Create/change/reversal/approval/recovery matrix proves complete immutable provenance. |

### Control Cloud And Client Portal

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `CLOUD-01A` | `PENDING` | Make the cloud-only deployment the default staging path. | `CHAIN-01` | Compose/preflight/CI target includes only Control Cloud, Portal, and their private dependencies. |
| `CLOUD-01B` | `PENDING` | Label full co-location explicitly as a Disposable Integration Lab with demo-data-only guards. | `CLOUD-01A` | Lab naming, warnings, preflight, and CI prevent it from being mistaken for office or production topology. |
| `CLOUD-02A` | `PENDING` | Separate office and cloud configuration/preflight inputs. | `CLOUD-01A` | Office proves no DNS/SMTP dependency; cloud preflight requires only cloud inputs. |
| `CLOUD-02B` | `PENDING` | Separate Development, staging, and production cloud data, credentials, keys, email identities, and DNS names. | `CLOUD-02A` | Environment manifests and automated collision checks cover all six resource classes. |
| `CLOUD-03A` | `PENDING` | Provision and audit authorized cloud DNS, TLS, HTTPS endpoints, and ingress. | `CLOUD-02B` | Certificate/hostname/redirect/header/port audit passes. |
| `CLOUD-03B` | `PENDING` | Provision private cloud PostgreSQL and apply verified migrations. | `CLOUD-02B` | No public database listener; expected migration IDs and application readiness pass. |
| `CLOUD-03C` | `PENDING` | Configure cloud database backup/monitoring and prove an independent clean restore. | `CLOUD-03B` | Backup, alert, restore, retained-ID, and timing evidence pass. |
| `CLOUD-04A` | `PENDING` | Accept signing-key and provider-secret custody, access, replacement, and audit procedures. | `CLOUD-02B` | Dated owners, stores, access review, fingerprints, and replacement runbook. |
| `CLOUD-04B` | `PENDING` | Rehearse routine rotation and compromised-key/provider-secret response. | `CLOUD-04A` | Old/new acceptance windows, revocation, reissue, recovery, and audit evidence pass. |
| `CLOUD-05A` | `PENDING` | Prove Portal invitation, expiry, single-use, and password-reset controls outside Development. | `CLOUD-02B` | Positive and negative identity integration cases pass. |
| `CLOUD-05B` | `PENDING` | Prove Portal MFA, session expiry/revocation, and cookie/security-header controls. | `CLOUD-05A` | Positive, negative, replay, and stolen-session cases pass. |
| `CLOUD-05C` | `PENDING` | Prove Portal rate-limit, lockout, and identity-audit controls. | `CLOUD-05B` | Threshold, recovery, source, actor, and alert evidence pass. |
| `CLOUD-06A` | `PENDING` | Make the dated production cloud-host and transactional-email-provider decision and send one non-production test message. | `CLOUD-03A`, `CLOUD-05A` | Accepted `OPEN-006` record plus sender/domain/delivery evidence; no office dependency. |
| `CLOUD-06B` | `PENDING` | Configure cloud health monitoring, alerting, log retention, and service/database recovery runbooks. | `CLOUD-03C` | Alert-delivery, retention, restart, and recovery drills pass. |
| `CLOUD-06C` | `PENDING` | Audit Cloud/Portal authority and projection boundaries. | `CLOUD-04B`, `CLOUD-05C`, `CLOUD-06B` | Cloud cannot originate commercial decisions; Portal remains projection/self-service only. |
| `CLOUD-07` | `PENDING` | Run the version-contract compatibility and independent-upgrade matrix between Control Desk and Control Cloud. | `CLOUD-03B`, `CHAIN-01` | Supported old/new pairs preserve meaning; incompatible versions fail explicitly without reinterpreting state. |

### SafarSuite Local Server

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `LS-01` | `PROVEN` | Install the client-runtime lab and prove outbound registration, entitlement pull, command, heartbeat, and acknowledgement. | `CHAIN-01` | Installed-runtime evidence with no inbound dependency. |
| `LS-02` | `PROVEN` | Run signature, installation binding, validity, monotonic version, replay, and offline-validity matrix. | `LS-01` | Deterministic allow/deny evidence and continued offline enforcement. |
| `LS-03A` | `PENDING` | Audit Local Server authority, outbound-only communication, and client operational-data boundaries. | `LS-02` | No provider commercial decisions or client transactions cross the approved boundary. |
| `LS-03B` | `PENDING` | Independently back up and restore the Local Server database. | `LS-03A` | Clean-target restore, retained installation/entitlement state, and timing evidence pass. |

## `OFFICE-P0-08` — Physical And Connected Acceptance

| ID | Status | Small task | Depends on | Done/evidence |
| --- | --- | --- | --- | --- |
| `ACC08-01` | `PENDING` | Accept the supported Windows/hardware/free-space baseline and prepare a secret-rejecting evidence schema. | None | Dated `OPEN-007` decision, machine inventory, evidence-schema tests. |
| `ACC08-02` | `PENDING` | Freeze the exact signed office release candidate and certify a clean starting PC. | `DB03-05`, `SEC04-09`, `SVC05-12`, `BAK06-09`, `UPD07-09`, `PROD-03` | Clean tag/CI/hash/signature and baseline with no repo/SDK/Docker/WSL/PostgreSQL/state. |
| `ACC08-03` | `PENDING` | Run one-click install, no-echo bootstrap, cold reboot, launcher login, listener, dependency, and Windows Firewall audit. | `ACC08-01`, `ACC08-02` | Both services after newer boot, Ready, loopback only, no inbound/public firewall rule, and no manual infrastructure dependency. |
| `ACC08-04` | `PENDING` | Prove local business work and durable outbox behavior during cloud/Linux outage. | `ACC08-03` | Authenticated reads/writes continue; queued rows and outage timestamps retained. |
| `ACC08-05` | `PENDING` | Run installed client-to-payment-to-entitlement publish/sign/distribute/apply/heartbeat chain. | `ACC08-04`, `CLOUD-06C`, `CLOUD-07`, `LS-03B` | Complete business, outbox, receipt, bundle, import, decision, and heartbeat ID ledger. |
| `ACC08-06` | `PENDING` | Reconcile desired, delivered, and observed state after reconnection. | `ACC08-05` | Exactly-once acceptance, `InSync`, and zero unexplained differences. |
| `ACC08-07A` | `PENDING` | Drill clean shutdown, interrupted shutdown, API/database process failure, and startup diagnostics. | `ACC08-06` | Both services recover safely and every forced startup failure produces actionable evidence. |
| `ACC08-07B` | `PENDING` | Repeat backup and replacement-PC restore against the frozen release candidate. | `ACC08-07A`, `BAK06-09` | Backup hashes, measured timings, and retained-ID reconciliation match the candidate. |
| `ACC08-08A` | `PENDING` | Exercise accepted, tampered, startup-failed, migration-failed, and interrupted updates. | `ACC08-07B`, `UPD07-08A`, `UPD07-08B` | Every case fails closed or reaches the expected healthy signed version with office state intact. |
| `ACC08-08B` | `PENDING` | Exercise data-preserving uninstall and reinstall on the accepted candidate. | `ACC08-08A` | Same database, operators, protected configuration, outbox, and office IDs reopen. |
| `ACC08-09A` | `PENDING` | Run connected cloud/network/signature/replay failure drills. | `ACC08-08B` | Each forced failure has the expected fail-closed, queue, retry, or recovery result. |
| `ACC08-09B` | `PENDING` | Seal the secret-free evidence package and issue the formal go/no-go decision. | `ACC08-09A` | Checksummed evidence is tied to the exact candidate, includes three independent restore proofs, and records the decision. |

## Canonical Requirement Ownership

Each requirement has one primary closure ticket to prevent duplicated or forgotten acceptance:

- `TOP-001..003` → `ACC08-03`
- `TOP-004..005` → `ACC08-04`
- `TOP-006` → `CLOUD-01A`
- `TOP-007..008` → `CLOUD-02A`
- `CD-001..002` → `PROD-01E`
- `CD-003..005`, `CD-207` → `PROD-02`
- `CD-101` → `SVC05-10C`
- `CD-102`, `CD-108` → `ACC08-03`
- `CD-103` → `SVC05-07`
- `CD-104` → completed `OFFICE-P0-02`
- `CD-105..106` → `ACC08-04`
- `CD-107` → completed requirements baseline
- `CD-109` → `ACC08-07A`
- `CD-201`, `CD-203` → `SEC04-09`
- `CD-202` → `PROD-03`
- `CD-208` → `SEC04-07B`
- `CD-204` → `BAK06-06A`
- `CD-205`, `CD-209` → `BAK06-09`
- `CD-206` → `ACC08-08A`
- `CLD-001` → `CLOUD-01A`
- `CLD-002`, `CLD-008` → `CLOUD-06C`
- `CLD-003` → `ACC08-05`
- `CLD-004` → `CLOUD-03A`
- `CLD-005` → `CLOUD-03C`
- `CLD-006` → `CLOUD-04B`
- `CLD-007` → `CLOUD-06A`
- `CLD-009` → `CLOUD-05C`
- `CLD-010` → `CLOUD-07`
- `CLD-011` → `CLOUD-02B`
- `LS-001`, `LS-004` → `LS-01`
- `LS-003`, `LS-005` → `LS-02`
- `LS-002`, `LS-006` → `LS-03A`
- `OPEN-001` → `SVC05-01`
- `OPEN-002` → `UPD07-09`
- `OPEN-003`, `OPEN-008` → `BAK06-01`
- `OPEN-004` → `UPD07-01`
- `OPEN-005` → completed requirements baseline
- `OPEN-006` → `CLOUD-06A`
- `OPEN-007` → `ACC08-01`

## Milestone Closure Rule

Parent milestones in the active roadmap change to complete only when every row in that milestone section is `DONE` and its required evidence is retained. `PROVEN` is not installed-production completion. In particular, `OFFICE-P0-03` remains in progress until the active failure tickets, `DB03-02` through `DB03-06`, and the cross-milestone cold-reboot evidence are complete. A hosted CI run may close engineering proof but cannot replace cold-reboot, replacement-PC, or final connected physical acceptance.

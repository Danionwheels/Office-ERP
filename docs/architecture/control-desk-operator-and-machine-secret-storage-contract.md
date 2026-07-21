# Control Desk Operator and Machine-Secret Storage Contract

Date accepted: 2026-07-20  
Status: Authoritative for `SEC04` / Lean V1 operator security  
Deployment authority: [`final-system-requirements-and-deployment-contract.md`](final-system-requirements-and-deployment-contract.md)

## Boundary

SafarSuite Control Desk is installed on one dedicated Windows office PC. Operator identities belong to the local Control Desk PostgreSQL database. Machine secrets belong to a versioned Windows DPAPI LocalMachine envelope under the Control Desk ProgramData root.

Installed Production must not source operators, password hashes, or active signing secrets from packaged `appsettings`, command-line arguments, process-wide environment variables, the registry, the UI bundle, or a public HTTP bootstrap endpoint.

Development and automated-test hosts may continue to inject deterministic fixtures explicitly. Those fixtures must never be accepted by an installed Production host.

## Persisted Operator Model

The Auth module owns the operator model. The database representation is relational and remains behind Application ports; API contracts never expose persistence entities or password hashes.

### `auth.local_operators`

| Field | Contract |
| --- | --- |
| `operator_id` | Stable UUID primary key generated once. |
| `email` | Trimmed display/login email, maximum 320 characters. |
| `normalized_email` | Trimmed invariant-uppercase email, maximum 320 characters, unique. |
| `full_name` | Required trimmed operator name, maximum 200 characters. |
| `password_hash` | Canonical versioned PBKDF2-SHA256 value; never returned by an API/read model. |
| `status` | `Active` or `Disabled`; unknown values fail closed. |
| `security_version` | Positive monotonic integer copied into sessions and incremented by password reset, recovery, status change, role change, or scope change. |
| `created_at_utc` | Immutable creation timestamp. |
| `updated_at_utc` | Timestamp of the most recent protected change. |

### `auth.local_operator_roles`

Composite key `(operator_id, role)`. Role values are trimmed canonical names. V1 bootstrap grants `Administrator` only.

### `auth.local_operator_scopes`

Composite key `(operator_id, scope)`. Scope values are trimmed canonical names from `ControlDeskScopes`. V1 bootstrap grants `control-desk:admin` only.

Deleting operator history is not a normal operation. Disabling an operator and incrementing `security_version` is the supported removal path. Email uniqueness includes disabled operators so identity cannot be silently recycled.

## Authentication and Session Contract

- Login normalizes the supplied email and loads exactly one persisted operator.
- Authentication returns the same generic failure for an unknown email, disabled operator, malformed hash, or wrong password.
- Password comparison is fixed-time and uses the accepted canonical `pbkdf2-sha256.<iterations>.<salt>.<hash>` format.
- A bearer session contains `operator_id`, normalized identity, roles, scopes, `security_version`, issue/expiry times, nonce, and signing-key identifier.
- Every authenticated request confirms the persisted operator is active and that its current `security_version`, roles, and scopes still match the token. Protected changes therefore invalidate existing sessions immediately.
- Production fails closed when the operator store, active signing key, or protected machine-secret envelope is unavailable or invalid.

## Machine-Secret Envelope

Canonical installed path:

```text
%ProgramData%\SafarSuite\ControlDesk\Secrets\Machine\control-desk-machine-secrets.v1.json
```

The outer JSON envelope contains only:

- envelope schema version;
- protection kind `WindowsDpapiLocalMachine`;
- product/purpose identifiers;
- base64 DPAPI ciphertext;
- non-secret SHA-256 ciphertext fingerprint.

The DPAPI-protected payload contains:

- payload schema version;
- generation UUID and creation time;
- one active Control Desk session-signing key identifier;
- at least 32 random bytes of session-signing key material.

V1 retains one active session-signing key. Replacement or compromise recovery creates a new generation and immediately invalidates every previously issued Control Desk session. The envelope never contains database passwords; PostgreSQL credentials retain their existing database-secret lifecycle.

Writes use a named machine-wide lifecycle mutex, a same-directory temporary file, flush-to-disk, atomic replacement, and post-write decrypt/parse verification. Interrupted replacement must leave either the previous valid envelope or the new valid envelope, never a partially accepted secret.

## Exact Windows ACL Contract

Inheritance is disabled on the machine-secret directory and envelope. Owner is `NT AUTHORITY\SYSTEM`.

| Principal | Directory | Envelope |
| --- | --- | --- |
| `NT AUTHORITY\SYSTEM` | Full control | Full control |
| `BUILTIN\Administrators` | Full control | Full control |
| `NT SERVICE\SafarSuiteControlDeskApi` | Read/execute and traverse | Read |

No `Users`, `Authenticated Users`, `Everyone`, interactive operator, database service, or inherited ACE is allowed. Before the API service SID exists, setup permits only SYSTEM and Administrators; API service installation must add the exact service-SID read grant before first start. A normal Windows account must be unable to read or replace the envelope.

DPAPI LocalMachine protects the ciphertext at rest; the ACL is the process boundary that prevents arbitrary local users from obtaining ciphertext that any machine-authorized process could otherwise request to decrypt.

## Bootstrap Ceremony

There is no unauthenticated first-operator HTTP endpoint.

The first operator is created by an elevated local setup command that:

1. verifies administrator elevation and package identity;
2. accepts email/full name and reads the password twice through a no-echo prompt;
3. creates or verifies the protected machine-secret envelope;
4. opens the local PostgreSQL connection through the existing managed credential boundary;
5. in one transaction, refuses to proceed unless the operator table is empty and inserts the first active Administrator with `control-desk:admin`;
6. emits only operator ID, normalized email, timestamps, envelope generation ID/fingerprint, and success/failure codes.

A rerun never overwrites the operator, password, permissions, or machine secret. Once any operator exists, the bootstrap command exits with an explicit already-bootstrapped result.

## Preserve, Regenerate, and Reissue Rules

| Event | Operators | Machine-secret envelope | Session result |
| --- | --- | --- | --- |
| Setup rerun or application update | Preserve | Preserve exact valid envelope | Existing sessions remain valid. |
| Normal uninstall | Preserve with database data | Preserve | Reinstall can resume the same office identity. |
| Explicit purge | Delete only after separate confirmation/evidence | Delete | All sessions invalid. |
| Same-PC repair | Preserve | Repair ACLs; never regenerate a valid envelope | Existing sessions remain valid. |
| Password/permission/status recovery | Update one operator and increment `security_version` | Preserve | That operator's old sessions invalid. |
| Signing-secret compromise | Preserve | Atomically replace with a new generation | All old sessions invalid. |
| Replacement-PC restore | Restore operators/database | Do not activate the old machine-bound DPAPI envelope; create a new envelope after restore authorization | All old sessions invalid; operators use restored credentials unless separately reset. |
| Corrupt or undecryptable envelope | Preserve | Fail closed; require elevated replacement/reissue ceremony | No session issue or validation until reissued. |

## Recovery and Audit

- Offline operator recovery requires elevation, target operator ID/email, a non-empty actor, and a non-empty reason.
- Password recovery, enable/disable, role/scope changes, and signing-secret replacement create audit evidence with actor, reason, time, target/generation, and outcome; no password, hash, key, ciphertext, connection string, or full secret path enters logs.
- Recovery refuses ambiguous email matches, missing operators, invalid role/scope names, and attempts to leave the installation without an active Administrator.
- Secret evidence uses only generation IDs and SHA-256 fingerprints. Fingerprints are identifiers, not authenticity proofs.

## Installation and Configuration Provider

The installed API receives the decrypted signing secret only from a dedicated machine-secret configuration provider after ACL and envelope validation. The provider exposes the minimum in-memory option needed by session signing and does not export values back into environment variables.

Packaged Production settings contain session duration and non-secret policy only. They contain no `Users` entries and no `SessionSigningSecret`. Production startup validates that persisted-operator authentication and the installed machine-secret provider are active; configuration-only users are Development/test compatibility, not a Production fallback.

## Required Proofs

The implementation cannot close `L1-05` until automated evidence covers:

- normalized-email uniqueness and disabled-operator behavior;
- canonical hash success, tamper, and invalid-format cases;
- single-use elevated no-echo bootstrap and non-admin denial;
- exact ACL allowlist plus normal-user read/replace denial;
- login, `401`, wrong-scope `403`, and valid-scope success using a persisted operator;
- immediate session invalidation after password, status, role, or scope change;
- atomic secret write, interruption, tamper, concurrency, same-PC preserve, compromise replacement, and replacement-PC reissue;
- secret scanning of logs, evidence, package settings, and published artifacts.

# Desired-Access Limit Boundary

Date accepted: 2026-07-12

Canonical direction: `docs/architecture/product-charter-2026-07-11.md`

## Decision

The Office Control System owns every client access allowance. Control Cloud distributes an exact signed copy, and SafarSuite Server verifies and exposes that copy for local enforcement. Neither Cloud nor a client installation may reinterpret or silently increase the approved values.

The canonical limit set is:

- allowed devices
- allowed branches
- optional allowed named users
- optional allowed concurrent users
- zero or more module-scoped feature limits

## Semantics

`AllowedNamedUsers` and `AllowedConcurrentUsers` are nullable. `null` means no explicit cap or a legacy-unspecified value. It does not mean zero. Zero is a real explicit limit.

When both user limits exist, concurrent users cannot exceed named users. All counts and feature-limit values are nonnegative.

A feature limit has one normalized identity and quantity:

```text
(ModuleCode, FeatureCode) -> LimitValue + Unit
```

The module must be enabled in the same immutable decision. A parent cannot contain duplicate module/feature identities. Feature codes and units are normalized uppercase identifiers so casing cannot create separate limits.

## Immutable Chain

```text
ClientContract revision
  -> approved ClientAccessRevision
  -> EntitlementSnapshot
  -> EntitlementSnapshotIssued event v6
  -> Control Cloud projection
  -> installation-bound signed bundle v5
  -> SafarSuite Server verified cache
  -> GET /api/v1/local-server/limits
```

Each boundary copies values into its own immutable artifact. A later contract, access approval, event, or bundle cannot rewrite historical rows.

## Persistence

Office PostgreSQL stores feature limits in normalized child tables for contracts, access revisions, and entitlement snapshots. Each table has a unique parent/module/feature index and a nonnegative-value check. User caps remain nullable columns with nonnegative and cross-field checks.

Control Cloud stores complete limits in the entitlement projection and signed payload. Its bundle-issue register stores the user caps and feature-limit count as bounded audit facts; the signed payload remains the exact issued artifact.

Legacy rows migrate with null user caps and no feature-limit rows. The migration does not invent commercial meaning.

## Runtime Boundary

SafarSuite Server verifies signature, installation identity, version ordering, user-limit consistency, duplicate feature identities, and enabled-module references before replacing its cache.

`GET /api/v1/local-server/limits` returns the verified allowance set and current entitlement access state. Product modules own usage measurement and enforcement for their feature codes; they must compare usage with this gateway rather than reading Cloud or Office databases.

## Deliberately Not Claimed

This slice represents and distributes approved limits. Effective access scheduling and value-level observed-state acknowledgements are now defined in `effective-access-reconciliation-boundary.md`. Named-user membership, concurrent-session counting, and per-feature usage counters remain module/runtime responsibilities and are not reasons to weaken this source-of-truth boundary.

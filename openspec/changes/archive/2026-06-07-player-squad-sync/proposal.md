# Proposal: player-squad-sync — admin-triggered, persisted player squad sync

## Problem

Player search for tournament special bets is backed by `ApiSportsFootballPlayerSearchProvider.SearchAsync`, which builds its squad index **lazily** on the first search request via `HybridCache.GetOrCreateAsync` and never persists it. This has four concrete problems:

1. **Burns the API-Sports quota uncontrollably.** The account is on the API-SPORTS FREE plan: **100 requests/day total**. Every cache miss (restart, eviction, scale-out) silently re-fetches `/teams?search=` + `/players/squads?team=` for every included team. Each deployed instance rebuilds independently, so the same data is fetched multiple times across instances — none of it under anyone's control.
2. **No persistence.** The index lives only in cache. There is no durable record of who the players are, and nothing survives a restart without spending quota again.
3. **Silent failure.** `EnsureSuccessStatusCode()` throws *inside* the cache factory, so a failed build just makes search return nothing — no admin-visible diagnostic, no "last synced" signal, no error report.
4. **No admin control.** There is no way for an admin to decide *when* the (quota-consuming) refresh happens. It happens implicitly, at the worst possible times.

The free worldcup26.ir football-data already has the right shape for this: an explicit **admin "Sync" button** that persists the result and reports counts. Player squads should work the same way.

## Why now

The tournament special picks feature (recently unified — see commit `17eef0f`) depends on player autocomplete. The current lazy provider makes that feature fragile and quota-hostile. Because the daily budget is only 100 requests, *any* unplanned refresh is a real risk. The user's #1 driver: **"no quiero gastar llamadas a la API"** — quota spend must become explicit and admin-controlled.

## Goal / success criteria

- API-Sports is called **only** when an admin explicitly clicks the sync button. Zero automatic, lazy, scheduled, or background calls.
- Player squad data is **persisted in the database** and survives restarts, evictions, and scale-out.
- Player search reads from the **database**, never from the live API.
- The admin sees clear feedback after a sync: players indexed, teams processed, per-team errors, and a **last-synced timestamp**.
- The change mirrors the existing football-data sync pattern closely enough that it's obvious to anyone who already understands that flow.

## Chosen approach

**Approach 1 from the exploration** (dedicated table + dedicated sync command/handler/endpoint mirroring `SyncFootballData*`). The full comparison and rationale live in `exploration.md` — not repeated here. In short: it keeps the rate-limited API-Sports provider isolated from the free worldcup26.ir sync, reuses a proven pattern almost verbatim, and turns the search path into a pure indexed DB read.

Shape:

- New `ExternalFootballPlayer` entity + `external_football_players` table (own EF migration, mirroring the `external_football_*` table conventions: `(ProviderName, ExternalId)` unique index, secondary index on `NormalizedName`, `SyncedAtUtc` per row, full delete+insert replace on sync).
- New `SyncPlayerSquadsCommand` → `SyncPlayerSquadsHandler` → `SyncPlayerSquadsResultDto`, mirroring the `SyncFootballData*` trio. The team-resolution + squad-fetch logic that lives in today's `BuildPlayerIndexAsync` **moves into this handler** — it is the *only* place that calls API-Sports.
- New endpoint `POST /api/football-data/players/sync` with `[Authorize(Policy = "Admin")]`.
- Admin UI button + result/error display, mirroring the existing `syncFootballData()` flow (`isSyncing` signal, success/error message signals, last-synced display).
- `ApiSportsFootballPlayerSearchProvider.SearchAsync` rewritten to **query the persisted rows** by `NormalizedName` — no API calls on the search path at all.

### How Redis factors in

Redis is already wired as HybridCache's L2 and as the messaging backbone — **this change does not add new Redis usage**. The search path moves to a direct indexed DB read (see decision 2 below), and the sync is admin-triggered, so there is no new cache or lock requirement now. If a perf cache or a distributed sync-lock is ever needed, both are easy additive follow-ups on the existing Redis wiring.

## Resolved design decisions

1. **Endpoint route/naming → `POST /api/football-data/players/sync`.**
   Justification: it sits in the existing `/api/football-data` group right next to `GET /api/football-data/players/search`, matches the established `POST /sync` and `POST /fixtures/group-stage/import` admin-action convention in `FootballDataEndpoints.cs`, and keeps all external-football concerns under one route group. No new endpoint group or `/api/admin` namespace needed.

2. **Drop caching on the search path (no HybridCache wrapper over the DB read).**
   The read is a single indexed lookup on `NormalizedName`, which is already fast. Adding a cache layer now buys nothing and adds invalidation/staleness concerns. Re-adding a thin HybridCache read-through over the DB is a trivial follow-up if a real need ever appears. Favor simplicity. The cache layer must **never** wrap the external API call — that's exactly the behavior we're removing.

3. **No server-side "sync in progress" guard now — defer it.**
   This is a hobby app with effectively one admin. The client-side disabled-while-syncing button (the existing `isSyncing` pattern) is sufficient to prevent accidental double-clicks. A server-side guard (a `SyncedAtUtc` recency check or a short Redis lock) is a known, easy additive follow-up if overlapping syncs ever become plausible. Not worth the ceremony today.

4. **Abort the whole sync on the first HTTP 429.**
   A 429 means the daily quota is exhausted. Continuing through the remaining teams would only spend more of the 100/day budget on requests that are guaranteed to fail. So: catch per-team `HttpRequestException`, and **as soon as a 429 is seen, stop and return** what was indexed so far plus a clear "rate limited / quota exhausted" error in the result. Non-429 per-team failures (e.g. a team not found) are collected as per-team errors and the sync continues — only 429 aborts.

## Scope

### In scope

- New `ExternalFootballPlayer` entity, `external_football_players` table, EF configuration, migration, and `DbSet`.
- Repository support to **replace** (delete-existing + insert-new) and **read** persisted player rows.
- `SyncPlayerSquadsCommand` / `SyncPlayerSquadsHandler` / `SyncPlayerSquadsResultDto` (counts: teams processed, players indexed; per-team errors; `SyncedAtUtc`).
- `POST /api/football-data/players/sync` admin endpoint.
- Admin UI: "Sync players" button, result feedback (counts, errors, last-synced timestamp), error display — mirroring the football-data sync UI.
- Rewrite `ApiSportsFootballPlayerSearchProvider.SearchAsync` to read from the DB.
- Guard for missing API key (no-op with a clear "not configured" result, matching today's DI behavior — never throw) and empty included-team list (report `TeamsProcessedCount: 0` clearly, not a silent success).

### Out of scope

- **API key / included-team-list configuration.** `ApiSportsFootball__ApiKey` and `IncludedTeamNames` are handled via Dokploy env vars — no code or UI for them.
- **Team-selection UI.** Explicitly rejected; env vars already cover team selection.
- **Automatic / scheduled / background sync.** The whole point is admin-triggered-only.
- **Multi-provider abstraction** beyond what already exists.
- **New Redis usage** (perf cache or sync-lock) — deferred follow-ups, not part of this slice.

## Risks / open items

- **Stale data between syncs is intentional** (admin-triggered by design) — mitigated by surfacing "last synced" prominently in the admin UI.
- **Team-id resolution cost.** First sync ≈ 2 requests/team (`/teams?search=` then `/players/squads`); with ~8 default teams ≈ 16 requests. The exploration suggests persisting the resolved API-Sports team id to halve steady-state cost (≈ 8 req/sync). Worth doing if cheap, but secondary to the abort-on-429 safeguard — flagged for the spec/design phase to decide the exact persistence shape.
- **Concurrent sync triggers** — accepted risk for now; client-side button-disable only (see decision 3).
- **Migration drift** — low risk (additive table), but must follow the exact naming/index conventions of the existing `external_football_*` tables to avoid EF model-snapshot drift.
- **Players lacking ids/names** — keep the existing filter (`Id is not null && !string.IsNullOrWhiteSpace(Name)`); `PhotoUrl` / `Position` stay nullable.

## Next steps

`sdd-spec` and `sdd-design` can run in parallel from this proposal.

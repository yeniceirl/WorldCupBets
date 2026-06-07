# Player Squad Sync Specification

## Purpose

Replace the lazy, cache-built, never-persisted player squad index with an admin-triggered sync that persists `ExternalFootballPlayer` rows in the database. Player search becomes a pure DB read with zero external API calls. API-Sports calls happen only when an admin explicitly clicks "Sync players," protecting the 100 requests/day quota.

## Requirements

### Requirement: Admin-triggered player squad sync

The system MUST expose `POST /api/football-data/players/sync`, authorized via the `Admin` policy, which fetches squads for the configured `IncludedTeamNames` from API-Sports and replaces the persisted `external_football_players` rows with the result.

#### Scenario: Successful sync replaces persisted rows

- GIVEN an admin is authenticated and `ApiSportsFootball__ApiKey` and `IncludedTeamNames` are configured
- WHEN the admin triggers `POST /api/football-data/players/sync`
- THEN the system resolves each included team, fetches its squad from API-Sports, deletes existing persisted player rows for the provider, and inserts the newly fetched rows in the same operation
- AND the response reports `TeamsProcessedCount`, `PlayersIndexedCount`, an empty `Errors` list, and `SyncedAtUtc`

#### Scenario: Non-admin cannot trigger sync

- GIVEN a caller is not authenticated or lacks the `Admin` policy
- WHEN they call `POST /api/football-data/players/sync`
- THEN the system MUST reject the request with an authorization error and MUST NOT call API-Sports or modify persisted rows

#### Scenario: Per-team failure is reported without aborting

- GIVEN one included team cannot be resolved or its squad fetch fails with a non-429 error (e.g. team not found)
- WHEN the sync runs
- THEN the system collects that team's name and error message into the result's `Errors` list, continues processing remaining teams, and still replaces persisted rows with whatever squads were successfully fetched

### Requirement: Abort sync on first rate-limit response

The system MUST abort the entire sync as soon as any API-Sports call returns HTTP 429, persist whatever was successfully indexed up to that point, and report the abort distinctly from per-team "not found" style errors.

#### Scenario: First 429 stops further processing

- GIVEN the sync is partway through processing the included teams
- WHEN an API-Sports call for a team returns HTTP 429
- THEN the system MUST stop processing remaining teams immediately, replace persisted rows with the squads indexed so far, and include a distinct "rate limited / quota exhausted" entry in the result rather than a generic per-team error

#### Scenario: Non-429 errors do not trigger abort

- GIVEN a team lookup fails with a non-429 error (e.g. HTTP 404 "team not found")
- WHEN the sync processes that team
- THEN the system MUST record it as a per-team error distinguishable from a rate-limit abort and continue with the next team

### Requirement: Persisted player search with no live API calls

`ApiSportsFootballPlayerSearchProvider.SearchAsync` MUST query the persisted `external_football_players` table by normalized-name match and MUST NOT call API-Sports or any external football-data provider while serving a search request.

#### Scenario: Search reads from persisted rows

- GIVEN the `external_football_players` table has been populated by a prior sync
- WHEN a caller searches for a player name
- THEN the system returns matches found via a normalized-name lookup against the persisted table
- AND no HTTP call to API-Sports occurs as part of serving the search

#### Scenario: Search before any sync has run

- GIVEN the `external_football_players` table is empty because no sync has ever completed
- WHEN a caller searches for a player name
- THEN the system returns an empty result set without error and without calling any external API

### Requirement: Admin sync result feedback

The system MUST surface sync results to the admin in the same style as the existing "Sync provider" feedback: teams processed, players indexed, per-team errors (including distinct rate-limit-abort reporting), and the `SyncedAtUtc` timestamp, with the last-synced timestamp persisted and displayed across page reloads.

#### Scenario: Admin sees success summary after sync

- GIVEN an admin triggers the sync and it completes (with or without partial per-team errors)
- WHEN the response returns
- THEN the admin UI displays `TeamsProcessedCount`, `PlayersIndexedCount`, any `Errors` entries, and the `SyncedAtUtc` timestamp, mirroring the existing football-data sync result display

#### Scenario: Last-synced timestamp persists across reloads

- GIVEN a sync has completed at least once
- WHEN the admin reloads the admin page
- THEN the most recent `SyncedAtUtc` is still shown, sourced from persisted data rather than in-memory state

### Requirement: Guard against missing configuration

The system MUST treat a missing/blank API-Sports API key as a no-op that returns a clear "not configured" result rather than throwing, and MUST treat an empty `IncludedTeamNames` list as a clean zero-result sync rather than a silent success or an error.

#### Scenario: Missing API key produces a clear no-op result

- GIVEN `ApiSportsFootball__ApiKey` is blank or not set
- WHEN an admin triggers the sync
- THEN the system MUST NOT throw, MUST NOT call API-Sports, and MUST return a result clearly indicating the provider is "not configured"

#### Scenario: Empty included-team list reports zero processed teams

- GIVEN `IncludedTeamNames` is empty
- WHEN an admin triggers the sync
- THEN the system MUST return a result with `TeamsProcessedCount: 0` and `PlayersIndexedCount: 0`, clearly distinguishable in the UI from a successful sync of configured teams, and MUST NOT report this as an error

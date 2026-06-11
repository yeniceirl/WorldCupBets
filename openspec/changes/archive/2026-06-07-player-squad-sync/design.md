# Design: player-squad-sync

## Technical Approach

Mirror the existing `SyncFootballData*` trio almost verbatim for a new, isolated player-squad concern. Add an `ExternalFootballPlayer` entity + `external_football_players` table, a dedicated sync command/handler/result that becomes the ONLY caller of API-Sports, and rewrite `ApiSportsFootballPlayerSearchProvider.SearchAsync` into a pure indexed DB read. No new abstractions, no Redis, no scheduling. Implements proposal Approach 1 and resolved decisions 1-4.

## Architecture Decisions

| Decision | Choice | Rejected | Rationale |
|----------|--------|----------|-----------|
| Repository home | New sibling `IExternalFootballPlayerRepository` (Replace + Search) | Add methods to `IExternalFootballDataRepository` | The existing repo is snapshot-shaped (one DTO graph). Players have different read shape (ranked name search) and a different rate-limited provider. A sibling keeps single-responsibility and mirrors "one repo per concern". |
| Search ranking lives in | Repository (DB query + in-memory rank on the ≤candidates set) | Push full rank into SQL | `Contains` + `NormalizedName` index narrows rows cheaply; the existing starts-with-word/alphabetical/take-10 rank is preserved in C# verbatim. Avoids translating fuzzy rank to SQL. |
| Team-id resolution caching | Reuse persisted `TeamExternalId` on player rows as the resolution cache; skip `/teams?search=` when a prior row exists for that team name | Separate `(ProviderName,NameEn)->TeamId` lookup table | Risk #2 mitigation with zero new schema. First sync ≈2 req/team; later syncs ≈1 req/team. Read existing rows BEFORE the delete+insert replace. |
| Search-path caching | None (drop HybridCache from the provider entirely) | Thin read-through HybridCache over DB | Decision 2. Indexed read is already fast; cache adds staleness/invalidation. Trivial additive follow-up if ever needed. |
| 429 handling | Abort whole sync on first 429; non-429 per-team errors collected and continue | Best-effort continue through 429s | Decision 4. 429 = quota exhausted; further calls only burn the 100/day budget. |
| Sync-in-progress guard | Client-side button-disable only | Server recency check / Redis lock | Decision 3. Single-admin hobby app; mirrors `isSyncingFootballData`. |

## Data Flow

    Admin click ──► POST /api/football-data/players/sync (Admin policy)
        │
        ▼
    SyncPlayerSquadsHandler  ── reads existing player rows (team-id cache)
        │   for each IncludedTeamName:
        │     /teams?search=  (skip if cached id)  ─► API-Sports
        │     /players/squads?team=               ─► API-Sports
        │     on 429: stop, report rate-limited, return partial
        ▼
    IExternalFootballPlayerRepository.ReplacePlayersAsync (delete+insert)
        ▼
    external_football_players  ◄── ApiSportsFootballPlayerSearchProvider.SearchAsync (DB read only)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Domain/Entities/ExternalFootballPlayer.cs` | Create | Entity mirroring `ExternalFootballTeam`: `ProviderName, ExternalId, Name, NormalizedName, TeamExternalId, TeamName, Position, PhotoUrl, SyncedAtUtc`; private ctor + `Create`. |
| `Application/Abstractions/IExternalFootballPlayerRepository.cs` | Create | `ReplacePlayersAsync`, `SearchAsync`, `GetTeamIdMapAsync`. |
| `Infrastructure/Persistence/Repositories/ExternalFootballPlayerRepository.cs` | Create | Delete+insert replace; `Contains` query + rank; team-id map read. |
| `Infrastructure/Persistence/Configurations/ExternalFootballPlayerConfiguration.cs` | Create | Table `external_football_players`, unique `(ProviderName,ExternalId)`, index on `NormalizedName`. |
| `Infrastructure/Persistence/Migrations/*_AddExternalFootballPlayers.cs` (+Designer +snapshot) | Create | Additive table mirroring `20260603033649_AddExternalFootballDataCache` shape. |
| `Infrastructure/Persistence/AppDbContext.cs` | Modify | Add `DbSet<ExternalFootballPlayer> ExternalFootballPlayers`. |
| `Application/Features/FootballData/SyncPlayerSquadsCommand.cs` | Create | Empty record. |
| `Application/Features/FootballData/SyncPlayerSquadsResultDto.cs` | Create | See Interfaces. |
| `Application/Features/FootballData/SyncPlayerSquadsHandler.cs` | Create | Team resolution + squad fetch + 429 abort + replace. Owns API-Sports calls. |
| `Application/Abstractions/IPlayerSquadProvider.cs` | Create | Thin port the handler depends on for API-Sports fetch (keeps Application clean of HttpClient). |
| `Infrastructure/ExternalFootball/ApiSportsPlayerSquadProvider.cs` | Create | Moves `BuildPlayerIndexAsync`/`ResolveNationalTeamAsync`/`SendAsync` here; throws/flags 429. |
| `Infrastructure/ExternalFootball/ApiSportsFootballPlayerSearchProvider.cs` | Modify | Drop HybridCache + index build + team resolution; `SearchAsync` delegates to repository. |
| `Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | Modify | Register player repo; rewire `IPlayerSearchProvider` without HybridCache; register `IPlayerSquadProvider`. |
| `WebApi/Endpoints/FootballDataEndpoints.cs` | Modify | Add `POST /players/sync` `[Authorize(Policy="Admin")]`. |
| `ApiSportsFootballOptions.cs` | Modify | `SquadCacheHours` removed (unused after cache drop). |
| `frontend/.../matches.service.ts` | Modify | `syncPlayerSquads(): Observable<SyncPlayerSquadsResult>` -> `POST /api/football-data/players/sync`. |
| `frontend/.../matches.models.ts` | Modify | `SyncPlayerSquadsResult` + `PlayerSquadSyncError` interfaces. |
| `frontend/.../admin-page.component.ts` | Modify | "Sync players" button, `isSyncingPlayers` signal, result/error display mirroring `syncFootballData()`. |

## Interfaces / Contracts

```csharp
public sealed record SyncPlayerSquadsCommand;

public sealed record PlayerSquadSyncErrorDto(string TeamName, string Message, bool RateLimited);

public sealed record SyncPlayerSquadsResultDto(
    string ProviderName,
    int TeamsProcessedCount,           // 0 => no teams configured (explicit, not silent)
    int PlayersIndexedCount,
    IReadOnlyList<PlayerSquadSyncErrorDto> Errors,
    DateTime SyncedAtUtc,
    bool NotConfigured);               // true => API key blank, no-op result, never throws

public interface IExternalFootballPlayerRepository
{
    Task ReplacePlayersAsync(string providerName, IReadOnlyList<ExternalFootballPlayer> players, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(string providerName, string normalizedQuery, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string,string>> GetTeamIdMapAsync(string providerName, CancellationToken ct = default); // teamName -> TeamExternalId
}

public interface IPlayerSquadProvider // Infrastructure-owned API-Sports port
{
    Task<string?> ResolveTeamIdAsync(string teamName, CancellationToken ct);   // throws ApiSportsRateLimitException on 429
    Task<IReadOnlyList<ExternalFootballPlayer>> GetSquadAsync(string providerName, string teamName, string teamExternalId, CancellationToken ct);
}
```

Handler control flow: missing key -> `NotConfigured:true`, zero teams. No teams configured -> `TeamsProcessedCount:0`. Per team: use cached id else resolve; catch `ApiSportsRateLimitException` -> add rate-limited error, break loop, replace what was gathered, return. Catch other `HttpRequestException` -> per-team error, continue.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | Handler 429 abort, not-configured, zero-teams, team-id-cache reuse, per-team error continue | Fake `IPlayerSquadProvider` + in-memory repo |
| Unit | Repository search rank (starts-with-word boost, alphabetical, take 10), `Contains` filter | SQLite/in-memory or fake list |
| Integration | `POST /players/sync` requires Admin; search reads persisted rows only | WebApplicationFactory |

## Migration / Rollout

One additive EF migration `AddExternalFootballPlayers` (new table only, no data backfill, no breaking change). Must match existing `external_football_*` naming/index conventions to avoid model-snapshot drift. First search after deploy returns empty until an admin runs the sync — acceptable and expected.

## Open Questions

- [ ] Confirm `IPlayerSquadProvider` port is desired vs. inlining HttpClient in the handler. Port keeps Application free of `HttpClient` (consistent with `IFootballDataProvider`); recommended. Non-blocking.

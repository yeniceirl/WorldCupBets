using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.FootballData;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ImportGroupStageFixturesHandlerTests
{
    [Fact]
    public async Task Imports_group_stage_fixtures_from_cached_snapshot()
    {
        var syncedAtUtc = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var matchRepository = new StubMatchRepository();
        var snapshotRepository = new StubExternalFootballDataRepository(CreateSnapshot(syncedAtUtc));

        var result = await ImportGroupStageFixturesHandler.Handle(
            new ImportGroupStageFixturesCommand(),
            snapshotRepository,
            matchRepository,
            CancellationToken.None);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Single(matchRepository.Matches);
        Assert.Equal("A", matchRepository.Matches[0].GroupName);
        Assert.Equal("Mexico", matchRepository.Matches[0].HomeTeamName);
        Assert.Equal("South Africa", matchRepository.Matches[0].AwayTeamName);
        Assert.Equal(new DateTime(2026, 6, 11, 19, 0, 0, DateTimeKind.Utc), matchRepository.Matches[0].StartsAtUtc);
        Assert.Equal("Estadio Azteca", matchRepository.Matches[0].Venue);
        Assert.Equal("worldcup26", matchRepository.Matches[0].SourceProvider);
        Assert.Equal("1", matchRepository.Matches[0].SourceMatchId);
    }

    [Fact]
    public async Task Import_is_idempotent_by_group_and_team_names()
    {
        var syncedAtUtc = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var existingMatch = Match.CreateGroupStageFixture(
            "A",
            "Mexico",
            "South Africa",
            new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc),
            "Old Venue",
            "other-provider",
            "old-id",
            syncedAtUtc.AddDays(-1));
        var matchRepository = new StubMatchRepository(existingMatch);
        var snapshotRepository = new StubExternalFootballDataRepository(CreateSnapshot(syncedAtUtc));

        var result = await ImportGroupStageFixturesHandler.Handle(
            new ImportGroupStageFixturesCommand(),
            snapshotRepository,
            matchRepository,
            CancellationToken.None);

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.UnsafeUpdateSkippedCount);
        Assert.Single(matchRepository.Matches);
        Assert.Equal(new DateTime(2026, 6, 11, 19, 0, 0, DateTimeKind.Utc), existingMatch.StartsAtUtc);
        Assert.Equal("Estadio Azteca", existingMatch.Venue);
        Assert.Equal("worldcup26", existingMatch.SourceProvider);
        Assert.Equal("1", existingMatch.SourceMatchId);
    }

    [Fact]
    public async Task Import_does_not_move_fixture_start_time_when_bets_exist()
    {
        var syncedAtUtc = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var originalStartsAtUtc = new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc);
        var existingMatch = Match.CreateGroupStageFixture(
            "A",
            "Mexico",
            "South Africa",
            originalStartsAtUtc,
            "Old Venue",
            "other-provider",
            "old-id",
            syncedAtUtc.AddDays(-1));
        SetEntityId(existingMatch, 20);
        var matchRepository = new StubMatchRepository([existingMatch], [existingMatch.Id]);
        var snapshotRepository = new StubExternalFootballDataRepository(CreateSnapshot(syncedAtUtc));

        var result = await ImportGroupStageFixturesHandler.Handle(
            new ImportGroupStageFixturesCommand(),
            snapshotRepository,
            matchRepository,
            CancellationToken.None);

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(1, result.UnsafeUpdateSkippedCount);
        Assert.Equal(originalStartsAtUtc, existingMatch.StartsAtUtc);
        Assert.Equal("Estadio Azteca", existingMatch.Venue);
        Assert.Equal("worldcup26", existingMatch.SourceProvider);
        Assert.Equal("1", existingMatch.SourceMatchId);
    }

    private static ExternalFootballSnapshot CreateSnapshot(DateTime syncedAtUtc)
    {
        return new ExternalFootballSnapshot(
            [],
            [new ExternalFootballStadiumDto("1", "Estadio Azteca", null, "Mexico City", "Mexico", null, null)],
            [],
            [
                new ExternalFootballMatchDto(
                    "1",
                    "1",
                    "2",
                    "Mexico",
                    "South Africa",
                    null,
                    null,
                    "A",
                    "1",
                    "06/11/2026 13:00",
                    "1",
                    false,
                    "notstarted",
                    "group",
                    0,
                    0)
            ],
            syncedAtUtc);
    }

    private sealed class StubExternalFootballDataRepository(ExternalFootballSnapshot snapshot) : IExternalFootballDataRepository
    {
        public Task ReplaceSnapshotAsync(string providerName, ExternalFootballSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExternalFootballSnapshot?> GetSnapshotAsync(string providerName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ExternalFootballSnapshot?>(snapshot);
        }

    }

    private static void SetEntityId(Entity entity, int id)
    {
        var property = typeof(Entity).GetProperty(nameof(Entity.Id), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        property!.SetValue(entity, id);
    }

    private sealed class StubMatchRepository : IMatchRepository
    {
        private readonly HashSet<int> matchIdsWithBets;

        public StubMatchRepository(params Match[] seeded)
            : this(seeded, [])
        {
        }

        public StubMatchRepository(IEnumerable<Match> seeded, IEnumerable<int> matchIdsWithBets)
        {
            Matches = [.. seeded];
            this.matchIdsWithBets = matchIdsWithBets.ToHashSet();
        }

        public List<Match> Matches { get; }

        public Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Match>>(Matches.ToArray());
        }

        public Task<IReadOnlyList<Match>> ListGroupStageFixturesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Match>>(Matches.Where(match => match.Phase == MatchPhase.GroupStage).ToArray());
        }

        public Task<IReadOnlySet<int>> ListMatchIdsWithBetsAsync(IEnumerable<int> matchIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<int>>(matchIds.Where(matchIdsWithBets.Contains).ToHashSet());
        }

        public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Match?> GetByIdForSettlementAsync(int matchId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Match>> ListPendingResultSettlementAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Match>>(Matches.Where(match => match.StartsAtUtc <= nowUtc).ToArray());
        }

        public Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Match match, CancellationToken cancellationToken = default)
        {
            Matches.Add(match);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

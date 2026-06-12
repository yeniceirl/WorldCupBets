using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Matches;

public sealed class SyncMatchResultsHandler
{
    private const string WorldCup26ProviderName = "worldcup26";

    public static async Task<SyncMatchResultsResultDto> Handle(
        SyncMatchResultsCommand command,
        IFootballDataProvider footballDataProvider,
        IOfficialMatchResultProvider officialMatchResultProvider,
        IMatchRepository matchRepository,
        IMatchBetRepository matchBetRepository,
        ITournamentSettlementRepository tournamentSettlementRepository,
        IUserRepository userRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(GetProviderNameIfConfigured(officialMatchResultProvider)))
        {
            return new SyncMatchResultsResultDto(false, null, 0, 0, 0, 0, 0, []);
        }

        var nowUtc = DateTime.UtcNow;
        var matches = command.MatchId.HasValue
            ? await GetTargetedMatchesAsync(command.MatchId.Value, matchRepository, nowUtc, cancellationToken)
            : await matchRepository.ListPendingResultSettlementAsync(nowUtc, cancellationToken);

        if (matches.Count == 0)
        {
            return new SyncMatchResultsResultDto(true, null, 0, 0, 0, 0, 0, []);
        }

        var snapshot = await footballDataProvider.GetSnapshotAsync(cancellationToken);
        var matchesByExternalId = snapshot.Matches
            .Where(match => !string.IsNullOrWhiteSpace(match.ExternalId))
            .ToDictionary(match => match.ExternalId, StringComparer.OrdinalIgnoreCase);
        var items = new List<MatchResultSyncItemDto>();
        var confirmedCount = 0;
        var alreadySettledCount = 0;
        var deferredCount = 0;
        var failedCount = 0;

        foreach (var match in matches)
        {
            var syncOutcome = await SyncMatchAsync(
                match,
                matchesByExternalId,
                officialMatchResultProvider,
                matchRepository,
                matchBetRepository,
                tournamentSettlementRepository,
                userRepository,
                transactionFactory,
                cancellationToken);

            items.Add(syncOutcome.Item);
            confirmedCount += syncOutcome.ConfirmedCount;
            alreadySettledCount += syncOutcome.AlreadySettledCount;
            deferredCount += syncOutcome.DeferredCount;
            failedCount += syncOutcome.FailedCount;
        }

        return new SyncMatchResultsResultDto(
            ApiSportsConfigured: true,
            snapshot.SyncedAtUtc,
            matches.Count,
            confirmedCount,
            alreadySettledCount,
            deferredCount,
            failedCount,
            items);
    }

    private static async Task<IReadOnlyList<WorldCupBets.Domain.Entities.Match>> GetTargetedMatchesAsync(
        int matchId,
        IMatchRepository matchRepository,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForSettlementAsync(matchId, cancellationToken);
        return match is null || match.SettledAtUtc.HasValue || match.IsBettingOpenAt(nowUtc) ? [] : [match];
    }

    private static async Task<MatchSyncOutcome> SyncMatchAsync(
        WorldCupBets.Domain.Entities.Match match,
        IReadOnlyDictionary<string, ExternalFootballMatchDto> matchesByExternalId,
        IOfficialMatchResultProvider officialMatchResultProvider,
        IMatchRepository matchRepository,
        IMatchBetRepository matchBetRepository,
        ITournamentSettlementRepository tournamentSettlementRepository,
        IUserRepository userRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        if (match.SettledAtUtc.HasValue)
        {
            return MatchSyncOutcome.AlreadySettled(match.Id, "Match was already settled.");
        }

        if (string.IsNullOrWhiteSpace(match.SourceMatchId) || !string.Equals(match.SourceProvider, WorldCup26ProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return MatchSyncOutcome.Failed(match.Id, "Missing worldcup26 source identifier.");
        }

        if (!matchesByExternalId.TryGetValue(match.SourceMatchId, out var externalMatch))
        {
            return MatchSyncOutcome.Failed(match.Id, "worldcup26 cache did not return the fixture.");
        }

        if (!IsCheapSignalFinished(externalMatch))
        {
            return MatchSyncOutcome.Deferred(match.Id, "worldcup26 still does not mark the match as finished.");
        }

        try
        {
            var lookup = BuildLookup(match, externalMatch);
            var confirmation = await officialMatchResultProvider.TryConfirmAsync(lookup, cancellationToken);
            if (confirmation is null)
            {
                return MatchSyncOutcome.Deferred(match.Id, "API-Sports did not confirm a final result yet.");
            }

            var officialResult = ToOfficialResult(confirmation.HomeScore, confirmation.AwayScore);
            var recordResult = await RecordMatchResultHandler.Handle(
                new RecordMatchResultCommand(match.Id, officialResult),
                matchRepository,
                matchBetRepository,
                tournamentSettlementRepository,
                userRepository,
                transactionFactory,
                cancellationToken);

            if (recordResult.IsFailure)
            {
                return MatchSyncOutcome.Failed(match.Id, recordResult.Error?.Message ?? "Unable to record the official result.");
            }

            return recordResult.Value!.WasAlreadySettled
                ? MatchSyncOutcome.AlreadySettled(match.Id, "Official result was already settled.")
                : MatchSyncOutcome.Confirmed(match.Id, $"Recorded official {confirmation.HomeScore}-{confirmation.AwayScore}.");
        }
        catch (ApiSportsRateLimitException exception)
        {
            return MatchSyncOutcome.Deferred(match.Id, exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return MatchSyncOutcome.Failed(match.Id, exception.Message);
        }
    }

    private static string? GetProviderNameIfConfigured(IOfficialMatchResultProvider officialMatchResultProvider)
    {
        return officialMatchResultProvider.ProviderName;
    }

    private static bool IsCheapSignalFinished(ExternalFootballMatchDto externalMatch)
    {
        return externalMatch.IsFinished
            && externalMatch.HomeScore.HasValue
            && externalMatch.AwayScore.HasValue;
    }

    private static OfficialMatchResultLookup BuildLookup(Match match, ExternalFootballMatchDto externalMatch)
    {
        var scheduledDate = TryParseLocalDate(externalMatch.LocalDateText)
            ?? DateOnly.FromDateTime(match.StartsAtUtc);

        return new OfficialMatchResultLookup(
            match.HomeTeamName,
            match.AwayTeamName,
            match.StartsAtUtc,
            scheduledDate);
    }

    private static DateOnly? TryParseLocalDate(string localDateText)
    {
        return DateTime.TryParse(localDateText, out var parsed)
            ? DateOnly.FromDateTime(parsed)
            : null;
    }

    private static MatchBetSelection ToOfficialResult(int homeScore, int awayScore)
    {
        if (homeScore > awayScore)
        {
            return MatchBetSelection.Home;
        }

        if (awayScore > homeScore)
        {
            return MatchBetSelection.Away;
        }

        return MatchBetSelection.Draw;
    }

    private sealed record MatchSyncOutcome(
        MatchResultSyncItemDto Item,
        int ConfirmedCount,
        int AlreadySettledCount,
        int DeferredCount,
        int FailedCount)
    {
        public static MatchSyncOutcome Confirmed(int matchId, string message)
        {
            return new MatchSyncOutcome(new MatchResultSyncItemDto(matchId, "confirmed", message), 1, 0, 0, 0);
        }

        public static MatchSyncOutcome AlreadySettled(int matchId, string message)
        {
            return new MatchSyncOutcome(new MatchResultSyncItemDto(matchId, "already_settled", message), 0, 1, 0, 0);
        }

        public static MatchSyncOutcome Deferred(int matchId, string message)
        {
            return new MatchSyncOutcome(new MatchResultSyncItemDto(matchId, "deferred", message), 0, 0, 1, 0);
        }

        public static MatchSyncOutcome Failed(int matchId, string message)
        {
            return new MatchSyncOutcome(new MatchResultSyncItemDto(matchId, "failed", message), 0, 0, 0, 1);
        }
    }
}

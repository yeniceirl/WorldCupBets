using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Features.Admin;

public sealed record GetAuditBalanceSummaryQuery();

public sealed record GetAuditUserSubledgerQuery(int UserId);

public sealed class GetAuditBalanceSummaryHandler
{
    public static async Task<AuditBalanceSummaryDto> Handle(
        GetAuditBalanceSummaryQuery query,
        IAuditReadRepository auditReadRepository,
        CancellationToken cancellationToken)
    {
        var users = await auditReadRepository.ListUsersAsync(cancellationToken);
        var matchBets = await auditReadRepository.ListMatchBetsAsync(null, cancellationToken);
        var challengePositions = await auditReadRepository.ListChallengePositionsAsync(null, cancellationToken);
        var tournamentPicks = await auditReadRepository.ListTournamentPicksAsync(null, cancellationToken);
        var settlement = await auditReadRepository.GetTournamentSettlementAsync(cancellationToken);

        var ledgerItemsByUser = AuditReportCalculator.BuildLedgerItemsByUser(matchBets, challengePositions, tournamentPicks, settlement);
        var rows = users
            .Select(user => AuditReportCalculator.BuildSummaryRow(user, ledgerItemsByUser.GetValueOrDefault(user.UserId, [])))
            .OrderByDescending(row => row.DerivedTotalBalanceCc)
            .ThenBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AuditBalanceSummaryDto(AuditReportCalculator.Metadata, rows);
    }
}

public sealed class GetAuditUserSubledgerHandler
{
    public static async Task<AuditUserSubledgerDto?> Handle(
        GetAuditUserSubledgerQuery query,
        IAuditReadRepository auditReadRepository,
        CancellationToken cancellationToken)
    {
        var user = await auditReadRepository.GetUserAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var matchBets = await auditReadRepository.ListMatchBetsAsync(query.UserId, cancellationToken);
        var challengePositions = await auditReadRepository.ListChallengePositionsAsync(query.UserId, cancellationToken);
        var tournamentPicks = await auditReadRepository.ListTournamentPicksAsync(query.UserId, cancellationToken);
        var settlement = await auditReadRepository.GetTournamentSettlementAsync(cancellationToken);

        var ledgerItems = AuditReportCalculator.BuildLedgerItems(matchBets, challengePositions, tournamentPicks, settlement)
            .OrderByDescending(item => item.PlacedAtUtc)
            .ThenBy(item => item.SourceType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AuditUserSubledgerDto(
            AuditReportCalculator.Metadata,
            AuditReportCalculator.BuildSummaryRow(user, ledgerItems),
            ledgerItems);
    }
}

file static class AuditReportCalculator
{
    private const decimal CopaCoinScale = 100m;

    public static readonly AuditReportMetadataDto Metadata = new(
        AuditReportLabels.DerivedCurrentState,
        AuditReportLabels.DerivedCurrentStateDescription,
        IsDerivedFromCurrentState: true);

    public static IReadOnlyDictionary<int, IReadOnlyList<AuditLedgerItemDto>> BuildLedgerItemsByUser(
        IReadOnlyList<AuditMatchBetReadModel> matchBets,
        IReadOnlyList<AuditChallengePositionReadModel> challengePositions,
        IReadOnlyList<AuditTournamentPickReadModel> tournamentPicks,
        AuditTournamentSettlementReadModel? settlement)
    {
        return BuildRawLedgerItems(matchBets, challengePositions, tournamentPicks, settlement)
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AuditLedgerItemDto>)group.Select(item => item.Item).ToArray());
    }

    public static IReadOnlyList<AuditLedgerItemDto> BuildLedgerItems(
        IReadOnlyList<AuditMatchBetReadModel> matchBets,
        IReadOnlyList<AuditChallengePositionReadModel> challengePositions,
        IReadOnlyList<AuditTournamentPickReadModel> tournamentPicks,
        AuditTournamentSettlementReadModel? settlement)
    {
        return BuildRawLedgerItems(matchBets, challengePositions, tournamentPicks, settlement)
            .Select(item => item.Item)
            .ToArray();
    }

    private static IReadOnlyList<(int UserId, AuditLedgerItemDto Item)> BuildRawLedgerItems(
        IReadOnlyList<AuditMatchBetReadModel> matchBets,
        IReadOnlyList<AuditChallengePositionReadModel> challengePositions,
        IReadOnlyList<AuditTournamentPickReadModel> tournamentPicks,
        AuditTournamentSettlementReadModel? settlement)
    {
        var items = new List<(int UserId, AuditLedgerItemDto Item)>();
        items.AddRange(BuildMatchBetItems(matchBets));
        items.AddRange(BuildChallengeItems(challengePositions));
        items.AddRange(BuildTournamentPickItems(tournamentPicks, settlement));
        return items;
    }

    public static AuditBalanceSummaryRowDto BuildSummaryRow(AuditUserReadModel user, IReadOnlyList<AuditLedgerItemDto> items)
    {
        var pendingTotalCc = items.Sum(item => item.PendingAmountCc);
        var wonTotalCc = items.Where(item => string.Equals(item.Status, "won", StringComparison.OrdinalIgnoreCase)).Sum(item => item.CreditAmountCc);
        var lostTotalCc = items.Sum(item => item.LossAmountCc);

        return new AuditBalanceSummaryRowDto(
            user.UserId,
            user.DisplayName,
            user.Email,
            user.CurrentBalanceCc,
            pendingTotalCc,
            user.CurrentBalanceCc + pendingTotalCc,
            wonTotalCc,
            lostTotalCc,
            user.RescueDebtCc,
            user.RescueCount);
    }

    private static IEnumerable<(int UserId, AuditLedgerItemDto Item)> BuildMatchBetItems(IReadOnlyList<AuditMatchBetReadModel> matchBets)
    {
        foreach (var group in matchBets.GroupBy(matchBet => matchBet.MatchId))
        {
            var settled = group.First().SettledAtUtc.HasValue && group.First().OfficialResult.HasValue;
            var officialResult = group.First().OfficialResult;
            var winners = settled && officialResult.HasValue
                ? group.Where(matchBet => matchBet.Selection == officialResult.Value).ToArray()
                : [];
            var losers = settled && officialResult.HasValue
                ? group.Where(matchBet => matchBet.Selection != officialResult.Value).ToArray()
                : [];
            var losingPoolCc = losers.Sum(matchBet => matchBet.StakeAmountCc);
            var profitSharePerWinnerCc = winners.Length == 0 ? 0m : RoundDownToCents(losingPoolCc / winners.Length);

            foreach (var matchBet in group)
            {
                var metadata = new[]
                {
                    new AuditLedgerMetadataItemDto("Selection", matchBet.Selection.ToString()),
                    new AuditLedgerMetadataItemDto("Match", $"{matchBet.HomeTeamName} vs {matchBet.AwayTeamName}")
                };

                if (!settled || officialResult is null)
                {
                    yield return (matchBet.UserId, new AuditLedgerItemDto(
                        "match_bet",
                        matchBet.MatchBetId,
                        $"{matchBet.HomeTeamName} vs {matchBet.AwayTeamName}",
                        matchBet.PlacedAtUtc,
                        matchBet.StakeAmountCc,
                        "pending",
                        0m,
                        0m,
                        matchBet.StakeAmountCc,
                        "Waiting for official match result",
                        metadata));
                    continue;
                }

                if (winners.Length == 0)
                {
                    var refundCc = RoundDownToCents(matchBet.StakeAmountCc / 2m);
                    yield return (matchBet.UserId, new AuditLedgerItemDto(
                        "match_bet",
                        matchBet.MatchBetId,
                        $"{matchBet.HomeTeamName} vs {matchBet.AwayTeamName}",
                        matchBet.PlacedAtUtc,
                        matchBet.StakeAmountCc,
                        "refunded",
                        refundCc,
                        matchBet.StakeAmountCc - refundCc,
                        0m,
                        null,
                        metadata));
                    continue;
                }

                if (matchBet.Selection == officialResult.Value)
                {
                    yield return (matchBet.UserId, new AuditLedgerItemDto(
                        "match_bet",
                        matchBet.MatchBetId,
                        $"{matchBet.HomeTeamName} vs {matchBet.AwayTeamName}",
                        matchBet.PlacedAtUtc,
                        matchBet.StakeAmountCc,
                        "won",
                        matchBet.StakeAmountCc + profitSharePerWinnerCc,
                        0m,
                        0m,
                        null,
                        metadata));
                    continue;
                }

                yield return (matchBet.UserId, new AuditLedgerItemDto(
                    "match_bet",
                    matchBet.MatchBetId,
                    $"{matchBet.HomeTeamName} vs {matchBet.AwayTeamName}",
                    matchBet.PlacedAtUtc,
                    matchBet.StakeAmountCc,
                    "lost",
                    0m,
                    matchBet.StakeAmountCc,
                    0m,
                    null,
                    metadata));
            }
        }
    }

    private static IEnumerable<(int UserId, AuditLedgerItemDto Item)> BuildChallengeItems(IReadOnlyList<AuditChallengePositionReadModel> positions)
    {
        foreach (var position in positions)
        {
            var metadata = new[]
            {
                new AuditLedgerMetadataItemDto("Side", position.Side == MatchChallengeSide.Creator ? position.CreatorSideText : position.TakerSideText),
                new AuditLedgerMetadataItemDto("Match", $"{position.HomeTeamName} vs {position.AwayTeamName}")
            };

            switch (position.Status)
            {
                case MatchChallengeStatus.Open:
                    yield return (position.UserId, new AuditLedgerItemDto(
                        "challenge",
                        position.MatchChallengeId,
                        position.ClaimText,
                        position.EscrowedAtUtc,
                        position.StakeAmountCc,
                        "pending",
                        0m,
                        0m,
                        position.StakeAmountCc,
                        "Waiting for another bettor to accept",
                        metadata));
                    break;
                case MatchChallengeStatus.Matched:
                    yield return (position.UserId, new AuditLedgerItemDto(
                        "challenge",
                        position.MatchChallengeId,
                        position.ClaimText,
                        position.EscrowedAtUtc,
                        position.StakeAmountCc,
                        "pending",
                        0m,
                        0m,
                        position.StakeAmountCc,
                        "Waiting for admin challenge settlement",
                        metadata));
                    break;
                case MatchChallengeStatus.Settled when position.WinnerSide == position.Side:
                    yield return (position.UserId, new AuditLedgerItemDto(
                        "challenge",
                        position.MatchChallengeId,
                        position.ClaimText,
                        position.EscrowedAtUtc,
                        position.StakeAmountCc,
                        "won",
                        position.TotalStakeAmountCc,
                        0m,
                        0m,
                        null,
                        metadata));
                    break;
                case MatchChallengeStatus.Settled:
                    yield return (position.UserId, new AuditLedgerItemDto(
                        "challenge",
                        position.MatchChallengeId,
                        position.ClaimText,
                        position.EscrowedAtUtc,
                        position.StakeAmountCc,
                        "lost",
                        0m,
                        position.StakeAmountCc,
                        0m,
                        null,
                        metadata));
                    break;
                case MatchChallengeStatus.Voided:
                case MatchChallengeStatus.Expired:
                    yield return (position.UserId, new AuditLedgerItemDto(
                        "challenge",
                        position.MatchChallengeId,
                        position.ClaimText,
                        position.EscrowedAtUtc,
                        position.StakeAmountCc,
                        "refunded",
                        position.StakeAmountCc,
                        0m,
                        0m,
                        null,
                        metadata));
                    break;
            }
        }
    }

    private static IEnumerable<(int UserId, AuditLedgerItemDto Item)> BuildTournamentPickItems(
        IReadOnlyList<AuditTournamentPickReadModel> picks,
        AuditTournamentSettlementReadModel? settlement)
    {
        var championPicks = picks.Where(pick => pick.Category == TournamentPickCategory.Champion).ToArray();
        var championWinners = settlement?.ChampionSettledAtUtc is not null
            ? championPicks.Where(pick => string.Equals(pick.SelectedText, settlement.ChampionTeamName, StringComparison.OrdinalIgnoreCase)).ToArray()
            : [];
        var championLosers = settlement?.ChampionSettledAtUtc is not null
            ? championPicks.Where(pick => !string.Equals(pick.SelectedText, settlement.ChampionTeamName, StringComparison.OrdinalIgnoreCase)).ToArray()
            : [];
        var championLosingPoolCc = championLosers.Sum(pick => pick.StakeAmountCc);
        var championProfitShareCc = championWinners.Length == 0 || settlement is null
            ? 0m
            : RoundDownToCents((championLosingPoolCc + settlement.ChampionJackpotCc) / championWinners.Length);

        foreach (var pick in picks)
        {
            var metadata = new[]
            {
                new AuditLedgerMetadataItemDto("Selection", pick.SelectedText),
                new AuditLedgerMetadataItemDto("Category", pick.Category.ToString())
            };

            if (pick.Category == TournamentPickCategory.Champion)
            {
                if (settlement?.ChampionSettledAtUtc is null)
                {
                    yield return (pick.UserId, new AuditLedgerItemDto(
                        "tournament_pick",
                        pick.TournamentPickId,
                        "Champion pick",
                        pick.PlacedAtUtc,
                        pick.StakeAmountCc,
                        "pending",
                        0m,
                        0m,
                        pick.StakeAmountCc,
                        "Waiting for champion settlement",
                        metadata));
                    continue;
                }

                if (string.Equals(pick.SelectedText, settlement.ChampionTeamName, StringComparison.OrdinalIgnoreCase))
                {
                    yield return (pick.UserId, new AuditLedgerItemDto(
                        "tournament_pick",
                        pick.TournamentPickId,
                        "Champion pick",
                        pick.PlacedAtUtc,
                        pick.StakeAmountCc,
                        "won",
                        pick.StakeAmountCc + championProfitShareCc,
                        0m,
                        0m,
                        null,
                        metadata));
                    continue;
                }

                yield return (pick.UserId, new AuditLedgerItemDto(
                    "tournament_pick",
                    pick.TournamentPickId,
                    "Champion pick",
                    pick.PlacedAtUtc,
                    pick.StakeAmountCc,
                    "lost",
                    0m,
                    pick.StakeAmountCc,
                    0m,
                    null,
                    metadata));
                continue;
            }

            yield return (pick.UserId, new AuditLedgerItemDto(
                "tournament_pick",
                pick.TournamentPickId,
                pick.Category == TournamentPickCategory.BestPlayer ? "Best player pick" : "Top scorer pick",
                pick.PlacedAtUtc,
                pick.StakeAmountCc,
                "pending",
                0m,
                0m,
                pick.StakeAmountCc,
                "Waiting for tournament special settlement",
                metadata));
        }
    }

    private static decimal RoundDownToCents(decimal amountCc)
    {
        return Math.Floor(amountCc * CopaCoinScale) / CopaCoinScale;
    }
}

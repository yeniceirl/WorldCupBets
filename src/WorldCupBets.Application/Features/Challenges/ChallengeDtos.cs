using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Features.Challenges;

public sealed record ChallengeDto(
    int Id,
    int MatchId,
    ChallengeMatchDto Match,
    string ClaimText,
    decimal StakeAmountCc,
    string Status,
    string? WinnerSide,
    DateTime CreatedAtUtc,
    DateTime? MatchedAtUtc,
    DateTime? SettledAtUtc,
    DateTime? VoidedAtUtc,
    DateTime? ExpiredAtUtc,
    IReadOnlyList<ChallengePositionDto> Positions)
{
    public static ChallengeDto FromEntity(MatchChallenge challenge, Match? match = null)
    {
        var challengeMatch = match is null
            ? ChallengeMatchDto.FromEntity(challenge)
            : ChallengeMatchDto.FromEntity(match);
        return new ChallengeDto(
            challenge.Id,
            challenge.MatchId,
            challengeMatch,
            challenge.ClaimText,
            challenge.StakeAmountCc,
            challenge.Status.ToString(),
            challenge.WinnerSide?.ToString(),
            challenge.CreatedAtUtc,
            challenge.MatchedAtUtc,
            challenge.SettledAtUtc,
            challenge.VoidedAtUtc,
            challenge.ExpiredAtUtc,
            challenge.Positions
                .OrderBy(position => position.Side)
                .Select(position => new ChallengePositionDto(
                    position.UserId,
                    position.User?.DisplayName ?? string.Empty,
                    position.Side.ToString(),
                    position.StakeAmountCc,
                    position.EscrowedAtUtc))
                .ToArray());
    }
}

public sealed record ChallengeMatchDto(
    int Id,
    string Stage,
    string HomeTeamName,
    string AwayTeamName,
    DateTime StartsAtUtc,
    string Venue,
    string? OfficialResult,
    bool IsSettled)
{
    public static ChallengeMatchDto FromEntity(Match match)
    {
        return new ChallengeMatchDto(
            match.Id,
            match.GetStageLabel(),
            match.HomeTeamName,
            match.AwayTeamName,
            match.StartsAtUtc,
            match.Venue,
            match.OfficialResult?.ToString(),
            match.SettledAtUtc.HasValue);
    }

    public static ChallengeMatchDto FromEntity(MatchChallenge challenge)
    {
        if (challenge.Match is not null)
        {
            return FromEntity(challenge.Match);
        }

        return new ChallengeMatchDto(
            challenge.MatchId,
            string.Empty,
            string.Empty,
            string.Empty,
            DateTime.MinValue,
            string.Empty,
            null,
            false);
    }
}

public sealed record ChallengePositionDto(
    int UserId,
    string DisplayName,
    string Side,
    decimal StakeAmountCc,
    DateTime EscrowedAtUtc);

public sealed record ChallengeMutationResultDto(ChallengeDto Challenge, decimal CurrentBalanceCc);

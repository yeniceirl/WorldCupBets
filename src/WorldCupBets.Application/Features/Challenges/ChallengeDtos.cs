using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Features.Challenges;

public sealed record ChallengeDto(
    int Id,
    int MatchId,
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
    public static ChallengeDto FromEntity(MatchChallenge challenge)
    {
        return new ChallengeDto(
            challenge.Id,
            challenge.MatchId,
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

public sealed record ChallengePositionDto(
    int UserId,
    string DisplayName,
    string Side,
    decimal StakeAmountCc,
    DateTime EscrowedAtUtc);

public sealed record ChallengeMutationResultDto(ChallengeDto Challenge, decimal CurrentBalanceCc);

using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Features.Challenges;

public sealed record ListChallengesQuery(int MatchId);

public sealed record CreateChallengeCommand(
    int CreatorUserId,
    int MatchId,
    string ClaimText,
    decimal StakeAmountCc);

public sealed record AcceptChallengeCommand(int ChallengeId, int TakerUserId);

public sealed record CancelChallengeCommand(int ChallengeId, int CreatorUserId);

public sealed record SettleChallengeCommand(int ChallengeId, MatchChallengeSide WinnerSide);

public sealed record VoidChallengeCommand(int ChallengeId);

public sealed record ExpireChallengeCommand(int ChallengeId);

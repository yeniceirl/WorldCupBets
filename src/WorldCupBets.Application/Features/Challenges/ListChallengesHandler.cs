using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Challenges;

public sealed class ListChallengesHandler
{
    public static async Task<IReadOnlyList<ChallengeDto>> Handle(
        ListChallengesQuery query,
        IMatchChallengeRepository challengeRepository,
        CancellationToken cancellationToken)
    {
        var challenges = await challengeRepository.ListByMatchAsync(query.MatchId, cancellationToken);
        return challenges.Select(challenge => ChallengeDto.FromEntity(challenge)).ToArray();
    }
}

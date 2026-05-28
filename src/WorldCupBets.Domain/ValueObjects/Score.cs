using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.ValueObjects;

public sealed record Score
{
    private Score(int home, int away)
    {
        Home = home;
        Away = away;
    }

    public int Home { get; }

    public int Away { get; }

    public static Result<Score> Create(int home, int away)
    {
        if (home < 0 || away < 0)
        {
            return Result<Score>.Failure(new Error("score.invalid", "Score values must be zero or greater."));
        }

        return Result<Score>.Success(new Score(home, away));
    }
}

using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class TournamentSettlement : Entity
{
    public const int SingletonId = 1;

    private TournamentSettlement()
    {
    }

    private TournamentSettlement(int id)
    {
        Id = id;
    }

    public int ChampionJackpotCc { get; private set; }

    public string? ChampionTeamName { get; private set; }

    public DateTime? ChampionSettledAtUtc { get; private set; }

    public int UndistributedJackpotCc { get; private set; }

    public int Version { get; private set; }

    public static TournamentSettlement CreateSingleton()
    {
        return new TournamentSettlement(SingletonId);
    }

    public void AddChampionJackpot(int amountCc)
    {
        if (amountCc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountCc), "Amount must be greater than zero.");
        }

        ChampionJackpotCc = checked(ChampionJackpotCc + amountCc);
    }

    public void MarkChampionSettled(string championTeamName, DateTime settledAtUtc, int undistributedJackpotCc)
    {
        if (string.IsNullOrWhiteSpace(championTeamName))
        {
            throw new ArgumentException("Champion team name is required.", nameof(championTeamName));
        }

        if (undistributedJackpotCc < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(undistributedJackpotCc), "Undistributed jackpot cannot be negative.");
        }

        if (ChampionSettledAtUtc.HasValue)
        {
            return;
        }

        ChampionTeamName = championTeamName.Trim();
        ChampionSettledAtUtc = settledAtUtc;
        UndistributedJackpotCc = undistributedJackpotCc;
    }
}

using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class TournamentPick : Entity
{
    private TournamentPick()
    {
    }

    private TournamentPick(
        int userId,
        TournamentPickCategory category,
        string selectedText,
        string? externalId,
        decimal stakeAmountCc,
        DateTime placedAtUtc)
    {
        UserId = userId;
        Category = category;
        SelectedText = selectedText;
        ExternalId = externalId;
        StakeAmountCc = stakeAmountCc;
        PlacedAtUtc = placedAtUtc;
    }

    public int UserId { get; private set; }

    public User? User { get; private set; }

    public TournamentPickCategory Category { get; private set; }

    public string SelectedText { get; private set; } = string.Empty;

    public string? ExternalId { get; private set; }

    public decimal StakeAmountCc { get; private set; }

    public DateTime PlacedAtUtc { get; private set; }

    public static TournamentPick CreateChampion(int userId, string teamName, decimal stakeAmountCc, DateTime placedAtUtc)
    {
        return new TournamentPick(userId, TournamentPickCategory.Champion, teamName.Trim(), null, stakeAmountCc, placedAtUtc);
    }

    public void ChangeChampionSelection(string teamName)
    {
        if (Category != TournamentPickCategory.Champion)
        {
            throw new InvalidOperationException("Only champion picks can change their team selection.");
        }

        SelectedText = teamName.Trim();
    }

    public void ChangePlayerSelection(string playerName, string? externalPlayerId)
    {
        if (Category is not TournamentPickCategory.BestPlayer and not TournamentPickCategory.TopScorer)
        {
            throw new InvalidOperationException("Only player picks can change their player selection.");
        }

        SelectedText = playerName.Trim();
        ExternalId = externalPlayerId?.Trim();
    }

    public static TournamentPick CreatePlayer(
        int userId,
        TournamentPickCategory category,
        string playerName,
        string? externalPlayerId,
        decimal stakeAmountCc,
        DateTime placedAtUtc)
    {
        if (category is not TournamentPickCategory.BestPlayer and not TournamentPickCategory.TopScorer)
        {
            throw new ArgumentOutOfRangeException(nameof(category), category, "Player picks must use a player category.");
        }

        return new TournamentPick(userId, category, playerName.Trim(), externalPlayerId?.Trim(), stakeAmountCc, placedAtUtc);
    }
}

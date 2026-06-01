namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceChampionBetCommand(
    int UserId,
    string TeamName);

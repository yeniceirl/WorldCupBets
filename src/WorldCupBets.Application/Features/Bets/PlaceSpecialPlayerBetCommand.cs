using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceSpecialPlayerBetCommand(
    int UserId,
    TournamentPickCategory Category,
    string PlayerName,
    string? ExternalPlayerId);

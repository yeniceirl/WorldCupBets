namespace WorldCupBets.Application.Features.Matches;

public sealed record MatchListItemDto(
    int Id,
    string Stage,
    string HomeTeamName,
    string AwayTeamName,
    string? GroupName,
    DateTime StartsAtUtc,
    DateTime BettingClosesAtUtc,
    bool IsBettingOpen,
    int StakeAmountCc,
    string Venue,
    string? CurrentUserBetSelection,
    string? OfficialResult,
    bool IsSettled,
    DateTime? SettledAtUtc);

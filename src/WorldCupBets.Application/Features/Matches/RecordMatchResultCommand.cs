using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Features.Matches;

public sealed record RecordMatchResultCommand(
    int MatchId,
    MatchBetSelection OfficialResult);

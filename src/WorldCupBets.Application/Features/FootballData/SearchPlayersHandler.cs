using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Application.Features.FootballData;

public sealed class SearchPlayersHandler
{
    public static Task<IReadOnlyList<PlayerSearchResultDto>> Handle(
        SearchPlayersQuery query,
        IPlayerSearchProvider playerSearchProvider,
        CancellationToken cancellationToken)
    {
        return playerSearchProvider.SearchAsync(query.Query, cancellationToken);
    }
}

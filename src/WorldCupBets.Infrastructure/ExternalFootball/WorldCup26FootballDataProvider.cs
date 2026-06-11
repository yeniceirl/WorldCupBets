using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class WorldCup26FootballDataProvider(HttpClient httpClient, ExternalFootballDataOptions options) : IFootballDataProvider
{
    public string ProviderName => options.Provider;

    public async Task<ExternalFootballSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var syncedAtUtc = DateTime.UtcNow;
        var teamsTask = httpClient.GetFromJsonAsync<TeamsResponse>("/get/teams", cancellationToken);
        var stadiumsTask = httpClient.GetFromJsonAsync<StadiumsResponse>("/get/stadiums", cancellationToken);
        var gamesTask = httpClient.GetFromJsonAsync<GamesResponse>("/get/games", cancellationToken);

        await Task.WhenAll(teamsTask, stadiumsTask, gamesTask);

        var teams = (teamsTask.Result?.Teams ?? []).Select(ToDto).ToArray();
        var stadiums = (stadiumsTask.Result?.Stadiums ?? []).Select(ToDto).ToArray();
        var matches = (gamesTask.Result?.Games ?? []).Select(ToDto).ToArray();
        var groupStandings = ExternalFootballStandingsCalculator.Calculate(teams, matches);

        return new ExternalFootballSnapshot(
            teams,
            stadiums,
            groupStandings,
            matches,
            syncedAtUtc);
    }

    private static ExternalFootballTeamDto ToDto(WorldCup26Team team)
    {
        return new ExternalFootballTeamDto(
            team.Id ?? string.Empty,
            team.NameEn ?? string.Empty,
            team.FifaCode ?? string.Empty,
            team.Iso2,
            team.Groups,
            team.Flag);
    }

    private static ExternalFootballStadiumDto ToDto(WorldCup26Stadium stadium)
    {
        return new ExternalFootballStadiumDto(
            stadium.Id ?? string.Empty,
            stadium.NameEn ?? string.Empty,
            stadium.FifaName,
            stadium.CityEn,
            stadium.CountryEn,
            stadium.Capacity,
            stadium.Region);
    }

    private static ExternalFootballMatchDto ToDto(WorldCup26Game game)
    {
        return new ExternalFootballMatchDto(
            game.Id ?? string.Empty,
            NormalizeTeamId(game.HomeTeamId),
            NormalizeTeamId(game.AwayTeamId),
            game.HomeTeamNameEn,
            game.AwayTeamNameEn,
            game.HomeTeamLabel,
            game.AwayTeamLabel,
            game.Group ?? string.Empty,
            game.Matchday ?? string.Empty,
            game.LocalDate ?? string.Empty,
            game.StadiumId ?? string.Empty,
            string.Equals(game.Finished, "TRUE", StringComparison.OrdinalIgnoreCase),
            game.TimeElapsed ?? string.Empty,
            game.Type ?? string.Empty,
            ParseNullableInt(game.HomeScore),
            ParseNullableInt(game.AwayScore));
    }

    private static string? NormalizeTeamId(string? teamId)
    {
        return string.IsNullOrWhiteSpace(teamId) || teamId == "0" ? null : teamId;
    }

    private static int ParseInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private sealed record TeamsResponse([property: JsonPropertyName("teams")] IReadOnlyList<WorldCup26Team>? Teams);

    private sealed record StadiumsResponse([property: JsonPropertyName("stadiums")] IReadOnlyList<WorldCup26Stadium>? Stadiums);

    private sealed record GamesResponse([property: JsonPropertyName("games")] IReadOnlyList<WorldCup26Game>? Games);

    private sealed record WorldCup26Team(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name_en")] string? NameEn,
        [property: JsonPropertyName("fifa_code")] string? FifaCode,
        [property: JsonPropertyName("iso2")] string? Iso2,
        [property: JsonPropertyName("groups")] string? Groups,
        [property: JsonPropertyName("flag")] string? Flag);

    private sealed record WorldCup26Stadium(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name_en")] string? NameEn,
        [property: JsonPropertyName("fifa_name")] string? FifaName,
        [property: JsonPropertyName("city_en")] string? CityEn,
        [property: JsonPropertyName("country_en")] string? CountryEn,
        [property: JsonPropertyName("capacity")] int? Capacity,
        [property: JsonPropertyName("region")] string? Region);

    private sealed record WorldCup26Game(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("home_team_id")] string? HomeTeamId,
        [property: JsonPropertyName("away_team_id")] string? AwayTeamId,
        [property: JsonPropertyName("home_team_name_en")] string? HomeTeamNameEn,
        [property: JsonPropertyName("away_team_name_en")] string? AwayTeamNameEn,
        [property: JsonPropertyName("home_team_label")] string? HomeTeamLabel,
        [property: JsonPropertyName("away_team_label")] string? AwayTeamLabel,
        [property: JsonPropertyName("home_score")] string? HomeScore,
        [property: JsonPropertyName("away_score")] string? AwayScore,
        [property: JsonPropertyName("group")] string? Group,
        [property: JsonPropertyName("matchday")] string? Matchday,
        [property: JsonPropertyName("local_date")] string? LocalDate,
        [property: JsonPropertyName("stadium_id")] string? StadiumId,
        [property: JsonPropertyName("finished")] string? Finished,
        [property: JsonPropertyName("time_elapsed")] string? TimeElapsed,
        [property: JsonPropertyName("type")] string? Type);
}

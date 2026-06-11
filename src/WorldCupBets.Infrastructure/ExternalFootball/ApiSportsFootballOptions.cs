namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class ApiSportsFootballOptions
{
    public static readonly string[] DefaultIncludedTeamNames =
    [
        "Argentina",
        "France",
        "Brazil",
        "England",
        "Spain",
        "Portugal",
        "Germany",
        "Netherlands"
    ];

    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://v3.football.api-sports.io";

    public IReadOnlySet<string> IncludedTeamNames { get; init; } = new HashSet<string>(DefaultIncludedTeamNames, StringComparer.OrdinalIgnoreCase);
}

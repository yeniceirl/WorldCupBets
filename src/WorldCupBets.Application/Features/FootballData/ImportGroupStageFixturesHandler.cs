using System.Globalization;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.FootballData;

public sealed class ImportGroupStageFixturesHandler
{
    private const string ProviderName = "worldcup26";

    public static async Task<ImportGroupStageFixturesResultDto> Handle(
        ImportGroupStageFixturesCommand command,
        IExternalFootballDataRepository externalFootballDataRepository,
        IMatchRepository matchRepository,
        CancellationToken cancellationToken)
    {
        var snapshot = await externalFootballDataRepository.GetSnapshotAsync(ProviderName, cancellationToken)
            ?? throw new InvalidOperationException("External football data must be synchronized before importing group stage fixtures.");

        var stadiumsByExternalId = snapshot.Stadiums.ToDictionary(stadium => stadium.ExternalId, StringComparer.OrdinalIgnoreCase);
        var existingFixtures = await matchRepository.ListGroupStageFixturesAsync(cancellationToken);
        var matchIdsWithBets = await matchRepository.ListMatchIdsWithBetsAsync(existingFixtures.Select(match => match.Id), cancellationToken);
        var existingFixturesByKey = existingFixtures.ToDictionary(GetFixtureKey, StringComparer.OrdinalIgnoreCase);
        var importedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;
        var unsafeUpdateSkippedCount = 0;

        foreach (var externalMatch in snapshot.Matches.Where(IsGroupStageFixture))
        {
            if (!TryParseLocalDateAsUtc(externalMatch.LocalDateText, stadiumsByExternalId.GetValueOrDefault(externalMatch.StadiumExternalId), out var startsAtUtc))
            {
                skippedCount++;
                continue;
            }

            var homeTeamName = externalMatch.HomeTeamNameEn?.Trim();
            var awayTeamName = externalMatch.AwayTeamNameEn?.Trim();
            if (string.IsNullOrWhiteSpace(homeTeamName) || string.IsNullOrWhiteSpace(awayTeamName))
            {
                skippedCount++;
                continue;
            }

            var venue = stadiumsByExternalId.TryGetValue(externalMatch.StadiumExternalId, out var stadium)
                ? stadium.NameEn
                : "TBD";
            var fixtureKey = GetFixtureKey(externalMatch.GroupName, homeTeamName, awayTeamName);

            if (existingFixturesByKey.TryGetValue(fixtureKey, out var existingFixture))
            {
                if (matchIdsWithBets.Contains(existingFixture.Id) && existingFixture.StartsAtUtc != startsAtUtc)
                {
                    existingFixture.UpdateGroupStageFixtureMetadata(
                        venue,
                        ProviderName,
                        externalMatch.ExternalId,
                        snapshot.SyncedAtUtc);
                    skippedCount++;
                    unsafeUpdateSkippedCount++;
                    continue;
                }

                existingFixture.UpdateGroupStageFixture(
                    startsAtUtc,
                    venue,
                    ProviderName,
                    externalMatch.ExternalId,
                    snapshot.SyncedAtUtc);
                updatedCount++;
                continue;
            }

            var match = Match.CreateGroupStageFixture(
                externalMatch.GroupName,
                homeTeamName,
                awayTeamName,
                startsAtUtc,
                venue,
                ProviderName,
                externalMatch.ExternalId,
                snapshot.SyncedAtUtc);

            await matchRepository.AddAsync(match, cancellationToken);
            existingFixturesByKey.Add(fixtureKey, match);
            importedCount++;
        }

        await matchRepository.SaveChangesAsync(cancellationToken);

        return new ImportGroupStageFixturesResultDto(
            ProviderName,
            importedCount,
            updatedCount,
            skippedCount,
            unsafeUpdateSkippedCount,
            snapshot.SyncedAtUtc);
    }

    private static bool IsGroupStageFixture(ExternalFootballMatchDto externalMatch)
    {
        return string.Equals(externalMatch.StageType, "group", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(externalMatch.GroupName);
    }

    private static bool TryParseLocalDateAsUtc(string localDateText, ExternalFootballStadiumDto? stadium, out DateTime startsAtUtc)
    {
        if (DateTime.TryParseExact(
                localDateText,
                "MM/dd/yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            var timeZone = ResolveTimeZone(stadium);
            if (timeZone is null)
            {
                startsAtUtc = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                return true;
            }

            startsAtUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified), timeZone);
            return true;
        }

        startsAtUtc = default;
        return false;
    }

    private static TimeZoneInfo? ResolveTimeZone(ExternalFootballStadiumDto? stadium)
    {
        var city = stadium?.CityEn?.Trim();
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        if (ContainsAny(city, "mexico city", "guadalajara", "zapopan"))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
        }

        if (ContainsAny(city, "monterrey", "guadalupe"))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Monterrey");
        }

        if (ContainsAny(city, "arlington", "dallas", "houston", "kansas city"))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }

        if (ContainsAny(city, "atlanta", "boston", "foxborough", "east rutherford", "new york/new jersey", "miami gardens", "miami", "philadelphia"))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }

        if (ContainsAny(city, "toronto"))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");
        }

        if (ContainsAny(city, "inglewood", "los angeles", "san francisco bay area", "santa clara", "seattle"))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        }

        if (ContainsAny(city, "vancouver"))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Vancouver");
        }

        return null;
    }

    private static bool ContainsAny(string value, params string[] fragments)
    {
        return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetFixtureKey(Match match)
    {
        return GetFixtureKey(match.GroupName ?? string.Empty, match.HomeTeamName, match.AwayTeamName);
    }

    private static string GetFixtureKey(string groupName, string homeTeamName, string awayTeamName)
    {
        return string.Join('|', MatchPhase.GroupStage, groupName.Trim(), homeTeamName.Trim(), awayTeamName.Trim());
    }
}

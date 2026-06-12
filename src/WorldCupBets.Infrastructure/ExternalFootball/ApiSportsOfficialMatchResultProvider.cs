using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class ApiSportsOfficialMatchResultProvider(HttpClient httpClient, ApiSportsFootballOptions options) : IOfficialMatchResultProvider
{
    private static readonly HashSet<string> FinalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "FT",
        "AET",
        "PEN"
    };

    public string ProviderName => string.IsNullOrWhiteSpace(options.ApiKey) ? string.Empty : "api-sports";

    public async Task<OfficialMatchResultConfirmation?> TryConfirmAsync(OfficialMatchResultLookup lookup, CancellationToken cancellationToken = default)
    {
        foreach (var scheduledDate in GetCandidateDates(lookup))
        {
            var response = await SendAsync<ApiSportsFixturesResponse>($"/fixtures?date={scheduledDate:yyyy-MM-dd}", cancellationToken);
            var fixture = SelectFixture(response?.Response ?? [], lookup);
            if (fixture is null)
            {
                continue;
            }

            if (!FinalStatuses.Contains(fixture.Fixture?.Status?.Short ?? string.Empty)
                || fixture.Goals?.Home is null
                || fixture.Goals?.Away is null)
            {
                return null;
            }

            return new OfficialMatchResultConfirmation(
                fixture.Fixture!.Id.ToString(CultureInfo.InvariantCulture),
                ParseUtc(fixture.Fixture.Date) ?? DateTime.UtcNow,
                fixture.Goals.Home.Value,
                fixture.Goals.Away.Value);
        }

        return null;
    }

    private async Task<T?> SendAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-apisports-key", options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new ApiSportsRateLimitException("API-Sports rate limit (HTTP 429) reached.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static IReadOnlyList<DateOnly> GetCandidateDates(OfficialMatchResultLookup lookup)
    {
        var dates = new HashSet<DateOnly>
        {
            lookup.ScheduledDate,
            DateOnly.FromDateTime(lookup.StartsAtUtc)
        };

        return dates.OrderBy(date => Math.Abs(date.DayNumber - lookup.ScheduledDate.DayNumber)).ToArray();
    }

    private static ApiSportsFixtureItem? SelectFixture(IReadOnlyList<ApiSportsFixtureItem> fixtures, OfficialMatchResultLookup lookup)
    {
        var normalizedHome = Normalize(lookup.HomeTeamName);
        var normalizedAway = Normalize(lookup.AwayTeamName);

        return fixtures
            .Where(fixture => Normalize(fixture.Teams?.Home?.Name) == normalizedHome)
            .Where(fixture => Normalize(fixture.Teams?.Away?.Name) == normalizedAway)
            .OrderBy(fixture => Math.Abs(((ParseUtc(fixture.Fixture?.Date) ?? lookup.StartsAtUtc) - lookup.StartsAtUtc).TotalMinutes))
            .FirstOrDefault();
    }

    private static DateTime? ParseUtc(string? value)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record ApiSportsFixturesResponse([property: JsonPropertyName("response")] IReadOnlyList<ApiSportsFixtureItem>? Response);

    private sealed record ApiSportsFixtureItem(
        [property: JsonPropertyName("fixture")] ApiSportsFixture? Fixture,
        [property: JsonPropertyName("teams")] ApiSportsTeams? Teams,
        [property: JsonPropertyName("goals")] ApiSportsGoals? Goals);

    private sealed record ApiSportsFixture(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("date")] string? Date,
        [property: JsonPropertyName("status")] ApiSportsFixtureStatus? Status);

    private sealed record ApiSportsFixtureStatus([property: JsonPropertyName("short")] string? Short);

    private sealed record ApiSportsTeams(
        [property: JsonPropertyName("home")] ApiSportsTeamName? Home,
        [property: JsonPropertyName("away")] ApiSportsTeamName? Away);

    private sealed record ApiSportsTeamName([property: JsonPropertyName("name")] string? Name);

    private sealed record ApiSportsGoals(
        [property: JsonPropertyName("home")] int? Home,
        [property: JsonPropertyName("away")] int? Away);
}

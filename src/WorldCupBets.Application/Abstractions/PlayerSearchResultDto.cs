namespace WorldCupBets.Application.Abstractions;

public sealed record PlayerSearchResultDto(
    string ExternalId,
    string Name,
    string? TeamName,
    string? Nationality,
    string? Position,
    string? ThumbnailUrl);

namespace WorldCupBets.Application.Abstractions;

/// <summary>
/// Thrown by an <see cref="IPlayerSquadProvider"/> implementation when the upstream API responds
/// with HTTP 429 (Too Many Requests), signalling that the daily quota has been exhausted and the
/// caller must abort further processing immediately.
/// </summary>
public sealed class ApiSportsRateLimitException(string message) : Exception(message);

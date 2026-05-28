namespace WorldCupBets.Application.Features.Auth;

public sealed record AuthTokenContext(
    int UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles);

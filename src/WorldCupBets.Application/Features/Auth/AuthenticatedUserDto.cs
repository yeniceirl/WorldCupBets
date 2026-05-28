namespace WorldCupBets.Application.Features.Auth;

public sealed record AuthenticatedUserDto(
    int Id,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles);

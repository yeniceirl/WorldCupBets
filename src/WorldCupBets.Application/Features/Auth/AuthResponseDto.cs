namespace WorldCupBets.Application.Features.Auth;

public sealed record AuthResponseDto(
    string AccessToken,
    AuthenticatedUserDto User);

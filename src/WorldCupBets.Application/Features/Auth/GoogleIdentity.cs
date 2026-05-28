namespace WorldCupBets.Application.Features.Auth;

public sealed record GoogleIdentity(
    string Subject,
    string Email,
    string DisplayName,
    bool EmailVerified);

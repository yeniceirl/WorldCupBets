namespace WorldCupBets.Application.Abstractions;

public interface IGoogleTokenValidator
{
    Task<bool> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

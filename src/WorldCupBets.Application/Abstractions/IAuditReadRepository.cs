using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Abstractions;

public interface IAuditReadRepository
{
    Task<IReadOnlyList<AuditUserReadModel>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<AuditUserReadModel?> GetUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditMatchBetReadModel>> ListMatchBetsAsync(int? userId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditChallengePositionReadModel>> ListChallengePositionsAsync(int? userId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditTournamentPickReadModel>> ListTournamentPicksAsync(int? userId = null, CancellationToken cancellationToken = default);

    Task<AuditTournamentSettlementReadModel?> GetTournamentSettlementAsync(CancellationToken cancellationToken = default);
}

public sealed record AuditUserReadModel(
    int UserId,
    string DisplayName,
    string Email,
    decimal CurrentBalanceCc,
    int RescueCount,
    decimal RescueDebtCc);

public sealed record AuditMatchBetReadModel(
    int MatchBetId,
    int UserId,
    MatchBetSelection Selection,
    decimal StakeAmountCc,
    DateTime PlacedAtUtc,
    int MatchId,
    string HomeTeamName,
    string AwayTeamName,
    DateTime StartsAtUtc,
    MatchBetSelection? OfficialResult,
    DateTime? SettledAtUtc);

public sealed record AuditChallengePositionReadModel(
    int MatchChallengeId,
    int UserId,
    MatchChallengeSide Side,
    decimal StakeAmountCc,
    DateTime EscrowedAtUtc,
    MatchChallengeStatus Status,
    MatchChallengeSide? WinnerSide,
    string ClaimText,
    string CreatorSideText,
    string TakerSideText,
    string HomeTeamName,
    string AwayTeamName,
    int ParticipantCount,
    decimal TotalStakeAmountCc);

public sealed record AuditTournamentPickReadModel(
    int TournamentPickId,
    int UserId,
    TournamentPickCategory Category,
    string SelectedText,
    decimal StakeAmountCc,
    DateTime PlacedAtUtc);

public sealed record AuditTournamentSettlementReadModel(
    string? ChampionTeamName,
    DateTime? ChampionSettledAtUtc,
    decimal ChampionJackpotCc,
    decimal UndistributedJackpotCc);

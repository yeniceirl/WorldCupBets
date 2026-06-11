using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class MatchChallenge : Entity
{
    public const int MaxClaimTextLength = 280;
    public const int MaxSideTextLength = 120;

    private readonly List<MatchChallengePosition> positions = [];

    private MatchChallenge()
    {
    }

    private MatchChallenge(
        int matchId,
        string claimText,
        string creatorSideText,
        string takerSideText,
        decimal stakeAmountCc,
        DateTime createdAtUtc)
    {
        MatchId = matchId;
        ClaimText = claimText;
        CreatorSideText = creatorSideText;
        TakerSideText = takerSideText;
        StakeAmountCc = stakeAmountCc;
        CreatedAtUtc = createdAtUtc;
        Status = MatchChallengeStatus.Open;
    }

    public int MatchId { get; private set; }

    public Match Match { get; private set; } = null!;

    public string ClaimText { get; private set; } = string.Empty;

    public string CreatorSideText { get; private set; } = string.Empty;

    public string TakerSideText { get; private set; } = string.Empty;

    public decimal StakeAmountCc { get; private set; }

    public MatchChallengeStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? MatchedAtUtc { get; private set; }

    public DateTime? SettledAtUtc { get; private set; }

    public DateTime? VoidedAtUtc { get; private set; }

    public DateTime? ExpiredAtUtc { get; private set; }

    public MatchChallengeSide? WinnerSide { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<MatchChallengePosition> Positions => positions.AsReadOnly();

    public MatchChallengePosition CreatorPosition => positions.Single(position => position.Side == MatchChallengeSide.Creator);

    public MatchChallengePosition? TakerPosition => positions.SingleOrDefault(position => position.Side == MatchChallengeSide.Taker);

    public bool IsActive => Status is MatchChallengeStatus.Open or MatchChallengeStatus.Matched;

    public bool IsTerminal => Status is MatchChallengeStatus.Settled or MatchChallengeStatus.Voided or MatchChallengeStatus.Expired;

    public static MatchChallenge Create(
        int creatorUserId,
        int matchId,
        string claimText,
        string creatorSideText,
        string takerSideText,
        decimal stakeAmountCc,
        DateTime createdAtUtc)
    {
        ValidateStake(stakeAmountCc);

        var challenge = new MatchChallenge(
            matchId,
            ValidateText(claimText, nameof(claimText), MaxClaimTextLength),
            ValidateText(creatorSideText, nameof(creatorSideText), MaxSideTextLength),
            ValidateText(takerSideText, nameof(takerSideText), MaxSideTextLength),
            stakeAmountCc,
            createdAtUtc);

        challenge.positions.Add(MatchChallengePosition.Create(creatorUserId, MatchChallengeSide.Creator, stakeAmountCc, createdAtUtc));
        return challenge;
    }

    public void Accept(int takerUserId, DateTime acceptedAtUtc)
    {
        EnsureStatus(MatchChallengeStatus.Open, "Only open challenges can be accepted.");

        if (CreatorPosition.UserId == takerUserId)
        {
            throw new InvalidOperationException("The creator cannot accept their own challenge.");
        }

        positions.Add(MatchChallengePosition.Create(takerUserId, MatchChallengeSide.Taker, StakeAmountCc, acceptedAtUtc));
        Status = MatchChallengeStatus.Matched;
        MatchedAtUtc = acceptedAtUtc;
    }

    public void Settle(MatchChallengeSide winnerSide, DateTime settledAtUtc)
    {
        EnsureStatus(MatchChallengeStatus.Matched, "Only matched challenges can be settled.");

        WinnerSide = winnerSide;
        Status = MatchChallengeStatus.Settled;
        SettledAtUtc = settledAtUtc;
    }

    public void Void(DateTime voidedAtUtc)
    {
        EnsureActive("Only active challenges can be voided.");

        Status = MatchChallengeStatus.Voided;
        VoidedAtUtc = voidedAtUtc;
    }

    public void Expire(DateTime expiredAtUtc)
    {
        EnsureActive("Only active challenges can be expired.");

        Status = MatchChallengeStatus.Expired;
        ExpiredAtUtc = expiredAtUtc;
    }

    private static void ValidateStake(decimal stakeAmountCc)
    {
        if (stakeAmountCc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stakeAmountCc), "Stake amount must be greater than zero.");
        }
    }

    private static string ValidateText(string text, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required.", parameterName);
        }

        var trimmed = text.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Text cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    private void EnsureStatus(MatchChallengeStatus expectedStatus, string message)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void EnsureActive(string message)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(message);
        }
    }
}

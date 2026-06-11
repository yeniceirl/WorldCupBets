using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class User : Entity
{
    public const decimal InitialBalanceCc = 1000m;
    public const decimal DeadRescueAmountCc = 100m;
    public const int MaxDeadRescuesPerTournament = 2;

    private User()
    {
    }

    private User(string googleSubject, string email, string displayName)
    {
        GoogleSubject = googleSubject;
        Email = email;
        DisplayName = displayName;
        CurrentBalanceCc = InitialBalanceCc;
    }

    public string GoogleSubject { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public decimal CurrentBalanceCc { get; private set; } = InitialBalanceCc;

    public int RescueCount { get; private set; }

    public decimal RescueDebtCc { get; private set; }

    public int Version { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = [];

    public static User Create(string googleSubject, string email, string displayName)
    {
        return new User(googleSubject, email, displayName);
    }

    public bool CanAfford(decimal amountCc)
    {
        return amountCc > 0 && CurrentBalanceCc >= amountCc;
    }

    public void DeductBalance(decimal amountCc)
    {
        if (amountCc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountCc), "Amount must be greater than zero.");
        }

        if (!CanAfford(amountCc))
        {
            throw new InvalidOperationException("The user does not have enough CopaCoin balance.");
        }

        CurrentBalanceCc -= amountCc;
    }

    public void CreditBalance(decimal amountCc)
    {
        if (amountCc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountCc), "Amount must be greater than zero.");
        }

        CurrentBalanceCc = checked(CurrentBalanceCc + amountCc);
    }

    public bool CanReceiveDeadRescue()
    {
        return CurrentBalanceCc == 0 && RescueCount < MaxDeadRescuesPerTournament;
    }

    public void ApplyDeadRescue()
    {
        if (!CanReceiveDeadRescue())
        {
            throw new InvalidOperationException("The user is not eligible for a dead rescue.");
        }

        CurrentBalanceCc += DeadRescueAmountCc;
        RescueCount += 1;
        RescueDebtCc += DeadRescueAmountCc;
    }

    public bool ApplyDeadRescueIfEligible()
    {
        if (!CanReceiveDeadRescue())
        {
            return false;
        }

        ApplyDeadRescue();
        return true;
    }
}

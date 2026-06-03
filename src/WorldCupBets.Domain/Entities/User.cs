using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class User : Entity
{
    public const int InitialBalanceCc = 1000;
    public const int DeadRescueAmountCc = 100;
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

    public int CurrentBalanceCc { get; private set; } = InitialBalanceCc;

    public int RescueCount { get; private set; }

    public int RescueDebtCc { get; private set; }

    public int Version { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = [];

    public static User Create(string googleSubject, string email, string displayName)
    {
        return new User(googleSubject, email, displayName);
    }

    public bool CanAfford(int amountCc)
    {
        return amountCc > 0 && CurrentBalanceCc >= amountCc;
    }

    public void DeductBalance(int amountCc)
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

    public void CreditBalance(int amountCc)
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

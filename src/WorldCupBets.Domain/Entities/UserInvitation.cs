using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class UserInvitation : Entity
{
    private UserInvitation()
    {
    }

    private UserInvitation(string email, string roleName)
    {
        Email = NormalizeEmail(email);
        RoleName = roleName;
    }

    public string Email { get; private set; } = string.Empty;

    public string RoleName { get; private set; } = string.Empty;

    public static UserInvitation Create(string email, string roleName = "Bettor")
    {
        return new UserInvitation(email, roleName);
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}

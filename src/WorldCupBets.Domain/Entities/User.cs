using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class User : Entity
{
    private User()
    {
    }

    private User(string googleSubject, string email, string displayName)
    {
        GoogleSubject = googleSubject;
        Email = email;
        DisplayName = displayName;
    }

    public string GoogleSubject { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = [];

    public static User Create(string googleSubject, string email, string displayName)
    {
        return new User(googleSubject, email, displayName);
    }
}

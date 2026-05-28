namespace WorldCupBets.Domain.Entities;

public sealed class UserRole
{
    private UserRole()
    {
    }

    private UserRole(User user, Role role)
    {
        User = user;
        Role = role;
    }

    public int UserId { get; private set; }

    public int RoleId { get; private set; }

    public User User { get; private set; } = null!;

    public Role Role { get; private set; } = null!;

    public static UserRole Create(User user, Role role)
    {
        return new UserRole(user, role);
    }
}

using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class Role : Entity
{
    private Role()
    {
    }

    private Role(string name)
    {
        Name = name;
    }

    public string Name { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = [];

    public static Role Create(string name)
    {
        return new Role(name);
    }
}

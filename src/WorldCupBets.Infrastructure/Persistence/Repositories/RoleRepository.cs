using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(AppDbContext dbContext) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return dbContext.Roles.SingleOrDefaultAsync(role => role.Name == name, cancellationToken);
    }
}

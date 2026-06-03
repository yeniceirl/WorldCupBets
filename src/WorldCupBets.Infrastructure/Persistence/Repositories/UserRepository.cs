using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByGoogleSubjectWithRolesAsync(string googleSubject, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(user => user.GoogleSubject == googleSubject, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CurrentBalanceCc)
            .ThenBy(user => user.DisplayName)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

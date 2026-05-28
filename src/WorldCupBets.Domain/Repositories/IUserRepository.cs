using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByGoogleSubjectWithRolesAsync(string googleSubject, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

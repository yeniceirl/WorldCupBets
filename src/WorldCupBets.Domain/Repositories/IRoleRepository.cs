using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

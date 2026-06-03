using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IUserInvitationRepository
{
    Task<UserInvitation?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

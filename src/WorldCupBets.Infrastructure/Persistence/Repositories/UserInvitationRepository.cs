using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class UserInvitationRepository(AppDbContext dbContext) : IUserInvitationRepository
{
    public Task<UserInvitation?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = UserInvitation.NormalizeEmail(email);
        return dbContext.UserInvitations.SingleOrDefaultAsync(invitation => invitation.Email == normalizedEmail, cancellationToken);
    }

    public Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
    {
        return dbContext.UserInvitations.AddAsync(invitation, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

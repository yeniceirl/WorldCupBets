using System.Reflection;
using WorldCupBets.Application.Features.Users;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class GetCurrentUserSummaryHandlerTests
{
    [Fact]
    public async Task Handle_Applies_Dead_Rescue_When_User_Is_At_Zero()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        SetProperty(user, nameof(User.CurrentBalanceCc), 0);

        var result = await GetCurrentUserSummaryHandler.Handle(
            new GetCurrentUserSummaryQuery(user.Id),
            new StubUserRepository(user),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(User.DeadRescueAmountCc, result.Value!.CurrentBalanceCc);
        Assert.Equal(1, result.Value.RescueCount);
        Assert.Equal(User.DeadRescueAmountCc, result.Value.RescueDebtCc);
    }

    private static void SetEntityId(Entity entity, int id)
    {
        var property = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(entity, id);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }

    private sealed class StubUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetByGoogleSubjectWithRolesAsync(string googleSubject, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(users.SingleOrDefault(user => user.GoogleSubject == googleSubject));
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(users.SingleOrDefault(user => user.Id == userId));
        }

        public Task<IReadOnlyList<User>> ListLeaderboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(users.OrderByDescending(user => user.CurrentBalanceCc).ToArray());
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

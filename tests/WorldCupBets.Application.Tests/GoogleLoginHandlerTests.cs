using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Auth;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class GoogleLoginHandlerTests
{
    [Fact]
    public async Task Handle_Provisions_Invited_First_Time_User_As_Bettor()
    {
        var validator = new StubGoogleTokenValidator(Result<GoogleIdentity>.Success(new GoogleIdentity(
            "google-123",
            "ada@example.com",
            "Ada Lovelace",
            true)));
        var tokenGenerator = new StubJwtTokenGenerator();
        var userRepository = new InMemoryUserRepository();
        var invitationRepository = new InMemoryUserInvitationRepository(UserInvitation.Create("ada@example.com"));
        var roleRepository = new InMemoryRoleRepository(Role.Create("Bettor"));

        var result = await GoogleLoginHandler.Handle(
            new GoogleLoginCommand("valid-id-token"),
            validator,
            tokenGenerator,
            userRepository,
            invitationRepository,
            roleRepository,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(userRepository.Users);
        Assert.Equal("google-123", userRepository.Users[0].GoogleSubject);
        Assert.Equal(User.InitialBalanceCc, userRepository.Users[0].CurrentBalanceCc);
        Assert.Contains("Bettor", result.Value!.User.Roles);
        Assert.Equal(result.Value.AccessToken, tokenGenerator.LastToken);
    }

    [Fact]
    public async Task Handle_Rejects_First_Time_User_Without_Invitation()
    {
        var validator = new StubGoogleTokenValidator(Result<GoogleIdentity>.Success(new GoogleIdentity(
            "google-123",
            "ada@example.com",
            "Ada Lovelace",
            true)));
        var tokenGenerator = new StubJwtTokenGenerator();
        var userRepository = new InMemoryUserRepository();
        var invitationRepository = new InMemoryUserInvitationRepository();
        var roleRepository = new InMemoryRoleRepository(Role.Create("Bettor"));

        var result = await GoogleLoginHandler.Handle(
            new GoogleLoginCommand("valid-id-token"),
            validator,
            tokenGenerator,
            userRepository,
            invitationRepository,
            roleRepository,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("auth.not_invited", result.Error?.Code);
        Assert.Empty(userRepository.Users);
        Assert.Equal(0, userRepository.AddCalls);
        Assert.Empty(tokenGenerator.LastToken);
    }

    [Fact]
    public async Task Handle_Reuses_Returning_User_Without_Creating_Duplicate()
    {
        var existingUser = User.Create("google-123", "ada@example.com", "Ada Lovelace");
        var bettorRole = Role.Create("Bettor");
        existingUser.UserRoles.Add(UserRole.Create(existingUser, bettorRole));
        SetEntityId(existingUser, 42);

        var validator = new StubGoogleTokenValidator(Result<GoogleIdentity>.Success(new GoogleIdentity(
            "google-123",
            "ada@example.com",
            "Ada Lovelace",
            true)));
        var tokenGenerator = new StubJwtTokenGenerator();
        var userRepository = new InMemoryUserRepository(existingUser);
        var invitationRepository = new InMemoryUserInvitationRepository();
        var roleRepository = new InMemoryRoleRepository(bettorRole);

        var result = await GoogleLoginHandler.Handle(
            new GoogleLoginCommand("valid-id-token"),
            validator,
            tokenGenerator,
            userRepository,
            invitationRepository,
            roleRepository,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(userRepository.Users);
        Assert.Equal(0, userRepository.AddCalls);
        Assert.Equal(42, result.Value!.User.Id);
        Assert.Equal(User.InitialBalanceCc, existingUser.CurrentBalanceCc);
        Assert.Contains("Bettor", result.Value.User.Roles);
    }

    private static void SetEntityId(Entity entity, int id)
    {
        var property = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(entity, id);
    }

    private sealed class StubGoogleTokenValidator(Result<GoogleIdentity> result) : IGoogleTokenValidator
    {
        public Task<Result<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class StubJwtTokenGenerator : IJwtTokenGenerator
    {
        public string LastToken { get; private set; } = string.Empty;

        public string GenerateAccessToken(AuthTokenContext context)
        {
            LastToken = $"token-for-{context.UserId}";
            return LastToken;
        }
    }

    private sealed class InMemoryUserRepository(params User[] seededUsers) : IUserRepository
    {
        private int nextId = seededUsers.Length + 1;

        public List<User> Users { get; } = [.. seededUsers];

        public int AddCalls { get; private set; }

        public Task<User?> GetByGoogleSubjectWithRolesAsync(string googleSubject, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.GoogleSubject == googleSubject));
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));
        }

        public Task<IReadOnlyList<User>> ListLeaderboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users.OrderByDescending(user => user.CurrentBalanceCc).ToArray());
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            AddCalls++;
            SetEntityId(user, nextId++);
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRoleRepository(params Role[] roles) : IRoleRepository
    {
        private readonly IReadOnlyCollection<Role> seededRoles = roles;

        public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(seededRoles.SingleOrDefault(role => role.Name == name));
        }
    }

    private sealed class InMemoryUserInvitationRepository(params UserInvitation[] invitations) : IUserInvitationRepository
    {
        public Task<UserInvitation?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = UserInvitation.NormalizeEmail(email);
            return Task.FromResult(invitations.SingleOrDefault(invitation => invitation.Email == normalizedEmail));
        }

        public Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    [Fact]
    public void Create_Tracks_Dead_Rescue_State()
    {
        var user = User.Create("google-999", "grace@example.com", "Grace Hopper");

        Assert.False(user.CanReceiveDeadRescue());

        SetProperty(user, nameof(User.CurrentBalanceCc), 0);

        Assert.True(user.CanReceiveDeadRescue());

        user.ApplyDeadRescue();

        Assert.Equal(User.DeadRescueAmountCc, user.CurrentBalanceCc);
        Assert.Equal(1, user.RescueCount);
        Assert.Equal(User.DeadRescueAmountCc, user.RescueDebtCc);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }
}

using System.Reflection;
using WorldCupBets.Application.Features.Admin;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class CreateUserInvitationHandlerTests
{
    [Fact]
    public async Task Handle_Creates_Invitation_For_Configured_Role()
    {
        var invitationRepository = new InMemoryUserInvitationRepository();
        var roleRepository = new InMemoryRoleRepository(Role.Create("Bettor"));

        var result = await CreateUserInvitationHandler.Handle(
            new CreateUserInvitationCommand("Ada@Example.com", "Bettor"),
            invitationRepository,
            roleRepository,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("ADA@EXAMPLE.COM", result.Value!.Email);
        Assert.Equal("Bettor", result.Value.RoleName);
        Assert.False(result.Value.WasAlreadyInvited);
        Assert.Single(invitationRepository.Invitations);
        Assert.Equal(1, invitationRepository.SaveCalls);
    }

    [Fact]
    public async Task Handle_Returns_Existing_Invitation_Without_Duplicate()
    {
        var existingInvitation = UserInvitation.Create("ada@example.com", "Bettor");
        SetEntityId(existingInvitation, 7);
        var invitationRepository = new InMemoryUserInvitationRepository(existingInvitation);
        var roleRepository = new InMemoryRoleRepository(Role.Create("Bettor"));

        var result = await CreateUserInvitationHandler.Handle(
            new CreateUserInvitationCommand("ADA@example.com", "Bettor"),
            invitationRepository,
            roleRepository,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(7, result.Value!.Id);
        Assert.True(result.Value.WasAlreadyInvited);
        Assert.Single(invitationRepository.Invitations);
        Assert.Equal(0, invitationRepository.SaveCalls);
    }

    [Fact]
    public async Task Handle_Rejects_Unconfigured_Role()
    {
        var invitationRepository = new InMemoryUserInvitationRepository();
        var roleRepository = new InMemoryRoleRepository();

        var result = await CreateUserInvitationHandler.Handle(
            new CreateUserInvitationCommand("ada@example.com", "Admin"),
            invitationRepository,
            roleRepository,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("admin.role_not_found", result.Error?.Code);
        Assert.Empty(invitationRepository.Invitations);
    }

    private static void SetEntityId(Entity entity, int id)
    {
        var property = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(entity, id);
    }

    private sealed class InMemoryUserInvitationRepository(params UserInvitation[] invitations) : IUserInvitationRepository
    {
        private int nextId = invitations.Length + 1;

        public List<UserInvitation> Invitations { get; } = [.. invitations];

        public int SaveCalls { get; private set; }

        public Task<UserInvitation?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = UserInvitation.NormalizeEmail(email);
            return Task.FromResult(Invitations.SingleOrDefault(invitation => invitation.Email == normalizedEmail));
        }

        public Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
        {
            SetEntityId(invitation, nextId++);
            Invitations.Add(invitation);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
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
}

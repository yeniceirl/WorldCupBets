using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("user_invitations");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.Email).HasMaxLength(320).IsRequired();
        builder.Property(invitation => invitation.RoleName).HasMaxLength(100).IsRequired();
        builder.HasIndex(invitation => invitation.Email).IsUnique();
    }
}

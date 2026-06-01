using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.GoogleSubject).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.CurrentBalanceCc).IsRequired();
        builder.Property(user => user.RescueCount).IsRequired();
        builder.Property(user => user.RescueDebtCc).IsRequired();
        builder.HasIndex(user => user.GoogleSubject).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}

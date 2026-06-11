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
        builder.Property(user => user.CurrentBalanceCc).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(user => user.RescueCount).IsRequired();
        builder.Property(user => user.RescueDebtCc).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(user => user.Version).IsConcurrencyToken().IsRequired();
        builder.HasIndex(user => user.GoogleSubject).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();

        builder.HasData(
            new
            {
                Id = 101,
                GoogleSubject = "demo-maple",
                Email = "maple@worldcupbets.local",
                DisplayName = "Maple Moose",
                CurrentBalanceCc = 1325m,
                RescueCount = 0,
                RescueDebtCc = 0m,
                Version = 0
            },
            new
            {
                Id = 102,
                GoogleSubject = "demo-zayu",
                Email = "zayu@worldcupbets.local",
                DisplayName = "Zayu Jaguar",
                CurrentBalanceCc = 1180m,
                RescueCount = 0,
                RescueDebtCc = 0m,
                Version = 0
            },
            new
            {
                Id = 103,
                GoogleSubject = "demo-clutch",
                Email = "clutch@worldcupbets.local",
                DisplayName = "Clutch Eagle",
                CurrentBalanceCc = 1110m,
                RescueCount = 1,
                RescueDebtCc = 100m,
                Version = 0
            },
            new
            {
                Id = 104,
                GoogleSubject = "demo-lucia",
                Email = "lucia@worldcupbets.local",
                DisplayName = "Lucia del Gol",
                CurrentBalanceCc = 990m,
                RescueCount = 0,
                RescueDebtCc = 0m,
                Version = 0
            },
            new
            {
                Id = 105,
                GoogleSubject = "demo-takeshi",
                Email = "takeshi@worldcupbets.local",
                DisplayName = "Takeshi Bracket",
                CurrentBalanceCc = 845m,
                RescueCount = 2,
                RescueDebtCc = 200m,
                Version = 0
            },
            new
            {
                Id = 106,
                GoogleSubject = "demo-nora",
                Email = "nora@worldcupbets.local",
                DisplayName = "Nora Finalista",
                CurrentBalanceCc = 760m,
                RescueCount = 0,
                RescueDebtCc = 0m,
                Version = 0
            });
    }
}

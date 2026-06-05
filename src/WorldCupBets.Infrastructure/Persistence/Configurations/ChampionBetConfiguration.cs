using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class ChampionBetConfiguration : IEntityTypeConfiguration<ChampionBet>
{
    public void Configure(EntityTypeBuilder<ChampionBet> builder)
    {
        builder.ToTable("champion_bets");
        builder.HasKey(championBet => championBet.Id);
        builder.Property(championBet => championBet.TeamName).HasMaxLength(100).IsRequired();
        builder.Property(championBet => championBet.StakeAmountCc).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(championBet => championBet.PlacedAtUtc).IsRequired();

        builder.HasIndex(championBet => championBet.UserId).IsUnique();

        builder.HasOne(championBet => championBet.User)
            .WithMany()
            .HasForeignKey(championBet => championBet.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

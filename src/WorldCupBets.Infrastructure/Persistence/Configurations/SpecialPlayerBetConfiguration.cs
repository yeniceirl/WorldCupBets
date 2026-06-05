using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class SpecialPlayerBetConfiguration : IEntityTypeConfiguration<SpecialPlayerBet>
{
    public void Configure(EntityTypeBuilder<SpecialPlayerBet> builder)
    {
        builder.ToTable("special_player_bets");
        builder.HasKey(specialPlayerBet => specialPlayerBet.Id);
        builder.Property(specialPlayerBet => specialPlayerBet.Category).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(specialPlayerBet => specialPlayerBet.PlayerName).HasMaxLength(160).IsRequired();
        builder.Property(specialPlayerBet => specialPlayerBet.ExternalPlayerId).HasMaxLength(80);
        builder.Property(specialPlayerBet => specialPlayerBet.StakeAmountCc).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(specialPlayerBet => specialPlayerBet.PlacedAtUtc).IsRequired();

        builder.HasIndex(specialPlayerBet => new { specialPlayerBet.UserId, specialPlayerBet.Category }).IsUnique();

        builder.HasOne(specialPlayerBet => specialPlayerBet.User)
            .WithMany()
            .HasForeignKey(specialPlayerBet => specialPlayerBet.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

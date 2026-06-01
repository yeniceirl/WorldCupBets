using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class MatchBetConfiguration : IEntityTypeConfiguration<MatchBet>
{
    public void Configure(EntityTypeBuilder<MatchBet> builder)
    {
        builder.ToTable("match_bets");
        builder.HasKey(matchBet => matchBet.Id);
        builder.Property(matchBet => matchBet.Selection)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(matchBet => matchBet.StakeAmountCc).IsRequired();
        builder.Property(matchBet => matchBet.PlacedAtUtc).IsRequired();

        builder.HasIndex(matchBet => new { matchBet.UserId, matchBet.MatchId }).IsUnique();

        builder.HasOne(matchBet => matchBet.User)
            .WithMany()
            .HasForeignKey(matchBet => matchBet.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(matchBet => matchBet.Match)
            .WithMany()
            .HasForeignKey(matchBet => matchBet.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

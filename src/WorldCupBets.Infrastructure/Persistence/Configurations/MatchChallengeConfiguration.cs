using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class MatchChallengeConfiguration : IEntityTypeConfiguration<MatchChallenge>
{
    public void Configure(EntityTypeBuilder<MatchChallenge> builder)
    {
        builder.ToTable("match_challenges");
        builder.HasKey(matchChallenge => matchChallenge.Id);
        builder.Property(matchChallenge => matchChallenge.ClaimText).HasMaxLength(MatchChallenge.MaxClaimTextLength).IsRequired();
        builder.Property(matchChallenge => matchChallenge.CreatorSideText).HasMaxLength(MatchChallenge.MaxSideTextLength).IsRequired();
        builder.Property(matchChallenge => matchChallenge.TakerSideText).HasMaxLength(MatchChallenge.MaxSideTextLength).IsRequired();
        builder.Property(matchChallenge => matchChallenge.StakeAmountCc).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(matchChallenge => matchChallenge.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(matchChallenge => matchChallenge.WinnerSide).HasConversion<string>().HasMaxLength(16);
        builder.Property(matchChallenge => matchChallenge.CreatedAtUtc).IsRequired();
        builder.Property(matchChallenge => matchChallenge.MatchedAtUtc);
        builder.Property(matchChallenge => matchChallenge.SettledAtUtc);
        builder.Property(matchChallenge => matchChallenge.VoidedAtUtc);
        builder.Property(matchChallenge => matchChallenge.ExpiredAtUtc);
        builder.Property(matchChallenge => matchChallenge.Version).IsConcurrencyToken().IsRequired();

        builder.HasIndex(matchChallenge => new { matchChallenge.MatchId, matchChallenge.Status });

        builder.HasOne(matchChallenge => matchChallenge.Match)
            .WithMany()
            .HasForeignKey(matchChallenge => matchChallenge.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(matchChallenge => matchChallenge.Positions)
            .WithOne(position => position.MatchChallenge)
            .HasForeignKey(position => position.MatchChallengeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

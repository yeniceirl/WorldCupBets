using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class MatchChallengePositionConfiguration : IEntityTypeConfiguration<MatchChallengePosition>
{
    public void Configure(EntityTypeBuilder<MatchChallengePosition> builder)
    {
        builder.ToTable("match_challenge_positions");
        builder.HasKey(position => position.Id);
        builder.Property(position => position.Side).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(position => position.StakeAmountCc).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(position => position.EscrowedAtUtc).IsRequired();

        builder.HasIndex(position => new { position.MatchChallengeId, position.Side }).IsUnique();
        builder.HasIndex(position => position.UserId);

        builder.HasOne(position => position.User)
            .WithMany()
            .HasForeignKey(position => position.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

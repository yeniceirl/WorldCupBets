using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(match => match.Id);
        builder.Property(match => match.Phase)
            .HasColumnName("Stage")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(match => match.HomeTeamName).HasMaxLength(100).IsRequired();
        builder.Property(match => match.AwayTeamName).HasMaxLength(100).IsRequired();
        builder.Property(match => match.StartsAtUtc).IsRequired();
        builder.Property(match => match.Venue).HasMaxLength(150).IsRequired();

        builder.HasData(
            new
            {
                Id = 1,
                Phase = MatchPhase.GroupStage,
                HomeTeamName = "Argentina",
                AwayTeamName = "Japan",
                StartsAtUtc = new DateTime(2026, 6, 14, 18, 0, 0, DateTimeKind.Utc),
                Venue = "MetLife Stadium"
            },
            new
            {
                Id = 2,
                Phase = MatchPhase.GroupStage,
                HomeTeamName = "Spain",
                AwayTeamName = "Mexico",
                StartsAtUtc = new DateTime(2026, 6, 15, 21, 0, 0, DateTimeKind.Utc),
                Venue = "Estadio Akron"
            },
            new
            {
                Id = 3,
                Phase = MatchPhase.GroupStage,
                HomeTeamName = "United States",
                AwayTeamName = "France",
                StartsAtUtc = new DateTime(2026, 6, 16, 1, 0, 0, DateTimeKind.Utc),
                Venue = "AT&T Stadium"
            });
    }
}

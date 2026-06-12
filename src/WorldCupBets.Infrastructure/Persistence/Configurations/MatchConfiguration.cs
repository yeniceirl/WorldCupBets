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
        builder.Property(match => match.GroupName).HasMaxLength(20);
        builder.Property(match => match.SourceProvider).HasMaxLength(40);
        builder.Property(match => match.SourceMatchId).HasMaxLength(40);
        builder.Property(match => match.SourceSyncedAtUtc);
        builder.Property(match => match.OfficialResult)
            .HasConversion<string>()
            .HasMaxLength(16);
        builder.Property(match => match.OfficialHomeScore);
        builder.Property(match => match.OfficialAwayScore);
        builder.Property(match => match.OfficialDataProvider).HasMaxLength(40);
        builder.Property(match => match.OfficialDataSourceReference).HasMaxLength(80);
        builder.Property(match => match.OfficialDataVerifiedAtUtc);
        builder.Property(match => match.SettledAtUtc);
        builder.Property(match => match.Version).IsConcurrencyToken().IsRequired();
        builder.HasIndex(match => new { match.Phase, match.GroupName, match.HomeTeamName, match.AwayTeamName });

        builder.HasData(
            new
            {
                Id = 1,
                Phase = MatchPhase.GroupStage,
                HomeTeamName = "Mexico",
                AwayTeamName = "South Africa",
                StartsAtUtc = new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc),
                Venue = "Estadio Azteca",
                GroupName = "A",
                SourceProvider = (string?)null,
                SourceMatchId = (string?)null,
                SourceSyncedAtUtc = (DateTime?)null,
                OfficialResult = (MatchBetSelection?)null,
                OfficialHomeScore = (int?)null,
                OfficialAwayScore = (int?)null,
                OfficialDataProvider = (string?)null,
                OfficialDataSourceReference = (string?)null,
                OfficialDataVerifiedAtUtc = (DateTime?)null,
                SettledAtUtc = (DateTime?)null,
                Version = 0
            },
            new
            {
                Id = 2,
                Phase = MatchPhase.GroupStage,
                HomeTeamName = "South Korea",
                AwayTeamName = "Czech Republic",
                StartsAtUtc = new DateTime(2026, 6, 12, 1, 0, 0, DateTimeKind.Utc),
                Venue = "Estadio Akron",
                GroupName = "A",
                SourceProvider = (string?)null,
                SourceMatchId = (string?)null,
                SourceSyncedAtUtc = (DateTime?)null,
                OfficialResult = (MatchBetSelection?)null,
                OfficialHomeScore = (int?)null,
                OfficialAwayScore = (int?)null,
                OfficialDataProvider = (string?)null,
                OfficialDataSourceReference = (string?)null,
                OfficialDataVerifiedAtUtc = (DateTime?)null,
                SettledAtUtc = (DateTime?)null,
                Version = 0
            },
            new
            {
                Id = 3,
                Phase = MatchPhase.GroupStage,
                HomeTeamName = "Czech Republic",
                AwayTeamName = "Mexico",
                StartsAtUtc = new DateTime(2026, 6, 24, 23, 0, 0, DateTimeKind.Utc),
                Venue = "Estadio Azteca",
                GroupName = "A",
                SourceProvider = (string?)null,
                SourceMatchId = (string?)null,
                SourceSyncedAtUtc = (DateTime?)null,
                OfficialResult = (MatchBetSelection?)null,
                OfficialHomeScore = (int?)null,
                OfficialAwayScore = (int?)null,
                OfficialDataProvider = (string?)null,
                OfficialDataSourceReference = (string?)null,
                OfficialDataVerifiedAtUtc = (DateTime?)null,
                SettledAtUtc = (DateTime?)null,
                Version = 0
            });
    }
}

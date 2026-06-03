using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class ExternalFootballMatchConfiguration : IEntityTypeConfiguration<ExternalFootballMatch>
{
    public void Configure(EntityTypeBuilder<ExternalFootballMatch> builder)
    {
        builder.ToTable("external_football_matches");
        builder.HasKey(match => match.Id);
        builder.Property(match => match.ProviderName).HasMaxLength(40).IsRequired();
        builder.Property(match => match.ExternalId).HasMaxLength(40).IsRequired();
        builder.Property(match => match.HomeTeamExternalId).HasMaxLength(40);
        builder.Property(match => match.AwayTeamExternalId).HasMaxLength(40);
        builder.Property(match => match.HomeTeamNameEn).HasMaxLength(100);
        builder.Property(match => match.AwayTeamNameEn).HasMaxLength(100);
        builder.Property(match => match.HomeTeamLabel).HasMaxLength(120);
        builder.Property(match => match.AwayTeamLabel).HasMaxLength(120);
        builder.Property(match => match.GroupName).HasMaxLength(20).IsRequired();
        builder.Property(match => match.Matchday).HasMaxLength(20).IsRequired();
        builder.Property(match => match.LocalDateText).HasMaxLength(40).IsRequired();
        builder.Property(match => match.StadiumExternalId).HasMaxLength(40).IsRequired();
        builder.Property(match => match.TimeElapsed).HasMaxLength(40).IsRequired();
        builder.Property(match => match.StageType).HasMaxLength(40).IsRequired();
        builder.Property(match => match.SyncedAtUtc).IsRequired();
        builder.HasIndex(match => new { match.ProviderName, match.ExternalId }).IsUnique();
    }
}

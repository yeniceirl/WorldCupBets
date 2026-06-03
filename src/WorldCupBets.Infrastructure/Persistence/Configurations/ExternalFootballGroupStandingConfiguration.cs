using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class ExternalFootballGroupStandingConfiguration : IEntityTypeConfiguration<ExternalFootballGroupStanding>
{
    public void Configure(EntityTypeBuilder<ExternalFootballGroupStanding> builder)
    {
        builder.ToTable("external_football_group_standings");
        builder.HasKey(standing => standing.Id);
        builder.Property(standing => standing.ProviderName).HasMaxLength(40).IsRequired();
        builder.Property(standing => standing.GroupName).HasMaxLength(10).IsRequired();
        builder.Property(standing => standing.TeamExternalId).HasMaxLength(40).IsRequired();
        builder.Property(standing => standing.SyncedAtUtc).IsRequired();
        builder.HasIndex(standing => new { standing.ProviderName, standing.GroupName, standing.TeamExternalId }).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class ExternalFootballTeamConfiguration : IEntityTypeConfiguration<ExternalFootballTeam>
{
    public void Configure(EntityTypeBuilder<ExternalFootballTeam> builder)
    {
        builder.ToTable("external_football_teams");
        builder.HasKey(team => team.Id);
        builder.Property(team => team.ProviderName).HasMaxLength(40).IsRequired();
        builder.Property(team => team.ExternalId).HasMaxLength(40).IsRequired();
        builder.Property(team => team.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(team => team.FifaCode).HasMaxLength(10).IsRequired();
        builder.Property(team => team.Iso2).HasMaxLength(10);
        builder.Property(team => team.GroupName).HasMaxLength(10);
        builder.Property(team => team.FlagUrl).HasMaxLength(300);
        builder.Property(team => team.SyncedAtUtc).IsRequired();
        builder.HasIndex(team => new { team.ProviderName, team.ExternalId }).IsUnique();
    }
}

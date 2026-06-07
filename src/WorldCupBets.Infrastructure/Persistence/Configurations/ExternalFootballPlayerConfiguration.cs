using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class ExternalFootballPlayerConfiguration : IEntityTypeConfiguration<ExternalFootballPlayer>
{
    public void Configure(EntityTypeBuilder<ExternalFootballPlayer> builder)
    {
        builder.ToTable("external_football_players");
        builder.HasKey(player => player.Id);
        builder.Property(player => player.ProviderName).HasMaxLength(40).IsRequired();
        builder.Property(player => player.ExternalId).HasMaxLength(40).IsRequired();
        builder.Property(player => player.Name).HasMaxLength(100).IsRequired();
        builder.Property(player => player.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(player => player.TeamExternalId).HasMaxLength(40).IsRequired();
        builder.Property(player => player.TeamName).HasMaxLength(100);
        builder.Property(player => player.Position).HasMaxLength(40);
        builder.Property(player => player.PhotoUrl).HasMaxLength(300);
        builder.Property(player => player.SyncedAtUtc).IsRequired();
        builder.HasIndex(player => new { player.ProviderName, player.ExternalId }).IsUnique();
        builder.HasIndex(player => player.NormalizedName);
    }
}

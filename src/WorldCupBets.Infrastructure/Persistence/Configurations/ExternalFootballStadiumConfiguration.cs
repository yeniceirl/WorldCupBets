using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class ExternalFootballStadiumConfiguration : IEntityTypeConfiguration<ExternalFootballStadium>
{
    public void Configure(EntityTypeBuilder<ExternalFootballStadium> builder)
    {
        builder.ToTable("external_football_stadiums");
        builder.HasKey(stadium => stadium.Id);
        builder.Property(stadium => stadium.ProviderName).HasMaxLength(40).IsRequired();
        builder.Property(stadium => stadium.ExternalId).HasMaxLength(40).IsRequired();
        builder.Property(stadium => stadium.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(stadium => stadium.FifaName).HasMaxLength(150);
        builder.Property(stadium => stadium.CityEn).HasMaxLength(150);
        builder.Property(stadium => stadium.CountryEn).HasMaxLength(100);
        builder.Property(stadium => stadium.Region).HasMaxLength(40);
        builder.Property(stadium => stadium.SyncedAtUtc).IsRequired();
        builder.HasIndex(stadium => new { stadium.ProviderName, stadium.ExternalId }).IsUnique();
    }
}

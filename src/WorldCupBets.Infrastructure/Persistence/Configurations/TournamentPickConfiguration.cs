using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class TournamentPickConfiguration : IEntityTypeConfiguration<TournamentPick>
{
    public void Configure(EntityTypeBuilder<TournamentPick> builder)
    {
        builder.ToTable("tournament_picks");
        builder.HasKey(tournamentPick => tournamentPick.Id);
        builder.Property(tournamentPick => tournamentPick.Category).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(tournamentPick => tournamentPick.SelectedText).HasMaxLength(160).IsRequired();
        builder.Property(tournamentPick => tournamentPick.ExternalId).HasMaxLength(80);
        builder.Property(tournamentPick => tournamentPick.StakeAmountCc).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(tournamentPick => tournamentPick.PlacedAtUtc).IsRequired();

        builder.HasIndex(tournamentPick => new { tournamentPick.UserId, tournamentPick.Category }).IsUnique();

        builder.HasOne(tournamentPick => tournamentPick.User)
            .WithMany()
            .HasForeignKey(tournamentPick => tournamentPick.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

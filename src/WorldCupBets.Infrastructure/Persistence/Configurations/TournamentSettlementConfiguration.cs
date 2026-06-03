using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Configurations;

public sealed class TournamentSettlementConfiguration : IEntityTypeConfiguration<TournamentSettlement>
{
    public void Configure(EntityTypeBuilder<TournamentSettlement> builder)
    {
        builder.ToTable("tournament_settlements");
        builder.HasKey(settlement => settlement.Id);
        builder.Property(settlement => settlement.Id).ValueGeneratedNever();
        builder.Property(settlement => settlement.ChampionJackpotCc).IsRequired();
        builder.Property(settlement => settlement.ChampionTeamName).HasMaxLength(100);
        builder.Property(settlement => settlement.ChampionSettledAtUtc);
        builder.Property(settlement => settlement.UndistributedJackpotCc).IsRequired();
        builder.Property(settlement => settlement.Version).IsConcurrencyToken().IsRequired();
    }
}

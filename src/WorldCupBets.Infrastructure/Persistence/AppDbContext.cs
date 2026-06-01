using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    public DbSet<ChampionBet> ChampionBets => Set<ChampionBet>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchBet> MatchBets => Set<MatchBet>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

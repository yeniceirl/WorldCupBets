using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly Type[] VersionedEntityTypes =
    [
        typeof(Match),
        typeof(MatchBet),
        typeof(TournamentSettlement),
        typeof(User)
    ];

    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    public DbSet<ExternalFootballTeam> ExternalFootballTeams => Set<ExternalFootballTeam>();

    public DbSet<ExternalFootballStadium> ExternalFootballStadiums => Set<ExternalFootballStadium>();

    public DbSet<ExternalFootballGroupStanding> ExternalFootballGroupStandings => Set<ExternalFootballGroupStanding>();

    public DbSet<ExternalFootballMatch> ExternalFootballMatches => Set<ExternalFootballMatch>();

    public DbSet<ExternalFootballPlayer> ExternalFootballPlayers => Set<ExternalFootballPlayer>();

    public DbSet<TournamentPick> TournamentPicks => Set<TournamentPick>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchBet> MatchBets => Set<MatchBet>();

    public DbSet<TournamentSettlement> TournamentSettlements => Set<TournamentSettlement>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IncrementConcurrencyVersions();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConflictException("The requested operation conflicted with another concurrent update.", exception);
        }
        catch (DbUpdateException exception) when (IsPostgresConcurrencyConflict(exception))
        {
            throw new PersistenceConflictException("The requested operation conflicted with another concurrent update.", exception);
        }
    }

    private void IncrementConcurrencyVersions()
    {
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State == EntityState.Modified && VersionedEntityTypes.Contains(entry.Entity.GetType())))
        {
            var version = entry.Property(nameof(User.Version));
            version.CurrentValue = (int)version.OriginalValue! + 1;
        }
    }

    private static bool IsPostgresConcurrencyConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation
        };
    }
}

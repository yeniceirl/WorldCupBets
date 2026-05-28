using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

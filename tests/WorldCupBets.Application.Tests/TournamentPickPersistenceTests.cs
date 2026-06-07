using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using WorldCupBets.Infrastructure.Persistence;
using WorldCupBets.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class TournamentPickPersistenceTests
{
    [Fact]
    public void AppDbContext_Maps_TournamentPick_To_TournamentPicks_Table_With_Category_Uniqueness()
    {
        using var dbContext = CreateDbContext();

        var entityType = dbContext.Model.FindEntityType(typeof(TournamentPick));

        Assert.NotNull(entityType);
        Assert.Equal("tournament_picks", entityType.GetTableName());
        Assert.Equal(40, entityType.FindProperty(nameof(TournamentPick.Category))?.GetMaxLength());
        Assert.Equal(160, entityType.FindProperty(nameof(TournamentPick.SelectedText))?.GetMaxLength());
        Assert.Equal(80, entityType.FindProperty(nameof(TournamentPick.ExternalId))?.GetMaxLength());
        Assert.Equal("numeric(18,2)", entityType.FindProperty(nameof(TournamentPick.StakeAmountCc))?.GetColumnType());
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(TournamentPick.UserId),
                nameof(TournamentPick.Category)
            ]));
    }

    [Fact]
    public void AppDbContext_Exposes_TournamentPicks_DbSet()
    {
        using var dbContext = CreateDbContext();

        Assert.Same(dbContext.Set<TournamentPick>(), dbContext.TournamentPicks);
    }

    [Fact]
    public void AppDbContext_Does_Not_Map_Replaced_Split_Tournament_Bet_Entities()
    {
        using var dbContext = CreateDbContext();

        Assert.Null(dbContext.Model.FindEntityType("WorldCupBets.Domain.Entities.ChampionBet"));
        Assert.Null(dbContext.Model.FindEntityType("WorldCupBets.Domain.Entities.SpecialPlayerBet"));
        Assert.DoesNotContain(typeof(AppDbContext).GetProperties(), property => property.Name is "ChampionBets" or "SpecialPlayerBets");
    }

    [Fact]
    public void AddTournamentPicks_Migration_Copies_And_Drops_Split_Tournament_Bet_Tables()
    {
        using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();

        var script = migrator.GenerateScript("20260605215144_AddSpecialPlayerBets", "20260607070825_AddTournamentPicks");

        Assert.Contains("INSERT INTO \"tournament_picks\"", script, StringComparison.Ordinal);
        Assert.Contains("FROM \"champion_bets\"", script, StringComparison.Ordinal);
        Assert.Contains("FROM \"special_player_bets\"", script, StringComparison.Ordinal);
        Assert.Contains("'Champion'", script, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE champion_bets", script, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE special_player_bets", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AddTournamentPicks_Down_Migration_Recreates_And_Splits_Split_Tournament_Bet_Tables()
    {
        using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();

        var script = migrator.GenerateScript("20260607070825_AddTournamentPicks", "20260605215144_AddSpecialPlayerBets");

        Assert.Contains("CREATE TABLE champion_bets", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE special_player_bets", script, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO \"champion_bets\"", script, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO \"special_player_bets\"", script, StringComparison.Ordinal);
        Assert.Contains("FROM \"tournament_picks\"", script, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE tournament_picks", script, StringComparison.Ordinal);
    }

    [Fact]
    public void TournamentPickRepository_Implements_TournamentPick_Contract()
    {
        Assert.IsAssignableFrom<ITournamentPickRepository>(new TournamentPickRepository(CreateDbContext()));
    }

    [Fact]
    public async Task TournamentPickRepository_Returns_Empty_User_Picks_When_No_Categories_Are_Requested()
    {
        var repository = new TournamentPickRepository(CreateDbContext());

        var picks = await repository.ListByUserAndCategoriesAsync(12, []);

        Assert.Empty(picks);
    }

    [Fact]
    public async Task TournamentPickRepository_Returns_Empty_Stake_Totals_When_No_Categories_Are_Requested()
    {
        var repository = new TournamentPickRepository(CreateDbContext());

        var totals = await repository.ListStakeAmountsByUserAsync([]);

        Assert.Empty(totals);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=worldcupbets_mapping_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}

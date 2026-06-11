using Microsoft.EntityFrameworkCore;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Infrastructure.Persistence;
using WorldCupBets.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ExternalFootballPlayerPersistenceTests
{
    [Fact]
    public void AppDbContext_Maps_ExternalFootballPlayer_To_External_Football_Players_Table_With_Indexes()
    {
        using var dbContext = CreateDbContext();

        var entityType = dbContext.Model.FindEntityType(typeof(ExternalFootballPlayer));

        Assert.NotNull(entityType);
        Assert.Equal("external_football_players", entityType.GetTableName());
        Assert.Equal(40, entityType.FindProperty(nameof(ExternalFootballPlayer.ProviderName))?.GetMaxLength());
        Assert.Equal(40, entityType.FindProperty(nameof(ExternalFootballPlayer.ExternalId))?.GetMaxLength());
        Assert.Equal(100, entityType.FindProperty(nameof(ExternalFootballPlayer.Name))?.GetMaxLength());
        Assert.Equal(100, entityType.FindProperty(nameof(ExternalFootballPlayer.NormalizedName))?.GetMaxLength());
        Assert.Equal(40, entityType.FindProperty(nameof(ExternalFootballPlayer.TeamExternalId))?.GetMaxLength());
        Assert.Equal(100, entityType.FindProperty(nameof(ExternalFootballPlayer.TeamName))?.GetMaxLength());
        Assert.Equal(40, entityType.FindProperty(nameof(ExternalFootballPlayer.Position))?.GetMaxLength());
        Assert.Equal(300, entityType.FindProperty(nameof(ExternalFootballPlayer.PhotoUrl))?.GetMaxLength());

        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(ExternalFootballPlayer.ProviderName),
                nameof(ExternalFootballPlayer.ExternalId)
            ]));

        Assert.Contains(entityType.GetIndexes(), index =>
            !index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(ExternalFootballPlayer.NormalizedName)
            ]));
    }

    [Fact]
    public void AppDbContext_Exposes_ExternalFootballPlayers_DbSet()
    {
        using var dbContext = CreateDbContext();

        Assert.Same(dbContext.Set<ExternalFootballPlayer>(), dbContext.ExternalFootballPlayers);
    }

    [Fact]
    public void ExternalFootballPlayerRepository_Implements_ExternalFootballPlayer_Contract()
    {
        Assert.IsAssignableFrom<IExternalFootballPlayerRepository>(new ExternalFootballPlayerRepository(CreateDbContext()));
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_When_Normalized_Query_Is_Blank()
    {
        var repository = new ExternalFootballPlayerRepository(CreateDbContext());

        var results = await repository.SearchAsync("api-sports", "   ");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_When_Normalized_Query_Is_Empty_String()
    {
        var repository = new ExternalFootballPlayerRepository(CreateDbContext());

        var results = await repository.SearchAsync("api-sports", string.Empty);

        Assert.Empty(results);
    }

    [Fact]
    public void ReplacePlayersAsync_Maps_Dto_Fields_Onto_Entity_Via_Create()
    {
        var dto = new ExternalFootballPlayerDto(
            "api-sports:1",
            "Lionel Messi",
            "lionel messi",
            "26",
            "Argentina",
            "Forward",
            "https://example.com/messi.png");

        var entity = ExternalFootballPlayer.Create(
            "api-sports",
            dto.ExternalId,
            dto.Name,
            dto.NormalizedName,
            dto.TeamExternalId,
            dto.TeamName,
            dto.Position,
            dto.PhotoUrl,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal("api-sports", entity.ProviderName);
        Assert.Equal(dto.ExternalId, entity.ExternalId);
        Assert.Equal(dto.Name, entity.Name);
        Assert.Equal(dto.NormalizedName, entity.NormalizedName);
        Assert.Equal(dto.TeamExternalId, entity.TeamExternalId);
        Assert.Equal(dto.TeamName, entity.TeamName);
        Assert.Equal(dto.Position, entity.Position);
        Assert.Equal(dto.PhotoUrl, entity.PhotoUrl);
    }

    [Fact]
    public void ExternalFootballPlayerDto_Round_Trips_All_Fields_For_Replace_And_Search()
    {
        var dto = new ExternalFootballPlayerDto(
            "api-sports:10",
            "Kylian Mbappé",
            "kylian mbappe",
            "2",
            "France",
            "Forward",
            "https://example.com/mbappe.png");

        Assert.Equal("api-sports:10", dto.ExternalId);
        Assert.Equal("Kylian Mbappé", dto.Name);
        Assert.Equal("kylian mbappe", dto.NormalizedName);
        Assert.Equal("2", dto.TeamExternalId);
        Assert.Equal("France", dto.TeamName);
        Assert.Equal("Forward", dto.Position);
        Assert.Equal("https://example.com/mbappe.png", dto.PhotoUrl);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=worldcupbets_mapping_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}

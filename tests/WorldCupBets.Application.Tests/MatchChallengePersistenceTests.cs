using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using WorldCupBets.Infrastructure.Persistence;
using WorldCupBets.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class MatchChallengePersistenceTests
{
    [Fact]
    public void AppDbContext_Maps_MatchChallenge_Tables_Columns_And_Indexes()
    {
        using var dbContext = CreateDbContext();

        var challengeType = dbContext.Model.FindEntityType(typeof(MatchChallenge));
        var positionType = dbContext.Model.FindEntityType(typeof(MatchChallengePosition));

        Assert.NotNull(challengeType);
        Assert.NotNull(positionType);
        Assert.Equal("match_challenges", challengeType.GetTableName());
        Assert.Equal("match_challenge_positions", positionType.GetTableName());
        Assert.Equal(MatchChallenge.MaxClaimTextLength, challengeType.FindProperty(nameof(MatchChallenge.ClaimText))?.GetMaxLength());
        Assert.Equal(MatchChallenge.MaxSideTextLength, challengeType.FindProperty(nameof(MatchChallenge.CreatorSideText))?.GetMaxLength());
        Assert.Equal("numeric(18,2)", challengeType.FindProperty(nameof(MatchChallenge.StakeAmountCc))?.GetColumnType());
        Assert.Equal(16, challengeType.FindProperty(nameof(MatchChallenge.Status))?.GetMaxLength());
        Assert.True(challengeType.FindProperty(nameof(MatchChallenge.Version))?.IsConcurrencyToken);
        Assert.Contains(challengeType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(MatchChallenge.MatchId),
                nameof(MatchChallenge.Status)
            ]));
        Assert.Contains(positionType.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(MatchChallengePosition.MatchChallengeId),
                nameof(MatchChallengePosition.Side)
            ]));
        Assert.Contains(positionType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(MatchChallengePosition.UserId)]));
    }

    [Fact]
    public void AppDbContext_Exposes_MatchChallenge_DbSets()
    {
        using var dbContext = CreateDbContext();

        Assert.Same(dbContext.Set<MatchChallenge>(), dbContext.MatchChallenges);
        Assert.Same(dbContext.Set<MatchChallengePosition>(), dbContext.MatchChallengePositions);
    }

    [Fact]
    public void AddMatchChallenges_Migration_Creates_Challenge_Tables_And_Indexes()
    {
        using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();

        var script = migrator.GenerateScript("20260607070825_AddTournamentPicks", "20260608042820_AddMatchChallenges");

        Assert.Contains("CREATE TABLE match_challenges", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE match_challenge_positions", script, StringComparison.Ordinal);
        Assert.Contains("IX_match_challenges_MatchId_Status", script, StringComparison.Ordinal);
        Assert.Contains("IX_match_challenge_positions_MatchChallengeId_Side", script, StringComparison.Ordinal);
        Assert.Contains("IX_match_challenge_positions_UserId", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchChallengeRepository_Implements_MatchChallenge_Contract()
    {
        Assert.IsAssignableFrom<IMatchChallengeRepository>(new MatchChallengeRepository(CreateDbContext()));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=worldcupbets_mapping_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}

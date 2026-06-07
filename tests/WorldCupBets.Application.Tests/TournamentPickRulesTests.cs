using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class TournamentPickRulesTests
{
    [Fact]
    public void CreateChampion_Stores_Team_As_Champion_Selection_Without_External_Id()
    {
        var placedAtUtc = new DateTime(2026, 6, 14, 18, 0, 0, DateTimeKind.Utc);

        var pick = TournamentPick.CreateChampion(7, " Argentina ", 50, placedAtUtc);

        Assert.Equal(7, pick.UserId);
        Assert.Equal(TournamentPickCategory.Champion, pick.Category);
        Assert.Equal("Argentina", pick.SelectedText);
        Assert.Null(pick.ExternalId);
        Assert.Equal(50, pick.StakeAmountCc);
        Assert.Equal(placedAtUtc, pick.PlacedAtUtc);
    }

    [Theory]
    [InlineData(TournamentPickCategory.BestPlayer, " Lionel Messi ", " 34146370 ", "34146370")]
    [InlineData(TournamentPickCategory.TopScorer, " Kylian Mbappe ", null, null)]
    public void CreatePlayer_Stores_Player_Category_Text_And_Optional_External_Id(
        TournamentPickCategory category,
        string playerName,
        string? externalPlayerId,
        string? expectedExternalId)
    {
        var placedAtUtc = new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc);

        var pick = TournamentPick.CreatePlayer(8, category, playerName, externalPlayerId, 50, placedAtUtc);

        Assert.Equal(8, pick.UserId);
        Assert.Equal(category, pick.Category);
        Assert.Equal(playerName.Trim(), pick.SelectedText);
        Assert.Equal(expectedExternalId, pick.ExternalId);
        Assert.Equal(50, pick.StakeAmountCc);
        Assert.Equal(placedAtUtc, pick.PlacedAtUtc);
    }

    [Fact]
    public void CreatePlayer_Rejects_Champion_Category()
    {
        var placedAtUtc = new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TournamentPick.CreatePlayer(8, TournamentPickCategory.Champion, "Argentina", null, 50, placedAtUtc));
    }

    [Fact]
    public void Repository_Contract_Exposes_Category_Aware_Tournament_Pick_Methods()
    {
        var methodNames = typeof(ITournamentPickRepository)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();

        Assert.Contains(nameof(ITournamentPickRepository.GetByUserAndCategoryAsync), methodNames);
        Assert.Contains(nameof(ITournamentPickRepository.GetTrackedByUserAndCategoryAsync), methodNames);
        Assert.Contains(nameof(ITournamentPickRepository.ListByUserAndCategoriesAsync), methodNames);
        Assert.Contains(nameof(ITournamentPickRepository.ListChampionForSettlementAsync), methodNames);
        Assert.Contains(nameof(ITournamentPickRepository.ListStakeAmountsByUserAsync), methodNames);
        Assert.Contains(nameof(ITournamentPickRepository.AddAsync), methodNames);
    }
}

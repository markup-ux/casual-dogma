using Arrowgene.Ddon.GameServer.GatheringItems;
using Xunit;

namespace Arrowgene.Ddon.Test.GameServer.GatheringItems;

public class ExplorationProgressionCatalogTest
{
    private const double AreaRankMultiplier = 2.5;

    [Fact]
    public void ResolveEffectiveTier_Level1WithMaxAreaRank_UsesPlayerLevel()
    {
        uint tier = ExplorationProgressionCatalog.ResolveEffectiveTier(
            playerLevel: 1,
            areaRank: 15,
            syncRecommendedLevel: 0,
            areaRankTierMultiplier: AreaRankMultiplier);

        Assert.Equal(1u, tier);
    }

    [Fact]
    public void ResolveEffectiveTier_OverLeveledInSyncDungeon_UsesSyncLevel()
    {
        uint tier = ExplorationProgressionCatalog.ResolveEffectiveTier(
            playerLevel: 60,
            areaRank: 15,
            syncRecommendedLevel: 36,
            areaRankTierMultiplier: AreaRankMultiplier);

        Assert.Equal(36u, tier);
    }

    [Fact]
    public void ResolveEffectiveTier_UnderLeveledInSyncDungeon_UsesPlayerLevel()
    {
        uint tier = ExplorationProgressionCatalog.ResolveEffectiveTier(
            playerLevel: 1,
            areaRank: 15,
            syncRecommendedLevel: 36,
            areaRankTierMultiplier: AreaRankMultiplier);

        Assert.Equal(1u, tier);
    }

    [Fact]
    public void ResolveEffectiveTier_OverLeveledInOpenWorld_UsesPlayerLevel()
    {
        uint tier = ExplorationProgressionCatalog.ResolveEffectiveTier(
            playerLevel: 60,
            areaRank: 15,
            syncRecommendedLevel: 0,
            areaRankTierMultiplier: AreaRankMultiplier);

        Assert.Equal(60u, tier);
    }
}

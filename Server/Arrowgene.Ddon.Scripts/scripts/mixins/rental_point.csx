#load "libs.csx"

public class Mixin : IRentalPointMixin
{
    private static readonly ILogger Logger = LogProvider.Logger(typeof(Mixin));

    public static readonly uint LEVEL_REWARD = 1000; // 100 JP
    public static readonly uint ALL_ADVENTURE_REWARD = 2000; // 200 JP
    public static readonly uint ALL_CRAFT_REWARD = 2000; // 200 JP

    public override uint GetRentalPointReward(GameClient client, RentalPawn returningPawn)
    {
        // Low level pawns give less base RP than high level pawns.
        double fLevel = (double) returningPawn.ActiveCharacterJobData.Lv / LibDdon.GetSetting<uint>("GameServerSettings", "JobLevelMax");

        // Additional rewards for using a pawn to adventure/craft.
        double fAdventure = (double) (returningPawn.MaxAdventureCount - returningPawn.AdventureCount) / returningPawn.MaxAdventureCount;
        double fCraft = (double) (returningPawn.MaxCraftCount - returningPawn.CraftCount) / returningPawn.MaxCraftCount;

        return (uint)(LEVEL_REWARD * fLevel + ALL_ADVENTURE_REWARD * fAdventure + ALL_CRAFT_REWARD * fCraft);
    }
}

return new Mixin();

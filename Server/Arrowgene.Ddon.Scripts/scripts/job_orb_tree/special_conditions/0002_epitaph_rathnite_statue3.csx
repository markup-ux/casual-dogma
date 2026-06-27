#load "libs.csx"

public class SpecialCondition : IJobOrbSpecialCondition
{
    public override uint ConditionId => 2;
    public override string Message => "Epitaph Road (Rathnite): Cave Depths Trial";

    public override bool EvaluateCondition(GameClient client)
    {
        return LibDdon.EpitaphRoadMgr.IsStatueUnlocked(client, Stage.HeroicSpiritSleepingPathCave, 88, 0);
    }
}

return new SpecialCondition();

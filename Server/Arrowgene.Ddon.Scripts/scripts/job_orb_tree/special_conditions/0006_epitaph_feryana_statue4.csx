#load "libs.csx"

public class SpecialCondition : IJobOrbSpecialCondition
{
    public override uint ConditionId => 6;
    public override string Message => "Epitaph Road (Feryana): Ruins Depths Trial";
    
    public override bool EvaluateCondition(GameClient client)
    {
        return LibDdon.EpitaphRoadMgr.IsStatueUnlocked(client, Stage.HeroicSpiritSleepingPathRuinsDeepestLevel, 101, 0);
    }
}

return new SpecialCondition();

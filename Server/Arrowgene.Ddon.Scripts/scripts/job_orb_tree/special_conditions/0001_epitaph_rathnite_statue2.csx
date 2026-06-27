#load "libs.csx"

public class SpecialCondition : IJobOrbSpecialCondition
{
    public override uint ConditionId => 1;
    public override string Message => "Epitaph Road (Rathnite): Cave Trial";
    
    public override bool EvaluateCondition(GameClient client)
    {
        return LibDdon.EpitaphRoadMgr.IsStatueUnlocked(client, Stage.HeroicSpiritSleepingPathCave, 38, 0);
    }
}

return new SpecialCondition();

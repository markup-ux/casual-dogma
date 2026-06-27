#load "libs.csx"

public class SpecialCondition : IJobOrbSpecialCondition
{
    public override uint ConditionId => 5;
    public override string Message => "Epitaph Road (Feryana): Tomb Trial";
    
    public override bool EvaluateCondition(GameClient client)
    {
        return LibDdon.EpitaphRoadMgr.IsStatueUnlocked(client, Stage.HeroicSpiritSleepingPathTomb, 101, 0);
    }
}

return new SpecialCondition();

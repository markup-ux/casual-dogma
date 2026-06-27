#load "libs.csx"

public class SpecialCondition : IJobOrbSpecialCondition
{
    public override uint ConditionId => 4;
    public override string Message => "Epitaph Road (Feryana): Well Trial";
    
    public override bool EvaluateCondition(GameClient client)
    {
        return LibDdon.EpitaphRoadMgr.IsStatueUnlocked(client, Stage.HeroicSpiritSleepingPathWell, 101, 0);
    }
}

return new SpecialCondition();

#load "libs.csx"

public class Mixin : IBitterblackEarringMixin
{
    public override ushort RollBitterBlackMazeEarringPercent(JobId jobId, Random rng)
    {
        // Warrior and Shield Sage earrings roll a higher appraisal range.
        if (jobId == JobId.Warrior || jobId == JobId.ShieldSage)
            return (ushort)rng.Next(8, 20 + 1);

        return (ushort)rng.Next(1, 13 + 1);
    }
}

return new Mixin();

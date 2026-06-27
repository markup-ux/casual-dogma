using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Model.Quest;

public class StagedRewardItemCrest
{
    public string Uid { get; set; } = "";
    public uint Slot { get; set; }
    public uint CrestId { get; set; }
    public uint Level { get; set; } = 0;
}

public class StagedRewardItem
{
    public string Uid { get; set; } = "";
    public long RewardBoxItemId { get; set; }
    public uint ItemId { get; set; }
    public uint Num { get; set; } = 1;
    public uint Color { get; set; }
    public uint PlusValue { get; set; }
    public uint SafetySetting { get; set; }
    public List<StagedRewardItemCrest> Crests { get; set; } = new();
}

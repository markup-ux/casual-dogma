using System;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Model;

public class SupplyCache
{
    public const uint SetIdBase = 0x80000000;

    public long Id { get; set; }
    public uint MapId { get; set; }
    public byte LayerNo { get; set; }
    public uint GroupId { get; set; }
    public double X { get; set; }
    public float Y { get; set; }
    public double Z { get; set; }
    public float Rotation { get; set; }
    public uint SetId { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<(ushort Slot, SupplyCacheItemData Item)> Items { get; set; } = [];

    public StageLayoutId StageLayoutId => new StageLayoutId(MapId, LayerNo, GroupId);

    public static uint MakeSetId(long cacheId) => SetIdBase | (uint)(cacheId & 0x7FFFFFFF);

    /// <summary>
    /// The client sends PopDropItemNtc setIds masked to 31 bits when opening a drop bag.
    /// </summary>
    public static uint ResolveSetId(uint setId) =>
        setId >= SetIdBase ? setId : MakeSetId(setId);

    public static bool IsSupplyCacheSetId(uint setId) => setId >= SetIdBase;
}

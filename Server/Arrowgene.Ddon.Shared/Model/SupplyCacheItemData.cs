using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.Shared.Model;

/// <summary>
/// JSON-serialized item instance stored inside a supply cache slot.
/// </summary>
public class SupplyCacheItemData
{
    public uint ItemId { get; set; }
    public uint Num { get; set; }
    public byte SafetySetting { get; set; }
    public byte Color { get; set; }
    public byte PlusValue { get; set; }
    public uint EquipPoints { get; set; }
    public List<CDataEquipElementParam> EquipElementParamList { get; set; } = [];
    public List<CDataAddStatusParam> AddStatusParamList { get; set; } = [];
    public List<CDataEquipStatParam> EquipStatParamList { get; set; } = [];

    public static SupplyCacheItemData FromItem(Item item, uint num)
    {
        return new SupplyCacheItemData
        {
            ItemId = item.ItemId,
            Num = num,
            SafetySetting = item.SafetySetting,
            Color = item.Color,
            PlusValue = item.PlusValue,
            EquipPoints = item.EquipPoints,
            EquipElementParamList = [.. item.EquipElementParamList.Select(x => new CDataEquipElementParam(x))],
            AddStatusParamList = [.. item.AddStatusParamList.Select(x => new CDataAddStatusParam(x))],
            EquipStatParamList = [.. item.EquipStatParamList.Select(x => new CDataEquipStatParam(x))],
        };
    }

    public Item ToItem()
    {
        return new Item
        {
            ItemId = ItemId,
            SafetySetting = SafetySetting,
            Color = Color,
            PlusValue = PlusValue,
            EquipPoints = EquipPoints,
            EquipElementParamList = [.. EquipElementParamList.Select(x => new CDataEquipElementParam(x))],
            AddStatusParamList = [.. AddStatusParamList.Select(x => new CDataAddStatusParam(x))],
            EquipStatParamList = [.. EquipStatParamList.Select(x => new CDataEquipStatParam(x))],
        };
    }
}

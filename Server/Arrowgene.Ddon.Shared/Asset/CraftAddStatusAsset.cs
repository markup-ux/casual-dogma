using Arrowgene.Ddon.Shared.Entity.Structure;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Asset
{
    public class CraftAddStatusAsset
    {
        public Dictionary<ushort, CraftAddStatus> AddStatuses { get; set; } = [];
    }

    public class CraftAddStatus
    {
        public ushort Index { get; set; }
        public byte Category { get; set; }
        public ushort BuffId { get; set; }
        public List<CDataItemAmount> ItemCost { get; set; } = [];

        public CDataEquipEnhanceLotteryOption CDataEquipEnhanceLotteryOption
        {
            get => new()
            {
                Index = Index,
                Category = Category,
                ItemCost = ItemCost,
                ShopTypeListings = [new(1)],
                MainSuccessExample = [
                    new()
                    {
                        Unk0 = 1,
                        EffectParamList = [
                            new() {
                                BuffId = BuffId,
                                Unk1 = 100,
                                Unk2 = 1,
                            }
                        ]
                    }
                ]
            };
        }
    }
}

using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;
using System.Text.Json;

namespace Arrowgene.Ddon.Shared.AssetReader
{
    public class CraftAddStatusAssetReader : IAssetDeserializer<CraftAddStatusAsset>
    {
        private static readonly ILogger Logger = LogProvider.Logger(typeof(CraftAddStatusAssetReader));

        public CraftAddStatusAsset ReadPath(string path)
        {
            Logger.Info($"Reading {path}");

            CraftAddStatusAsset asset = new();

            string json = Util.ReadAllText(path);
            JsonDocument document = JsonDocument.Parse(json);

            asset.AddStatuses = document.RootElement
                .EnumerateArray()
                .Select(jAddStatusData => new CraftAddStatus()
                {
                    Index = jAddStatusData.GetProperty("index").GetUInt16(),
                    Category = jAddStatusData.GetProperty("sort_index").GetByte(),
                    BuffId = jAddStatusData.GetProperty("buff_id").GetUInt16(),
                    ItemCost = [.. jAddStatusData.GetProperty("item_cost")
                        .EnumerateArray()
                        .Select(jItemCost => new Entity.Structure.CDataItemAmount()
                        {
                            ItemId = (ItemId)jItemCost.GetProperty("item_id").GetUInt32(),
                            Num = jItemCost.GetProperty("num").GetUInt16(),
                        })
                    ]
                })
                .ToDictionary(key => key.Index, val => val);

            return asset;
        }
    }
}

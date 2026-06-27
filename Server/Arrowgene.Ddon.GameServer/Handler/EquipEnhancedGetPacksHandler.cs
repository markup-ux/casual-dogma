using Arrowgene.Ddon.GameServer.Shop;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class EquipEnhancedGetPacks : GameRequestPacketHandler<C2SEquipEnhancedGetPacksReq, S2CEquipEnhancedGetPacksRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(EquipEnhancedGetPacks));
        
        public EquipEnhancedGetPacks(DdonGameServer server) : base(server)
        {
        }

        public override S2CEquipEnhancedGetPacksRes Handle(GameClient client, C2SEquipEnhancedGetPacksReq request)
        {
            S2CEquipEnhancedGetPacksRes res = new();

            // TODO: Figure out Ultimate Synthesis.
            // The response needs to be structured differently to get items to show up? Might be the item data in Unk8?

            switch (request.EnhanceType)
            {
                case EquipEnhanceType.LimitBreak:
                    res.ParamList = Server.AssetRepository.LimitBreakAsset.ToLotteryExampleList();
                    break;
                case EquipEnhanceType.UltimateSynthesis:
                    throw new ResponseErrorException(ErrorCode.ERROR_CODE_NOT_IMPLEMENTED, "Ultimate Synthesis is not implemented yet.");
                case EquipEnhanceType.AdditionalCraftMaterial:
                    res.ParamList = [.. Server.AssetRepository.CraftAddStatusAsset.AddStatuses.Values.Select(x => x.CDataEquipEnhanceLotteryOption)];
                    break;
                default:
                    throw new ResponseErrorException(ErrorCode.ERROR_CODE_NOT_IMPLEMENTED, "Unknown EquipEnhanceType.");
            }

            return res;
        }
    }
}

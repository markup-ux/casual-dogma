using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class EquipGetCraftLockedElementListHandler : GameRequestPacketHandler<C2SEquipGetCraftLockedElementListReq, S2CEquipGetCraftLockedElementListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(EquipGetCraftLockedElementListHandler));

        public EquipGetCraftLockedElementListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CEquipGetCraftLockedElementListRes Handle(GameClient client, C2SEquipGetCraftLockedElementListReq request)
        {
            // TODO: Does this need to be ALL storages?
            S2CEquipGetCraftLockedElementListRes res = new()
            {
                LockedElementList = [.. client.Character.Storage
                    .GetAllStorages()
                    .Values
                    .SelectMany(storage =>
                        storage.Items
                            .Where(item => item is not null && item.Item1.SafetySetting > 0)
                            .Select(item => new CDataItemEquipElement()
                            {
                                ItemUID = item.Item1.UId,
                                EquipElementList = item.Item1.EquipElementParamList
                            })
                        )]
            };

            return res;
        }
    }
}

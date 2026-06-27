using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Characters
{
    public static class RemoteStorageManager
    {
        /// <summary>
        /// Opens the storage box UI for the client.
        /// IsStart=true enters storage mode; ChangeList must be empty here (sending slot counts crashes the client).
        /// </summary>
        public static void OpenStorageBox(GameClient client)
        {
            client.Send(new S2CItemSwitchStorageNtc()
            {
                Unk0 = 0,
                IsStart = true,
                ChangeList = new List<CDataSwitchStorage>()
            });
        }
    }
}

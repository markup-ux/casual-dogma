using System.Collections.Generic;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class LobbyLobbyDataMsgHandler : StructurePacketHandler<GameClient, C2SLobbyLobbyDataMsgReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(LobbyLobbyDataMsgHandler));

        private readonly PartyManager _PartyManager;
        private readonly DdonGameServer _gameServer;

        public LobbyLobbyDataMsgHandler(DdonGameServer server) : base(server)
        {
            _PartyManager = server.PartyManager;
            _gameServer = server;
        }

        public override void Handle(GameClient client, StructurePacket<C2SLobbyLobbyDataMsgReq> packet)
        {
            byte[] rpcPacket = packet.Structure.RpcPacket;
            bool echoProtectedRpcToClient = false;

            if (_gameServer.RecoverableHpManager.ShouldPin(client))
            {
                rpcPacket = (byte[])packet.Structure.RpcPacket.Clone();
                echoProtectedRpcToClient = _gameServer.RecoverableHpManager.TryPatchPeriodicRpc(client, rpcPacket);
            }
            else
            {
                _gameServer.RecoverableHpManager.ClearProtectionState(client);
            }

            S2CLobbyLobbyDataMsgNotice res = new S2CLobbyLobbyDataMsgNotice();
            res.Type = packet.Structure.Type;
            res.CharacterId = client.Character.CharacterId;
            res.RpcPacket = rpcPacket;
            res.OnlineStatus = client.Character.OnlineStatus;

            if (!StageManager.IsHubArea(client.Character.Stage))
            {
                if (client.Party != null)
                {
                    client.Party.SendToAllExcept(res, client);
                }
            }
            else
            {
                client.LastLobbyDataMsg = res;

                foreach (GameClient otherClient in Server.ClientLookup.GetAll())
                {
                    if (otherClient == null || otherClient == client || otherClient.Character == null)
                    {
                        continue;
                    }

                    if (client.Character.Stage.Id == 347 && client.Character.ClanId != otherClient.Character.ClanId)
                    {
                        continue;
                    }

                    if (client.Character.Stage.Id == otherClient.Character.Stage.Id || _PartyManager.ClientsInSameParty(client, otherClient))
                    {
                        otherClient.Send(res);
                    }
                }
            }

            // The sender does not receive the party/hub relay; echo corrected white HP locally.
            if (echoProtectedRpcToClient)
            {
                client.Send(res);
            }

            RpcHandler.Handle(client, packet.Structure.Type, rpcPacket, _gameServer);
        }
    }
}

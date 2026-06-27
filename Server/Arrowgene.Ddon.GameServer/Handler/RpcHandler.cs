using Arrowgene.Buffers;
using Arrowgene.Ddon.GameServer;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.RpcPacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class RpcHandler
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(RpcHandler));

        public static void Handle(GameClient client, RpcMsgType msgType, byte[] rpcData, DdonGameServer server)
        {
            IBuffer buffer = new StreamBuffer(rpcData);
            buffer.SetPositionStart();
            
            // It seems like MsgIdFull  is almost like a "message class"
            // where RpcId is a unique action?
            RpcPacketHeader Header = new RpcPacketHeader().Read(buffer);

            // Logger.Debug(Header.AsString());
            if (gRpcPacketHandlers.ContainsKey(Header.MsgDTI) && gRpcPacketHandlers[Header.MsgDTI].ContainsKey(Header.MsgId))
            {
                gRpcPacketHandlers[Header.MsgDTI][Header.MsgId].Handle(client.Character, Header, buffer);

                if (Header.MsgDTI == RpcNetMsgDti.cNetMsgCtrlAction
                    && Header.MsgId == (ushort)RpcMsgIdControl.NET_MSG_ID_PERIODIC_TOP)
                {
                    server.RecoverableHpManager.ClampPeriodicUpdate(client);
                    server.SupplyCacheManager.HandlePeriodicPositionUpdate(client);
                }
            }
        }

        public static readonly Dictionary<RpcNetMsgDti, Dictionary<ushort, IRpcPacket>> gRpcPacketHandlers = new()
        {
            [RpcNetMsgDti.cNetMsgCtrlAction] = new Dictionary<ushort, IRpcPacket>
            {
                {(ushort) RpcMsgIdControl.NET_MSG_ID_PERIODIC_TOP, new RpcCtrlPeriodicTop()},
                {(ushort) RpcMsgIdControl.NET_MSG_ID_CS_CHANGE, new RpcCtrlCsChange()}
            }
        };
    }
}

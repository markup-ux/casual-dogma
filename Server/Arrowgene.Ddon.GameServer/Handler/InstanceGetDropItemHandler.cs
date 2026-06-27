using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class InstanceGetDropItemHandler : GameRequestPacketQueueHandler<C2SInstanceGetDropItemReq, S2CInstanceGetDropItemRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(InstanceGetDropItemHandler));
        
        public InstanceGetDropItemHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketQueue Handle(GameClient client, C2SInstanceGetDropItemReq request)
        {
            PacketQueue queue = new();
            StageLayoutId layout = request.LayoutId.AsStageLayoutId();
            Server.SupplyCacheManager.AdoptClientLayout(client, layout);

            if (Server.SupplyCacheManager.TryResolveCache(client, layout, request.SetId, out SupplyCache? cache)
                && cache != null)
            {
                long cacheId = cache.Id;

                S2CItemUpdateCharacterItemNtc ntc = new()
                {
                    UpdateType = ItemNoticeType.Drop
                };

                Server.Database.ExecuteInTransaction(connection =>
                {
                    foreach (CDataGatheringItemGetRequest gatheringItemRequest in request.GatheringItemGetRequestList
                                 .OrderByDescending(x => x.SlotNo))
                    {
                        Server.SupplyCacheManager.TryLoot(client, cache, gatheringItemRequest.SlotNo, gatheringItemRequest.Num, ntc, connection);
                    }
                });

                if (ntc.UpdateItemList.Count > 0)
                {
                    client.Enqueue(ntc, queue);
                }

                Server.SupplyCacheManager.FinalizeSupplyCacheLoot(client, layout, request.SetId, cacheId, queue);

                client.Enqueue(new S2CInstanceGetDropItemRes()
                {
                    LayoutId = request.LayoutId,
                    SetId = request.SetId,
                    GatheringItemGetRequestList = request.GatheringItemGetRequestList
                }, queue);

                return queue;
            }

            if (client.SupplyCacheDropTracker.IsConsumed(request.SetId))
            {
                client.Enqueue(new S2CInstanceGetDropItemRes()
                {
                    LayoutId = request.LayoutId,
                    SetId = request.SetId,
                    GatheringItemGetRequestList = request.GatheringItemGetRequestList
                }, queue);
                return queue;
            }

            var items = client.InstanceDropItemManager.Fetch(request.LayoutId, request.SetId);

            S2CItemUpdateCharacterItemNtc dropNtc = new()
            {
                UpdateType = ItemNoticeType.Drop
            };

            Server.Database.ExecuteInTransaction(connection =>
            {
                foreach (CDataGatheringItemGetRequest gatheringItemRequest in request.GatheringItemGetRequestList)
                {
                    InstancedGatheringItem dropItem = items[(int)gatheringItemRequest.SlotNo];
                    queue.AddRange(Server.ItemManager.GatherItem(client, dropNtc, dropItem, gatheringItemRequest.Num, connection));
                }
            });
            client.Enqueue(dropNtc, queue);

            client.Enqueue(new S2CInstanceGetDropItemRes() 
            {
                LayoutId = request.LayoutId,
                SetId = request.SetId,
                GatheringItemGetRequestList = request.GatheringItemGetRequestList
            }, queue);
            return queue;
        }
    }
}

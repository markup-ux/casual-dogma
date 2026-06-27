using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.GatheringItems;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class InstanceGetDropItemListHandler : GameRequestPacketQueueHandler<C2SInstanceGetDropItemListReq, S2CInstanceGetDropItemListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(InstanceGetDropItemListHandler));

        public InstanceGetDropItemListHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketQueue Handle(GameClient client, C2SInstanceGetDropItemListReq request)
        {
            PacketQueue queue = new();
            StageLayoutId layout = request.LayoutId.AsStageLayoutId();
            Server.SupplyCacheManager.AdoptClientLayout(client, layout);

            client.SupplyCacheDropTracker.TryGetCacheId(request.SetId, out long mappedAtStart);
            Logger.Info(
                $"[SUPPLY_CACHE_INTERACT] request cha={client.Character.CharacterId} setId={request.SetId} layout={layout} " +
                $"mappedCache={mappedAtStart} consumed={client.SupplyCacheDropTracker.IsConsumed(request.SetId)} " +
                $"needsReg={client.SupplyCacheDropTracker.NeedsDropSetRegistration(request.SetId)}");

            if (SupplyCacheDropTracker.IsPlayerWireSetId(request.SetId))
            {
                HandlePlayerWireDropList(client, request, layout, queue);
                return queue;
            }

            if (client.SupplyCacheDropTracker.IsConsumed(request.SetId))
            {
                LogDropList(client, request, SupplyCacheDropListOutcome.EmptyConsumed, 0, null, false);
                client.Enqueue(CreateEmptyDropListResponse(request), queue);
                return queue;
            }

            if (client.SupplyCacheDropTracker.TryGetCacheId(request.SetId, out long mappedCacheId)
                && !Server.SupplyCacheManager.TryGetCacheById(mappedCacheId, out _))
            {
                client.SupplyCacheDropTracker.MarkConsumed(request.SetId);
                LogDropList(client, request, SupplyCacheDropListOutcome.EmptyStaleCache, 0, mappedCacheId, false);
                client.Enqueue(CreateEmptyDropListResponse(request), queue);
                return queue;
            }

            if (TryResolveSupplyCacheDropList(client, request, out SupplyCache? cache))
            {
                EnqueueSupplyCacheDropList(client, request, layout, cache, queue);
                return queue;
            }

            if (IsSupplyCacheSetIdRequest(request.SetId))
            {
                LogDropList(client, request, SupplyCacheDropListOutcome.SupplyCacheMissing, 0, null, false);
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_INSTANCE_AREA_DROP_MISSING,
                    $"Missing supply cache for setId={request.SetId} layout={request.LayoutId}");
            }

            HandleEnemyDropList(client, request, queue);
            return queue;
        }

        private void HandlePlayerWireDropList(
            GameClient client,
            C2SInstanceGetDropItemListReq request,
            StageLayoutId layout,
            PacketQueue queue)
        {
            if (client.SupplyCacheDropTracker.IsConsumed(request.SetId))
            {
                LogDropList(client, request, SupplyCacheDropListOutcome.EmptyConsumed, 0, null, false);
                client.Enqueue(CreateEmptyDropListResponse(request), queue);
                return;
            }

            if (TryResolveSupplyCacheDropList(client, request, out SupplyCache? cache))
            {
                EnqueueSupplyCacheDropList(client, request, layout, cache, queue);
                return;
            }

            client.SupplyCacheDropTracker.MarkConsumed(request.SetId);
            LogDropList(client, request, SupplyCacheDropListOutcome.EmptyStaleCache, 0, null, false);
            client.Enqueue(CreateEmptyDropListResponse(request), queue);
        }

        private void EnqueueSupplyCacheDropList(
            GameClient client,
            C2SInstanceGetDropItemListReq request,
            StageLayoutId layout,
            SupplyCache cache,
            PacketQueue queue)
        {
            List<CDataGatheringItemElement> itemList =
                Server.SupplyCacheManager.GetDropList(cache, request.SetId, client.SupplyCacheDropTracker);

            client.SupplyCacheDropTracker.TryGetCacheId(request.SetId, out long mappedCacheId);
            bool needsRegistration = client.SupplyCacheDropTracker.NeedsDropSetRegistration(request.SetId);

            Logger.Info(
                $"[SUPPLY_CACHE_INTERACT] cha={client.Character.CharacterId} setId={request.SetId} layout={layout} " +
                $"cache={cache.Id} mappedCache={mappedCacheId} items={itemList.Count} needsReg={needsRegistration}");

            if (needsRegistration)
            {
                if (!Server.SupplyCacheManager.SendFocusedDropSetRegistration(client, layout, request.SetId))
                {
                    Logger.Error(
                        $"[SUPPLY_CACHE_INTERACT] drop-set registration skipped cha={client.Character.CharacterId} " +
                        $"setId={request.SetId} layout={layout} group={layout.GroupId}");
                    SupplyCacheEventLog.Record(
                        client,
                        "interact_reg_skip",
                        $"wire={request.SetId} layout={layout} cache={cache.Id} group={layout.GroupId}");
                }
                else
                {
                    client.SupplyCacheDropTracker.ClearDropSetRegistration(request.SetId);
                }
            }

            LogDropList(
                client,
                request,
                SupplyCacheDropListOutcome.SupplyCacheList,
                itemList.Count,
                cache.Id,
                false,
                "direct",
                needsRegistration,
                mappedCacheId);
            client.Enqueue(CreateDropListResponse(request, itemList), queue);
        }

        private void HandleEnemyDropList(
            GameClient client,
            C2SInstanceGetDropItemListReq request,
            PacketQueue queue)
        {
            List<InstancedGatheringItem> items;
            try
            {
                items = client.InstanceDropItemManager.Fetch(request.LayoutId, request.SetId);
            }
            catch (ResponseErrorException ex) when (ex.ErrorCode == ErrorCode.ERROR_CODE_INSTANCE_AREA_DROP_MISSING)
            {
                LogDropList(client, request, SupplyCacheDropListOutcome.EmptyStaleCache, 0, null, false);
                client.Enqueue(CreateEmptyDropListResponse(request), queue);
                return;
            }

            S2CItemUpdateCharacterItemNtc ntc = new()
            {
                UpdateType = ItemNoticeType.Drop
            };

            Server.Database.ExecuteInTransaction(connection =>
            {
                foreach (InstancedGatheringItem dropItem in items.Where(x => x.ItemNum > 0))
                {
                    Server.ItemManager.GatherItem(client, ntc, dropItem, dropItem.ItemNum, connection);
                }
            });

            bool autoLooted = ntc.UpdateItemList.Any();
            if (autoLooted)
            {
                client.Enqueue(ntc, queue);
            }

            LogDropList(
                client,
                request,
                autoLooted ? SupplyCacheDropListOutcome.EnemyAutoLoot : SupplyCacheDropListOutcome.EnemyList,
                items.Count,
                null,
                autoLooted);

            client.Enqueue(new S2CInstanceGetDropItemListRes()
            {
                LayoutId = request.LayoutId,
                SetId = request.SetId,
                ItemList = items
                    .Select((dropItem, index) => new CDataGatheringItemElement()
                    {
                        SlotNo = (uint)index,
                        ItemId = (uint)dropItem.ItemId,
                        ItemNum = dropItem.ItemNum,
                        Quality = dropItem.Quality,
                        IsHidden = dropItem.IsHidden
                    })
                    .ToList()
            }, queue);
        }

        private void LogDropList(
            GameClient client,
            C2SInstanceGetDropItemListReq request,
            SupplyCacheDropListOutcome outcome,
            int itemCount,
            long? cacheId,
            bool autoLooted,
            string responsePath = "",
            bool neededRegistration = false,
            long mappedCacheId = 0)
        {
            client.SupplyCacheDiagnostics.RecordGetDropItemList(
                Server,
                client,
                request.SetId,
                request.LayoutId,
                outcome,
                itemCount,
                cacheId,
                autoLooted,
                responsePath,
                neededRegistration,
                mappedCacheId == 0 ? null : mappedCacheId);
        }

        private static bool IsSupplyCacheSetIdRequest(uint setId) => SupplyCache.IsSupplyCacheSetId(setId);

        private bool TryResolveSupplyCacheDropList(
            GameClient client,
            C2SInstanceGetDropItemListReq request,
            out SupplyCache cache)
        {
            if (Server.SupplyCacheManager.TryResolveCache(client, request.LayoutId.AsStageLayoutId(), request.SetId, out SupplyCache? resolved)
                && resolved != null)
            {
                cache = resolved;
                return true;
            }

            uint mapId = request.LayoutId.StageId;

            if (client.SupplyCacheDropTracker.TryGetCacheId(request.SetId, out long mappedCacheId)
                && Server.SupplyCacheManager.TryGetCacheById(mappedCacheId, out SupplyCache? mappedCache)
                && mappedCache != null
                && mappedCache.MapId == mapId)
            {
                cache = mappedCache;
                return true;
            }

            if (SupplyCache.IsSupplyCacheSetId(request.SetId)
                && Server.SupplyCacheManager.TryGetCache(request.SetId, out SupplyCache? worldCache)
                && worldCache != null
                && worldCache.MapId == mapId)
            {
                cache = worldCache;
                return true;
            }

            cache = null!;
            return false;
        }

        private static S2CInstanceGetDropItemListRes CreateEmptyDropListResponse(C2SInstanceGetDropItemListReq request)
        {
            return new S2CInstanceGetDropItemListRes
            {
                LayoutId = request.LayoutId,
                SetId = request.SetId,
                ItemList = [],
            };
        }

        private static S2CInstanceGetDropItemListRes CreateDropListResponse(
            C2SInstanceGetDropItemListReq request,
            List<CDataGatheringItemElement> itemList)
        {
            return new S2CInstanceGetDropItemListRes
            {
                LayoutId = request.LayoutId,
                SetId = request.SetId,
                ItemList = itemList,
            };
        }
    }
}

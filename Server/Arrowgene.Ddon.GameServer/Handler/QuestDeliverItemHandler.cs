using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestDeliverItemHandler : GameRequestPacketHandler<C2SQuestDeliverItemReq, S2CQuestDeliverItemRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestDeliverItemHandler));

        public QuestDeliverItemHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CQuestDeliverItemRes Handle(GameClient client, C2SQuestDeliverItemReq request)
        {
            S2CQuestDeliverItemRes res = new S2CQuestDeliverItemRes()
            {
                QuestScheduleId = request.QuestScheduleId,
                ProcessNo = request.ProcessNo,
            };

            var quest = QuestManager.GetQuestByScheduleId(request.QuestScheduleId);
            var questStateManager = QuestManager.GetQuestStateManager(client, quest);
            var questState = questStateManager.GetQuestState(request.QuestScheduleId);

            Dictionary<uint, CDataDeliveredItem> deliveredItems = new Dictionary<uint, CDataDeliveredItem>();
            List<CDataItemUpdateResult> itemUpdateResults = new List<CDataItemUpdateResult>();
            // Holds validated (remaining, newTotal) per item, populated inside the transaction,
            // applied to in-memory state only after the transaction commits successfully.
            Dictionary<uint, (uint Remaining, uint NewTotal)> deliveryCommits = new Dictionary<uint, (uint, uint)>();
            Server.Database.ExecuteInTransaction(connection =>
            {
                var deliveryProgressCommonIds = GetDeliveryProgressCommonIds(client, quest.IsPersonal, request.QuestScheduleId, connection);
                foreach (var item in request.ItemUIDList)
                {
                    uint itemId = client.Character.Storage.FindItemByUIdInStorage(ItemManager.AllItemStorages, item.ItemUID)?.Item2.Item2.ItemId
                        ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_NOT_FOUND, $"Could not find item {item.ItemUID}.");
                    var searchResult = client.Character.Storage.FindItemByUIdInStorage(ItemManager.BothStorageTypes, item.ItemUID);
                    var itemUpdate = Server.ItemManager.ConsumeItemByUId(Server, client.Character, searchResult.Item1, item.ItemUID, item.Num, connectionIn: connection)
                        ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_QUEST_DONT_HAVE_DELIVERY_ITEM);

                    itemUpdateResults.Add(itemUpdate);

                    if (!deliveredItems.ContainsKey(itemId))
                    {
                        deliveredItems[itemId] = new CDataDeliveredItem()
                        {
                            ItemId = itemId,
                            ItemNum = 0,
                            NeedNum = 0
                        };
                    }
                    deliveredItems[itemId].ItemNum += (ushort)item.Num;
                }

                foreach (var deliveredItem in deliveredItems.Values)
                {
                    // Validate and compute without mutating in-memory state; if the DB upsert
                    // below throws the transaction rolls back cleanly with no state left behind.
                    var result = questState.ValidateDeliveryRequest((ItemId)deliveredItem.ItemId, deliveredItem.ItemNum);
                    deliveryCommits[deliveredItem.ItemId] = result;
                    foreach (var commonId in deliveryProgressCommonIds)
                    {
                        if (!Server.Database.UpsertQuestDeliveryProgress(commonId, request.QuestScheduleId, deliveredItem.ItemId, result.NewTotal, connection))
                        {
                            throw new ResponseErrorException(ErrorCode.ERROR_CODE_QUEST_INTERNAL_ERROR, $"Failed to save delivery progress for character common id {commonId}.");
                        }
                    }
                }
            });

            // Transaction committed, now safe to apply in-memory mutations.
            foreach (var deliveredItem in deliveredItems.Values)
            {
                var (remaining, newTotal) = deliveryCommits[deliveredItem.ItemId];
                deliveredItem.NeedNum = (ushort)remaining;
                questState.RestoreDeliveryAmount((ItemId)deliveredItem.ItemId, newTotal);
            }

            client.Send(new S2CItemUpdateCharacterItemNtc()
            {
                UpdateType = ItemNoticeType.QuestDelivery,
                UpdateItemList = itemUpdateResults
            });

            S2CQuestDeliverItemNtc ntc = new S2CQuestDeliverItemNtc()
            {
                DeliveredItemRecord = new CDataDeliveredItemRecord()
                {
                    CharacterId = client.Character.CharacterId,
                    QuestScheduleId = request.QuestScheduleId,
                    ProcessNo = request.ProcessNo,
                    DeliveredItemList = deliveredItems.Values.ToList()
                }
            };

            if (quest.IsPersonal)
            {
                client.Send(ntc);
            }
            else
            {
                client.Party.SendToAll(ntc);
            }

            return res;
        }

        private List<uint> GetDeliveryProgressCommonIds(GameClient client, bool isPersonal, uint questScheduleId, DbConnection connection)
        {
            if (isPersonal || client.Party == null)
            {
                return new List<uint>() { client.Character.CommonId };
            }

            var commonIds = new HashSet<uint>();
            foreach (var memberClient in client.Party.Clients)
            {
                if (memberClient?.Character == null)
                {
                    continue;
                }

                if (Server.Database.GetQuestProgressByScheduleId(memberClient.Character.CommonId, questScheduleId, connection) != null)
                {
                    commonIds.Add(memberClient.Character.CommonId);
                }
            }

            if (commonIds.Count == 0)
            {
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_QUEST_INTERNAL_ERROR, $"No party members have quest progress for delivery quest {questScheduleId}.");
            }

            return commonIds.ToList();
        }
    }
}

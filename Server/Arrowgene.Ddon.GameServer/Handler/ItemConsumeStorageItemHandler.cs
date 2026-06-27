using Arrowgene.Ddon.GameServer.Characters;

using Arrowgene.Ddon.Server;

using Arrowgene.Ddon.Server.Network;

using Arrowgene.Ddon.Shared.Entity.PacketStructure;

using Arrowgene.Ddon.Shared.Entity.Structure;

using Arrowgene.Ddon.Shared.Model;

using Arrowgene.Logging;



namespace Arrowgene.Ddon.GameServer.Handler

{

    public class ItemConsumeStorageItemHandler : GameRequestPacketQueueHandler<C2SItemConsumeStorageItemReq, S2CItemConsumeStorageItemRes>

    {

        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ItemConsumeStorageItemHandler));



        public ItemConsumeStorageItemHandler(DdonGameServer server) : base(server)

        {

        }



        public override PacketQueue Handle(GameClient client, C2SItemConsumeStorageItemReq request)

        {

            PacketQueue queue = new();

            S2CItemConsumeStorageItemRes res = new S2CItemConsumeStorageItemRes();



            S2CItemUpdateCharacterItemNtc ntc = new S2CItemUpdateCharacterItemNtc()

            {

                UpdateType = ItemNoticeType.ConsumeBag

            };



            bool supplyCachesEnabled = Server.SupplyCacheManager.IsEnabled;

            S2CInstancePopDropItemNtc? dropNtc = null;



            Server.Database.ExecuteInTransaction(connection =>

            {

                foreach (CDataStorageItemUIDList consumeItem in request.ConsumeItemList)

                {

                    Item? droppedItem = null;

                    uint droppedNum = consumeItem.Num;



                    if (supplyCachesEnabled)

                    {

                        if (consumeItem.SlotNo == 0)

                        {

                            var found = client.Character.Storage.GetStorage(consumeItem.StorageType).FindItemByUId(consumeItem.ItemUId);

                            droppedItem = found?.Item2;

                        }

                        else

                        {

                            droppedItem = client.Character.Storage.GetStorage(consumeItem.StorageType).GetItem(consumeItem.SlotNo)?.Item1;

                        }

                    }



                    CDataItemUpdateResult itemUpdate;

                    if (consumeItem.SlotNo == 0)

                    {

                        itemUpdate = Server.ItemManager.ConsumeItemByUId(Server, client.Character, consumeItem.StorageType, consumeItem.ItemUId, consumeItem.Num, connection)

                            ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_NOT_FOUND,

                            $"Cannot find item in {consumeItem.StorageType}, UID {consumeItem.ItemUId}");

                    }

                    else

                    {

                        itemUpdate = Server.ItemManager.ConsumeItemInSlot(Server, client.Character, consumeItem.StorageType, consumeItem.SlotNo, consumeItem.Num, connection)

                            ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_NOT_FOUND,

                            $"Cannot find item in {consumeItem.StorageType}, slot {consumeItem.SlotNo}");

                    }



                    ntc.UpdateItemList.Add(itemUpdate);



                    if (supplyCachesEnabled && droppedItem != null)

                    {

                        bool eligible = ItemManager.ItemBagStorageTypes.Contains(consumeItem.StorageType)

                            || (consumeItem.StorageType == StorageType.KeyItems

                                && Server.GameSettings.GameServerSettings.SupplyCacheAllowQuestItems);



                        if (!eligible)

                        {

                            client.SupplyCacheDiagnostics.RecordDropSkipped(

                                client,

                                consumeItem.StorageType,

                                droppedItem.ItemId,

                                $"Storage type {consumeItem.StorageType} is not routed to supply caches");

                        }

                        else

                        {

                            dropNtc = Server.SupplyCacheManager.HandleDrop(

                                client, consumeItem.StorageType, droppedItem, droppedNum, connection);

                        }

                    }

                }

            });



            client.Enqueue(res, queue);

            client.Enqueue(ntc, queue);



            if (dropNtc != null)
            {
                StageLayoutId dropLayout = client.InstanceLayoutId.Id != 0
                    ? client.InstanceLayoutId
                    : client.Character.Stage;
                uint wireSetId = client.SupplyCacheDropTracker.TryGetLastDroppedWireSetId(dropLayout.Id);
                Server.SupplyCacheManager.EnqueueDropSetRegistration(client, dropLayout, wireSetId, queue);
                client.Enqueue(dropNtc, queue);
                client.SupplyCacheDiagnostics.MarkPopDropSent();
                client.SupplyCacheDiagnostics.LogDropEvent(Server, client, client.Character.CharacterId, "popdrop_sent");
            }



            return queue;

        }

    }

}



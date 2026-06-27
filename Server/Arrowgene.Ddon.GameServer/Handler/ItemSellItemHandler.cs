using Arrowgene.Ddon.GameServer.Characters;
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
    public class ItemSellItemHandler : GameRequestPacketQueueHandler<C2SItemSellItemReq, S2CItemSellItemRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ItemSellItemHandler));
        
        public ItemSellItemHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketQueue Handle(GameClient client, C2SItemSellItemReq request)
        {
            PacketQueue packetQueue = new PacketQueue();
            client.Enqueue(new S2CItemSellItemRes(), packetQueue);

            uint totalAmountToAdd = 0;

            S2CItemUpdateCharacterItemNtc ntc = new S2CItemUpdateCharacterItemNtc()
            {
                UpdateType = ItemNoticeType.ShopItemSell
            };

            Server.Database.ExecuteInTransaction(connection =>
            {
                foreach (CDataStorageItemUIDList consumeItem in request.ConsumeItemList)
                {
                    List<StorageType> targetStorage = [];
                    if (consumeItem.StorageType == StorageType.ReceiveInItemBagCraft)
                    {
                        targetStorage = ItemManager.ItemBagStorageTypes;
                    }
                    else if (consumeItem.StorageType == StorageType.ReceiveInStorageCraft)
                    {
                        targetStorage = ItemManager.BoxStorageTypes;
                    }
                    else
                    {
                        targetStorage = [consumeItem.StorageType];                    
                    }

                    var ntcData = Server.ItemManager.ConsumeItemByUIdFromMultipleStorages(Server, client.Character, targetStorage, consumeItem.ItemUId, consumeItem.Num, connection);
                    ntc.UpdateItemList.AddRange(ntcData);

                    var itemId = ntcData.First().ItemList.ItemId;

                    uint goldValue = Server.AssetRepository.ClientItemInfos[itemId].Price;
                    uint amountToAdd = goldValue * consumeItem.Num;

                    // Anti-exploit: when gold is free, shop-buyable items can be acquired for nothing,
                    // so selling them must not yield any value (which would otherwise convert to XP).
                    // Items that cannot be bought from gold shops (drops, gathers, crafted gear, etc.)
                    // still sell normally.
                    if (Server.GameSettings.GameServerSettings.MakeGoldFree && Server.ShopManager.IsSoldInGoldShop(itemId))
                    {
                        amountToAdd = 0;
                    }

                    var (specialQueue, isSpecial) = Server.ItemManager.HandleSpecialItem(client, ntc, (ItemId)itemId, consumeItem.Num, SpecialItemMode.OnSell, connection);
                    if (isSpecial)
                    {
                        packetQueue.AddRange(specialQueue);
                        amountToAdd = 0;
                    }

                    totalAmountToAdd += amountToAdd;
                }
                CDataUpdateWalletPoint walletUpdate = Server.WalletManager.AddToWallet(client.Character, WalletType.Gold, totalAmountToAdd, 0, connection);
                ntc.UpdateWalletList.Add(walletUpdate);
            });

            client.Enqueue(ntc, packetQueue);

            return packetQueue;
        }
    }
}

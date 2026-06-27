#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Craft;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class CraftStartQualityUpHandler : GameRequestPacketQueueHandler<C2SCraftStartQualityUpReq, S2CCraftStartQualityUpRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(CraftStartQualityUpHandler));


        public CraftStartQualityUpHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketQueue Handle(GameClient client, C2SCraftStartQualityUpReq request)
        {
            PacketQueue queue = new();

            var (storageType, (slotno, equipItem, _)) = client.Character.Storage.FindItemByUIdInStorage(ItemManager.EquipmentStorages, request.ItemUID)
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_NOT_FOUND, $"Item with UID {request.ItemUID} not found.");
            ClientItemInfo itemInfo = Server.AssetRepository.ClientItemInfos[equipItem.ItemId];

            // The client seems to think that attaching a slayer stone is free and grants no XP.
            uint pawnExp = 0;
            uint totalCost = string.IsNullOrEmpty(request.RefineUID) ? 0 : itemInfo.Rank * 300u;
            S2CItemUpdateCharacterItemNtc updateCharacterItemNtc = new();

            CDataCurrentEquipInfo currentEquipInfo = new()
            {
                ItemUId = request.ItemUID,
            };

            // Lead pawn is always owned by player.
            Pawn leadPawn = Server.CraftManager.FindPawn(client, request.CraftMainPawnID);

            List<CraftPawn> craftPawns =
            [
                new CraftPawn(leadPawn, CraftPosition.Leader),
                .. request.CraftSupportPawnIDList.Select(p => new CraftPawn(Server.CraftManager.FindPawn(client, p.PawnId), CraftPosition.Assistant)),
                .. request.CraftMasterLegendPawnIDList.Select(p => new CraftPawn(Server.AssetRepository.PawnCraftMasterLegendAsset.Single(m => m.PawnId == p.PawnId))),
            ];

            uint plusValue = 0;
            bool isGreatSuccessEquipmentQuality = false;

            var consumedMaterials = request.CraftMaterialList.Union([new() {
                ItemNum = 1,
                ItemUId = request.RefineUID,
            }]).Where(x => !string.IsNullOrEmpty(x.ItemUId));

            Server.Database.ExecuteInTransaction(connection =>
            {
                if (!string.IsNullOrEmpty(request.RefineUID))
                {
                    double calculatedOdds = CraftManager.CalculateEquipmentQualityIncreaseRate(craftPawns);

                    var (_, (_, refineMaterialItem, _)) = client.Character.Storage.FindItemByUIdInStorage(ItemManager.BothStorageTypes, request.RefineUID);
                    CraftCalculationResult craftCalculationResult = CraftManager.CalculateEquipmentQuality(refineMaterialItem, (uint)calculatedOdds, itemInfo.Rank);
                    plusValue = craftCalculationResult.CalculatedValue;
                    isGreatSuccessEquipmentQuality = craftCalculationResult.IsGreatSuccess;
                    pawnExp = craftCalculationResult.Exp;
                }

                foreach(var material in consumedMaterials)
                {
                    try
                    {
                        var updateResults = Server.ItemManager.ConsumeItemByUIdFromMultipleStorages(Server, client.Character, ItemManager.BothStorageTypes, material.ItemUId, material.ItemNum, connection);
                        updateCharacterItemNtc.UpdateItemList.AddRange(updateResults);
                    }
                    catch (NotEnoughItemsException)
                    {
                        throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_INVALID_ITEM_NUM, "Client Item Desync has Occurred.");
                    }
                }

                // Updating the item.
                equipItem.PlusValue = Math.Max(equipItem.PlusValue, (byte)plusValue);

                if (request.AddStatusID != 0)
                {
                    var param = equipItem.AddStatusParamList.Find(x => x.EnhanceType == EquipEnhanceType.AdditionalCraftMaterial);
                    
                    if (param is null)
                    {
                        param = new CDataAddStatusParam()
                        {
                            EnhanceId = Server.AssetRepository.CraftAddStatusAsset.AddStatuses.GetValueOrDefault(request.AddStatusID)?.BuffId ?? 0,
                            EnhanceType = EquipEnhanceType.AdditionalCraftMaterial
                        };
                        equipItem.AddStatusParamList.Add(param);
                    }
                    else
                    {
                        param.EnhanceId = Server.AssetRepository.CraftAddStatusAsset.AddStatuses.GetValueOrDefault(request.AddStatusID)?.BuffId ?? 0;
                    }

                    Server.Database.UpsertEquipmentLimitBreakRecord(client.Character.CharacterId, equipItem.UId, param, connection);
                }

                CharacterCommon characterCommon = null;
                if (storageType == StorageType.CharacterEquipment || storageType == StorageType.PawnEquipment)
                {
                    currentEquipInfo.EquipSlot.EquipSlotNo = EquipManager.DetermineEquipSlot(slotno);
                    currentEquipInfo.EquipSlot.EquipType = EquipManager.GetEquipTypeFromSlotNo(slotno);
                }

                if (storageType == StorageType.PawnEquipment)
                {
                    uint pawnId = Storages.DeterminePawnId(client.Character, storageType, slotno);
                    currentEquipInfo.EquipSlot.PawnId = pawnId;
                    characterCommon = client.Character.Pawns.Find(x => x.PawnId == pawnId)
                        ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PAWN_NOT_FOUNDED);
                }
                else if (storageType == StorageType.CharacterEquipment)
                {
                    currentEquipInfo.EquipSlot.CharacterId = client.Character.CharacterId;
                    characterCommon = client.Character;
                }

                updateCharacterItemNtc.UpdateType = ItemNoticeType.StartEquipGradeUp;
                Server.ItemManager.UpgradeStorageItem(Server, client, client.Character.CharacterId, storageType, equipItem, slotno, connection);
                updateCharacterItemNtc.UpdateItemList.Add(Server.ItemManager.CreateItemUpdateResult(characterCommon, equipItem, storageType, slotno, 1, 1));

                uint cost = Server.CraftManager.CalculateRecipeCost(totalCost, itemInfo, craftPawns);
                if (cost > 0)
                {
                    CDataUpdateWalletPoint updateWalletPoint = Server.WalletManager.RemoveFromWallet(client.Character, WalletType.Gold, cost, connection)
                        ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_CRAFT_INSUFFICIENT_GOLD, 
                        $"Insufficient gold. {cost} > {Server.WalletManager.GetWalletAmount(client.Character, WalletType.Gold)}"); 
                    updateCharacterItemNtc.UpdateWalletList.Add(updateWalletPoint);
                }

                if (request.CraftMasterLegendPawnIDList.Count > 0)
                {
                    uint totalGPcost = (uint)request.CraftMasterLegendPawnIDList.Sum(p => Server.AssetRepository.PawnCraftMasterLegendAsset.Single(m => m.PawnId == p.PawnId).RentalCost);
                    CDataUpdateWalletPoint updateGP = Server.WalletManager.RemoveFromWallet(client.Character, WalletType.GoldenGemstones, totalGPcost, connection)
                        ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_GP_LACK_GP);
                    updateCharacterItemNtc.UpdateWalletList.Add(updateGP);
                }

                var res = new S2CCraftStartQualityUpRes()
                {
                    Unk0 = new()
                    {
                        IsGreatSuccess = isGreatSuccessEquipmentQuality,
                    },
                    AddStatusDataList = equipItem.AddStatusParamList,
                    CurrentEquip = currentEquipInfo
                };

                if (CraftManager.CanPawnExpUp(leadPawn))
                {
                    double BonusExpMultiplier = Server.GpCourseManager.PawnCraftBonus();
                    client.Enqueue(CraftManager.HandlePawnExpUpNtc(client, leadPawn, pawnExp, BonusExpMultiplier), queue);
                    if (CraftManager.CanPawnRankUp(leadPawn))
                    {
                        client.Enqueue(CraftManager.HandlePawnRankUpNtc(client, leadPawn), queue);
                        queue.AddRange(Server.AchievementManager.HandlePawnCrafting(client, leadPawn, connection));
                    }
                }
                else
                {
                    client.Enqueue(CraftManager.HandlePawnExpUpNtc(client, leadPawn, 0, 0), queue);
                }

                foreach (CraftPawn p in craftPawns)
                {
                    if (p.Pawn is RentalPawn rentalPawn)
                    {
                        Server.RentalPawnManager.HandleCraftCountDecrement(rentalPawn, connection);
                    }
                }

                Server.Database.UpdatePawnBaseInfo(leadPawn, connection);

                client.Enqueue(updateCharacterItemNtc, queue);
                client.Enqueue(res, queue);
            });

            return queue;
        }
    }
}

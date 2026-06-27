using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
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
    public class PawnGetRegisteredPawnDataHandler : GameRequestPacketQueueHandler<C2SPawnGetRegisteredPawnDataReq, S2CPawnGetRegisteredPawnDataRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PawnGetRegisteredPawnDataHandler));

        public PawnGetRegisteredPawnDataHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketQueue Handle(GameClient client, C2SPawnGetRegisteredPawnDataReq request)
        {
            PacketQueue queue = new();

            // Official pawn: generate a preview at the player's current level, no DB lookup needed
            var officialScript = Server.ScriptManager.OfficialPawnModule.GetById(unchecked((uint)request.PawnId));
            if (officialScript != null)
            {
                if (!Server.ScriptManager.OfficialPawnModule.IsAvailableToClient(officialScript, client, Server))
                {
                    throw new ResponseErrorException(ErrorCode.ERROR_CODE_PAWN_NOT_FOUNDED);
                }

                HandleOfficialPawn(client, officialScript, queue);
                return queue;
            }

            // Normal player pawn: existing DB path
            var mixin = Server.ScriptManager.MixinModule.Get<IRentalCostMixin>("rental_cost");

            Server.Database.ExecuteInTransaction(connection =>
            {
                uint ownerCharacterId = Server.Database.GetPawnOwnerCharacterId((uint)request.PawnId, connection);
                if (ownerCharacterId == Character.ServerCharacterId)
                {
                    throw new ResponseErrorException(ErrorCode.ERROR_CODE_CHARACTER_PAWN_PARAM_NOT_FOUND);
                }

                var ownerCharacter = Server.CharacterManager.SelectCharacter(ownerCharacterId, true, connection);
                Pawn pawn = ownerCharacter.Pawns.Where(x => x.PawnId == request.PawnId).FirstOrDefault()
                    ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PAWN_NOT_FOUNDED);

                HashSet<uint> clanPawns = [.. Server.Database.SelectClanPawns(client.Character.ClanId, limit: 1000, connectionIn: connection)];

                var res = new S2CPawnGetRegisteredPawnDataRes
                {
                    PawnId = (uint)request.PawnId,
                    PawnInfo = pawn.CDataPawnInfo
                };
                res.PawnInfo.AdventureCount = Server.GameSettings.GameServerSettings.RentalPawnAdventureCount;
                res.PawnInfo.MaxAdventureCount = Server.GameSettings.GameServerSettings.RentalPawnAdventureCount;
                res.PawnInfo.CraftCount = Server.GameSettings.GameServerSettings.RentalPawnCraftCount;
                res.PawnInfo.MaxCraftCount = Server.GameSettings.GameServerSettings.RentalPawnCraftCount;

                client.Enqueue(res, queue);

                var profileNtc = new S2CPawnGetPawnProfileNtc()
                {
                    CharacterId = ownerCharacterId,
                    PawnId = pawn.PawnId,
                    OwnerBaseInfo = Server.Database.SelectCommunityCharacterBaseInfo(ownerCharacterId, connection),
                    PawnProfile = pawn.CharacterProfile.CDataArisenProfile,
                    Comment = pawn.CharacterProfile.Comment,
                    RentalCost = mixin.GetRentalCost(client, pawn.CDataRegisterdPawnList, clanPawns.Contains(pawn.PawnId))
                };
                client.Enqueue(profileNtc, queue);

                var historyNtc = new S2CPawnGetPawnHistoryInfoNtc()
                {
                    CharacterId = ownerCharacterId,
                    PawnId = pawn.PawnId,
                    PawnHistoryList = Server.Database.SelectPawnHistory(pawn.PawnId, connection)
                };
                client.Enqueue(historyNtc, queue);

                var scoreNtc = new S2CPawnGetPawnTotalScoreInfoNtc()
                {
                    CharacterId = ownerCharacterId,
                    PawnId = pawn.PawnId,
                    PawnTotalScore = Server.Database.SelectPawnTotalScore(pawn.PawnId, connection)
                };
                client.Enqueue(scoreNtc, queue);
            });

            return queue;
        }

        private void HandleOfficialPawn(GameClient client, IOfficialPawnScript script, PacketQueue queue)
        {
            var mixin = Server.ScriptManager.MixinModule.Get<IRentalCostMixin>("rental_cost");
            var record = Server.ScriptManager.OfficialPawnModule.Generate(script, client.Character, Server);

            byte advCount   = script.AdventureCount ?? Server.GameSettings.GameServerSettings.RentalPawnAdventureCount;
            byte craftCount = script.CraftCount      ?? Server.GameSettings.GameServerSettings.RentalPawnCraftCount;

            var rentalPawn = record.ToRentalPawn(client.Character.CharacterId, advCount, craftCount);

            var res = new S2CPawnGetRegisteredPawnDataRes
            {
                PawnId = script.PawnId,
                PawnInfo = rentalPawn.CDataPawnInfo
            };
            res.PawnInfo.AdventureCount    = advCount;
            res.PawnInfo.MaxAdventureCount = advCount;
            res.PawnInfo.CraftCount        = craftCount;
            res.PawnInfo.MaxCraftCount     = craftCount;

            client.Enqueue(res, queue);

            var listEntry = new CDataRegisterdPawnList
            {
                PawnId = script.PawnId,
                Name = script.Name,
                Sex = script.EditInfo.Sex,
                PawnListData = new CDataPawnListData
                {
                    Job = script.Job,
                    Level = (uint)record.CharacterJobData.Lv,
                    CraftRank = record.CraftData.CraftRank,
                    PawnCraftSkillList = record.CraftData.PawnCraftSkillList,
                }
            };

            var profileNtc = new S2CPawnGetPawnProfileNtc
            {
                CharacterId = Character.ServerCharacterId,
                PawnId = script.PawnId,
                OwnerBaseInfo = new CDataCommunityCharacterBaseInfo
                {
                    CharacterId = Character.ServerCharacterId,
                    CharacterName = new CDataCharacterName { FirstName = Character.ServerCharacterFirstName }
                },
                PawnProfile = new CDataArisenProfile(),
                Comment = string.Empty,
                RentalCost = (uint)(mixin.GetRentalCost(client, listEntry, false) * script.RentalCostMultiplier)
            };
            client.Enqueue(profileNtc, queue);

            client.Enqueue(new S2CPawnGetPawnHistoryInfoNtc { CharacterId = Character.ServerCharacterId, PawnId = script.PawnId }, queue);
            client.Enqueue(new S2CPawnGetPawnTotalScoreInfoNtc { CharacterId = Character.ServerCharacterId, PawnId = script.PawnId, PawnTotalScore = new CDataPawnTotalScore() }, queue);
        }
    }
}

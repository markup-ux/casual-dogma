using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PawnGetOfficialPawnListHandler : GameRequestPacketHandler<C2SPawnGetOfficialPawnListReq, S2CPawnGetOfficialPawnListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PawnGetOfficialPawnListHandler));

        public PawnGetOfficialPawnListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CPawnGetOfficialPawnListRes Handle(GameClient client, C2SPawnGetOfficialPawnListReq request)
        {
            var results = new S2CPawnGetOfficialPawnListRes();
            int playerLevel = (int)(client.Character.ActiveCharacterJobData?.Lv ?? 1);

            var mixin = Server.ScriptManager.MixinModule.Get<IRentalCostMixin>("rental_cost");
            var officialPawnModule = Server.ScriptManager.OfficialPawnModule;
            var rentedOfficialPawnIds = client.Character.RentedPawns
                .Where(x => x.IsOfficialPawn)
                .Select(x => x.PawnId)
                .ToHashSet();
            var rentedOfficialPawnNames = client.Character.RentedPawns
                .Where(x => x.IsOfficialPawn)
                .Select(x => x.Name)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var script in officialPawnModule.GetForLevel(playerLevel))
            {
                if (!officialPawnModule.IsAvailableToClient(script, client, Server))
                    continue;

                if (rentedOfficialPawnIds.Contains(script.PawnId) || rentedOfficialPawnNames.Contains(script.Name))
                    continue;

                var record = officialPawnModule.Generate(script, client.Character, Server);
                var entry = new CDataRegisterdPawnList
                {
                    Name = script.Name,
                    Sex = script.EditInfo.Sex,
                    Updated = DateTimeOffset.UtcNow,
                    PawnId = script.PawnId,
                    PawnListData = new CDataPawnListData
                    {
                        Job = script.Job,
                        // Show the pawn at the player's current level so it's clear it scales
                        Level = (uint)playerLevel,
                        CraftRank = record.CraftData.CraftRank,
                        PawnCraftSkillList = record.CraftData.PawnCraftSkillList,
                    }
                };

                entry.RentalCost = (uint)(mixin.GetRentalCost(client, entry, false) * script.RentalCostMultiplier);
                results.OfficialPawnList.Add(entry);
            }

            return results;
        }
    }
}

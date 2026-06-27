using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PawnGetFavoritePawnListHandler : GameRequestPacketHandler<C2SPawnGetFavoritePawnListReq, S2CPawnGetFavoritePawnListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PawnGetFavoritePawnListHandler));


        public PawnGetFavoritePawnListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CPawnGetFavoritePawnListRes Handle(GameClient client, C2SPawnGetFavoritePawnListReq request)
        {
            HashSet<uint> clanPawns = [];

            var result = new S2CPawnGetFavoritePawnListRes();
            HashSet<uint> rentedIds = [.. client.Character.RentedPawns.Select(x => x.PawnId)];

            Server.Database.ExecuteInTransaction(connection =>
            {
                if (client.Character.ClanId != 0)
                {
                    clanPawns = [.. Server.Database.SelectClanPawns(client.Character.ClanId, limit: 1000, connectionIn: connection)];
                }

                var pawnFavorites = Server.Database.GetPawnFavorites(client.Character.CharacterId, connection).Except(rentedIds);
                foreach (var pawnId in pawnFavorites)
                {
                    var pawn = Server.Database.SelectPawn(connection, pawnId);
                    result.FavoritePawnList.Add(new CDataRegisterdPawnList()
                    {
                        Name = pawn.Name,
                        Sex = pawn.EditInfo.Sex,
                        Updated = DateTimeOffset.UtcNow,
                        PawnId = pawn.PawnId,
                        PawnListData = new CDataPawnListData()
                        {
                            Job = pawn.Job,
                            Level = pawn.ActiveCharacterJobData.Lv,
                            PawnCraftSkillList = pawn.CraftData.PawnCraftSkillList,
                        }
                    });
                }
            });

            var mixin = Server.ScriptManager.MixinModule.Get<IRentalCostMixin>("rental_cost");

            foreach (var registeredPawn in result.FavoritePawnList)
            {
                registeredPawn.RentalCost = mixin.GetRentalCost(client, registeredPawn, clanPawns.Contains(registeredPawn.PawnId));
            }

            return result;
        }
    }
}

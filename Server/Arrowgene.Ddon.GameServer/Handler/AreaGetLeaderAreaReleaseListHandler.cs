using Arrowgene.Ddon.GameServer.Dump;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class AreaGetLeaderAreaReleaseListHandler : GameRequestPacketHandler<C2SAreaGetLeaderAreaReleaseListReq, S2CAreaGetLeaderAreaReleaseListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(AreaGetLeaderAreaReleaseListHandler));

        public AreaGetLeaderAreaReleaseListHandler(DdonGameServer server) : base(server)
        {
        }

        // A list of spot IDs that occurs in all zones in the pcap.
        // Corresponds to the "time-limited" spots from S3.
        public static readonly HashSet<uint> TimeLimitedList = [1036, 1037, 1038, 1070, 1076, 1080, 1108, 1109, 1110, 1111, 1212, 1213];

        public override S2CAreaGetLeaderAreaReleaseListRes Handle(GameClient client, C2SAreaGetLeaderAreaReleaseListReq request)
        {
            var pcap = EntitySerializer.Get<S2CAreaGetLeaderAreaReleaseListRes>().Read(GameFull.data_Dump_117);

            var leader = client.Party.Leader;

            if (client.Party.Leader is null)
            {
                // No unlocks without a leader to pull AR from.
                return new();
            }

            var result = new S2CAreaGetLeaderAreaReleaseListRes
            {
                PeriodicallyReleasedSpots = Server.AreaRankManager.CheckPeriodicallyReleasedSpots(client),
                MonsterGatheringSpots = Server.AreaRankManager.CheckMonsterGatheringSpots(client)
            };

            var leaderRank = leader.Client.Character.AreaRanks;
            var completedQuests = leader.Client.Character.CompletedQuests;
            foreach ((var area, var rank) in leaderRank)
            {
                var releaseList = Server.AssetRepository.AreaRankSpotInfoAsset[area]
                .Where(spot => spot.UnlockRank > 0 || spot.UnlockQuest > 0)
                .Where(spot => Server.AreaRankManager.CheckSpot(leader.Client, spot))
                .Select(spot => spot.SpotId)
                //.Union(TimeLimitedList)
                .ToHashSet()
                .Select(spot => new CDataCommonU32(spot))
                .ToList();

                result.ReleaseAreaInfoSetList.Add(new()
                {
                    AreaId = area,
                    ReleaseList = releaseList
                });
            }
         
            //These are given in the pcap despite not being areas with normal ranks.
            for (var area = QuestAreaId.MemoryOfMegadosys; area <= QuestAreaId.BitterblackMaze; area++)
            {
                result.ReleaseAreaInfoSetList.Add(new()
                {
                    AreaId = area
                });
            }

            return result;
        }
    }
}

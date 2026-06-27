using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CAreaGetLeaderAreaReleaseListRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_AREA_GET_LEADER_AREA_RELEASE_LIST_RES;

        public List<CDataReleaseAreaInfoSet> ReleaseAreaInfoSetList { get; set; } = [];
        public List<CDataAreaRankMonsterGatheringSpot> MonsterGatheringSpots { get; set; } = [];
        public List<CDataAreaRankPeriodicallyReleasedSpot> PeriodicallyReleasedSpots { get; set; } = [];

        public class Serializer : PacketEntitySerializer<S2CAreaGetLeaderAreaReleaseListRes>
        {
            public override void Write(IBuffer buffer, S2CAreaGetLeaderAreaReleaseListRes obj)
            {
                WriteServerResponse(buffer, obj);
                WriteEntityList(buffer, obj.ReleaseAreaInfoSetList);
                WriteEntityList(buffer, obj.MonsterGatheringSpots);
                WriteEntityList(buffer, obj.PeriodicallyReleasedSpots);
            }

            public override S2CAreaGetLeaderAreaReleaseListRes Read(IBuffer buffer)
            {
                S2CAreaGetLeaderAreaReleaseListRes obj = new S2CAreaGetLeaderAreaReleaseListRes();
                ReadServerResponse(buffer, obj);
                obj.ReleaseAreaInfoSetList = ReadEntityList<CDataReleaseAreaInfoSet>(buffer);
                obj.MonsterGatheringSpots = ReadEntityList<CDataAreaRankMonsterGatheringSpot>(buffer);
                obj.PeriodicallyReleasedSpots = ReadEntityList<CDataAreaRankPeriodicallyReleasedSpot>(buffer);
                return obj;
            }
        }
    }
}

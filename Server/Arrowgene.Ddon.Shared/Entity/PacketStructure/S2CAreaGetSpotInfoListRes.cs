using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CAreaGetSpotInfoListRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_AREA_GET_SPOT_INFO_LIST_RES;

        public List<CDataSpotInfo> SpotInfoList { get; set; } = [];
        public List<CDataAreaRankMonsterGatheringSpot> MonsterGatheringSpots { get; set; } = [];
        public List<CDataAreaRankPeriodicallyReleasedSpot> PeriodicallyReleasedSpots { get; set; } = [];

        /// <summary>
        /// Is a list of SpotIds.
        /// Flags certain SpotIds for "Spot Boss has appeared!". Possibly does other things.
        /// </summary>
        public List<CDataCommonU32> SpotAlertList { get; set; } = [];

        public class Serializer : PacketEntitySerializer<S2CAreaGetSpotInfoListRes>
        {
            public override void Write(IBuffer buffer, S2CAreaGetSpotInfoListRes obj)
            {
                WriteServerResponse(buffer, obj);
                WriteEntityList(buffer, obj.SpotInfoList);
                WriteEntityList(buffer, obj.MonsterGatheringSpots);
                WriteEntityList(buffer, obj.PeriodicallyReleasedSpots);
                WriteEntityList(buffer, obj.SpotAlertList);
            }

            public override S2CAreaGetSpotInfoListRes Read(IBuffer buffer)
            {
                S2CAreaGetSpotInfoListRes obj = new S2CAreaGetSpotInfoListRes();
                ReadServerResponse(buffer, obj);
                obj.SpotInfoList = ReadEntityList<CDataSpotInfo>(buffer);
                obj.MonsterGatheringSpots = ReadEntityList<CDataAreaRankMonsterGatheringSpot>(buffer);
                obj.PeriodicallyReleasedSpots = ReadEntityList<CDataAreaRankPeriodicallyReleasedSpot>(buffer);
                obj.SpotAlertList = ReadEntityList<CDataCommonU32>(buffer);
                return obj;
            }
        }
    }
}

using Arrowgene.Buffers;
using System;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataAreaRankMonsterGatheringSpot
    {
        public uint SpotId { get; set; }

        /// <summary>
        /// 1-4 are valid values. Changes the enemies displayed at the spot in the info UI.
        /// The "boss" spawn seems to be on SpotState = 3.
        /// </summary>
        public uint SpotState { get; set; }
        public DateTimeOffset Unk2 { get; set; }

        public class Serializer : EntitySerializer<CDataAreaRankMonsterGatheringSpot>
        {
            public override void Write(IBuffer buffer, CDataAreaRankMonsterGatheringSpot obj)
            {
                WriteUInt32(buffer, obj.SpotId);
                WriteUInt32(buffer, obj.SpotState);
                WriteInt64(buffer, obj.Unk2.ToUnixTimeSeconds());
            }

            public override CDataAreaRankMonsterGatheringSpot Read(IBuffer buffer)
            {
                CDataAreaRankMonsterGatheringSpot obj = new CDataAreaRankMonsterGatheringSpot();
                obj.SpotId = ReadUInt32(buffer);
                obj.SpotState = ReadUInt32(buffer);
                obj.Unk2 = DateTimeOffset.FromUnixTimeSeconds(ReadInt64(buffer));
                return obj;
            }
        }
    }
}

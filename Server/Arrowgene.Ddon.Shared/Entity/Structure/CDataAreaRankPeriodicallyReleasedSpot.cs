using Arrowgene.Buffers;
using System;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataAreaRankPeriodicallyReleasedSpot
    {
        public uint SpotId { get; set; }
        public bool IsOpen { get; set; }
        /// <summary>
        /// If IsOpen is true, this is the time that it closes.
        /// If IsOpen is false, this is the time that it opens.
        /// </summary>
        public DateTimeOffset ChangeTime { get; set; }

        public class Serializer : EntitySerializer<CDataAreaRankPeriodicallyReleasedSpot>
        {
            public override void Write(IBuffer buffer, CDataAreaRankPeriodicallyReleasedSpot obj)
            {
                WriteUInt32(buffer, obj.SpotId);
                WriteBool(buffer, obj.IsOpen);
                WriteInt64(buffer, obj.ChangeTime.ToUnixTimeSeconds());
            }

            public override CDataAreaRankPeriodicallyReleasedSpot Read(IBuffer buffer)
            {
                CDataAreaRankPeriodicallyReleasedSpot obj = new CDataAreaRankPeriodicallyReleasedSpot();
                obj.SpotId = ReadUInt32(buffer);
                obj.IsOpen = ReadBool(buffer);
                obj.ChangeTime = DateTimeOffset.FromUnixTimeSeconds(ReadInt64(buffer));
                return obj;
            }
        }
    }
}

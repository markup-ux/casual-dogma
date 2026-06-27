using Arrowgene.Buffers;
using System;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataPawnHistory
    {
        public CDataPawnHistory() {
            DebtorBaseInfo = new CDataCommunityCharacterBaseInfo();
            PawnFeedback = new CDataPawnFeedback();
        }
    
        public uint PawnId { get; set; }
        public CDataCommunityCharacterBaseInfo DebtorBaseInfo { get; set; } = new();
        public DateTimeOffset ReturnDate { get; set; }
        public TimeSpan AdventureTime { get; set; }
        public byte AdventureCount { get; set; }
        public byte CraftCount { get; set; }
        public uint KillEnemyNum { get; set; }
        public CDataPawnFeedback PawnFeedback { get; set; } = new();
    
        public class Serializer : EntitySerializer<CDataPawnHistory>
        {
            public override void Write(IBuffer buffer, CDataPawnHistory obj)
            {
                WriteUInt32(buffer, obj.PawnId);
                WriteEntity<CDataCommunityCharacterBaseInfo>(buffer, obj.DebtorBaseInfo);
                WriteInt64(buffer, obj.ReturnDate.ToUnixTimeSeconds());
                WriteInt64(buffer, (long)obj.AdventureTime.TotalSeconds);
                WriteByte(buffer, obj.AdventureCount);
                WriteByte(buffer, obj.CraftCount);
                WriteUInt32(buffer, obj.KillEnemyNum);
                WriteEntity<CDataPawnFeedback>(buffer, obj.PawnFeedback);
            }
        
            public override CDataPawnHistory Read(IBuffer buffer)
            {
                CDataPawnHistory obj = new CDataPawnHistory();
                obj.PawnId = ReadUInt32(buffer);
                obj.DebtorBaseInfo = ReadEntity<CDataCommunityCharacterBaseInfo>(buffer);
                obj.ReturnDate = DateTimeOffset.FromUnixTimeSeconds(ReadInt64(buffer));
                obj.AdventureTime = TimeSpan.FromSeconds(ReadInt64(buffer));
                obj.AdventureCount = ReadByte(buffer);
                obj.CraftCount = ReadByte(buffer);
                obj.KillEnemyNum = ReadUInt32(buffer);
                obj.PawnFeedback = ReadEntity<CDataPawnFeedback>(buffer);
                return obj;
            }
        }
    }
}

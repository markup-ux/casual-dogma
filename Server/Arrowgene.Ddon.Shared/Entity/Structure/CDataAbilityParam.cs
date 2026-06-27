using System.Collections.Generic;
using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataAbilityParam
    {
        public AbilityId AbilityNo { get; set; }
        public JobId Job { get; set; }
        public byte Category { get; set; }
        public byte SortCategory { get; set; }
        public byte Type { get; set; } = 1;
        public uint Cost { get; set; }
        public List<CDataAbilityLevelParam> Params { get; set; } = [];

        public class Serializer : EntitySerializer<CDataAbilityParam>
        {
            public override void Write(IBuffer buffer, CDataAbilityParam obj)
            {
                WriteUInt32(buffer, (uint)obj.AbilityNo);
                WriteByte(buffer, (byte) obj.Job);
                WriteByte(buffer, obj.Category);
                WriteByte(buffer, obj.SortCategory);
                WriteByte(buffer, obj.Type);
                WriteUInt32(buffer, obj.Cost);
                WriteEntityList<CDataAbilityLevelParam>(buffer, obj.Params);
            }

            public override CDataAbilityParam Read(IBuffer buffer)
            {
                CDataAbilityParam obj = new CDataAbilityParam();
                obj.AbilityNo = (AbilityId) ReadUInt32(buffer);
                obj.Job = (JobId) ReadByte(buffer);
                obj.Category = ReadByte(buffer);
                obj.SortCategory = ReadByte(buffer);
                obj.Type = ReadByte(buffer);
                obj.Cost = ReadUInt32(buffer);
                obj.Params = ReadEntityList<CDataAbilityLevelParam>(buffer);
                return obj;
            }
        }
    }
}

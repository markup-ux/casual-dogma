using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;
using System.Text.Json.Serialization;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataCharacterJobData
    {
        // Some of these fields are purposefully hidden from the JSON serializer used for rental pawns.
        public JobId Job { get; set; }

        [JsonIgnore]
        public uint Exp{ get; set; }
        [JsonIgnore]
        public uint JobPoint{ get; set; }

        public uint Lv { get; set; }
        public ushort Atk { get; set; }
        public ushort Def { get; set; }
        public ushort MAtk { get; set; }
        public ushort MDef { get; set; }
        public ushort Strength { get; set; }
        public ushort DownPower { get; set; }
        public ushort ShakePower { get; set; }
        public ushort StunPower { get; set; }
        public ushort Constitution { get; set; }
        public ushort Guts { get; set; }

        [JsonIgnore]
        public byte FireResist{ get; set; }
        [JsonIgnore]
        public byte IceResist{ get; set; }
        [JsonIgnore]
        public byte ThunderResist{ get; set; }
        [JsonIgnore]
        public byte HolyResist{ get; set; }
        [JsonIgnore]
        public byte DarkResist{ get; set; }
        [JsonIgnore]
        public byte SpreadResist{ get; set; }
        [JsonIgnore]
        public byte FreezeResist{ get; set; }
        [JsonIgnore]
        public byte ShockResist{ get; set; }
        [JsonIgnore]
        public byte AbsorbResist{ get; set; }
        [JsonIgnore]
        public byte DarkElmResist{ get; set; }
        [JsonIgnore]
        public byte PoisonResist{ get; set; }
        [JsonIgnore]
        public byte SlowResist{ get; set; }
        [JsonIgnore]
        public byte SleepResist{ get; set; }
        [JsonIgnore]
        public byte StunResist{ get; set; }
        [JsonIgnore]
        public byte WetResist{ get; set; }
        [JsonIgnore]
        public byte OilResist{ get; set; }
        [JsonIgnore]
        public byte SealResist{ get; set; }
        [JsonIgnore]
        public byte CurseResist{ get; set; }
        [JsonIgnore]
        public byte SoftResist{ get; set; }
        [JsonIgnore]
        public byte StoneResist{ get; set; }
        [JsonIgnore]
        public byte GoldResist{ get; set; }
        [JsonIgnore]
        public byte FireReduceResist{ get; set; }
        [JsonIgnore]
        public byte IceReduceResist{ get; set; }
        [JsonIgnore]
        public byte ThunderReduceResist{ get; set; }
        [JsonIgnore]
        public byte HolyReduceResist{ get; set; }
        [JsonIgnore]
        public byte DarkReduceResist{ get; set; }
        [JsonIgnore]
        public byte AtkDownResist{ get; set; }
        [JsonIgnore]
        public byte DefDownResist{ get; set; }
        [JsonIgnore]
        public byte MAtkDownResist{ get; set; }
        [JsonIgnore]
        public byte MDefDownResist{ get; set; }

        public class Serializer : EntitySerializer<CDataCharacterJobData>
        {
            public override void Write(IBuffer buffer, CDataCharacterJobData obj)
            {
                WriteByte(buffer, (byte)obj.Job);
                WriteUInt32(buffer, obj.Exp);
                WriteUInt32(buffer, obj.JobPoint);
                WriteUInt32(buffer, obj.Lv);
                WriteUInt16(buffer, obj.Atk);
                WriteUInt16(buffer, obj.Def);
                WriteUInt16(buffer, obj.MAtk);
                WriteUInt16(buffer, obj.MDef);
                WriteUInt16(buffer, obj.Strength);
                WriteUInt16(buffer, obj.DownPower);
                WriteUInt16(buffer, obj.ShakePower);
                WriteUInt16(buffer, obj.StunPower);
                WriteUInt16(buffer, obj.Constitution);
                WriteUInt16(buffer, obj.Guts);
                WriteByte(buffer, obj.FireResist);
                WriteByte(buffer, obj.IceResist);
                WriteByte(buffer, obj.ThunderResist);
                WriteByte(buffer, obj.HolyResist);
                WriteByte(buffer, obj.DarkResist);
                WriteByte(buffer, obj.SpreadResist);
                WriteByte(buffer, obj.FreezeResist);
                WriteByte(buffer, obj.ShockResist);
                WriteByte(buffer, obj.AbsorbResist);
                WriteByte(buffer, obj.DarkElmResist);
                WriteByte(buffer, obj.PoisonResist);
                WriteByte(buffer, obj.SlowResist);
                WriteByte(buffer, obj.SleepResist);
                WriteByte(buffer, obj.StunResist);
                WriteByte(buffer, obj.WetResist);
                WriteByte(buffer, obj.OilResist);
                WriteByte(buffer, obj.SealResist);
                WriteByte(buffer, obj.CurseResist);
                WriteByte(buffer, obj.SoftResist);
                WriteByte(buffer, obj.StoneResist);
                WriteByte(buffer, obj.GoldResist);
                WriteByte(buffer, obj.FireReduceResist);
                WriteByte(buffer, obj.IceReduceResist);
                WriteByte(buffer, obj.ThunderReduceResist);
                WriteByte(buffer, obj.HolyReduceResist);
                WriteByte(buffer, obj.DarkReduceResist);
                WriteByte(buffer, obj.AtkDownResist);
                WriteByte(buffer, obj.DefDownResist);
                WriteByte(buffer, obj.MAtkDownResist);
                WriteByte(buffer, obj.MDefDownResist);
            }

            public override CDataCharacterJobData Read(IBuffer buffer)
            {
                CDataCharacterJobData obj = new CDataCharacterJobData();
                obj.Job = (JobId)ReadByte(buffer);
                obj.Exp = ReadUInt32(buffer);
                obj.JobPoint = ReadUInt32(buffer);
                obj.Lv = ReadUInt32(buffer);
                obj.Atk = ReadUInt16(buffer);
                obj.Def = ReadUInt16(buffer);
                obj.MAtk = ReadUInt16(buffer);
                obj.MDef = ReadUInt16(buffer);
                obj.Strength = ReadUInt16(buffer);
                obj.DownPower = ReadUInt16(buffer);
                obj.ShakePower = ReadUInt16(buffer);
                obj.StunPower = ReadUInt16(buffer);
                obj.Constitution = ReadUInt16(buffer);
                obj.Guts = ReadUInt16(buffer);
                obj.FireResist = ReadByte(buffer);
                obj.IceResist = ReadByte(buffer);
                obj.ThunderResist = ReadByte(buffer);
                obj.HolyResist = ReadByte(buffer);
                obj.DarkResist = ReadByte(buffer);
                obj.SpreadResist = ReadByte(buffer);
                obj.FreezeResist = ReadByte(buffer);
                obj.ShockResist = ReadByte(buffer);
                obj.AbsorbResist = ReadByte(buffer);
                obj.DarkElmResist = ReadByte(buffer);
                obj.PoisonResist = ReadByte(buffer);
                obj.SlowResist = ReadByte(buffer);
                obj.SleepResist = ReadByte(buffer);
                obj.StunResist = ReadByte(buffer);
                obj.WetResist = ReadByte(buffer);
                obj.OilResist = ReadByte(buffer);
                obj.SealResist = ReadByte(buffer);
                obj.CurseResist = ReadByte(buffer);
                obj.SoftResist = ReadByte(buffer);
                obj.StoneResist = ReadByte(buffer);
                obj.GoldResist = ReadByte(buffer);
                obj.FireReduceResist = ReadByte(buffer);
                obj.IceReduceResist = ReadByte(buffer);
                obj.ThunderReduceResist = ReadByte(buffer);
                obj.HolyReduceResist = ReadByte(buffer);
                obj.DarkReduceResist = ReadByte(buffer);
                obj.AtkDownResist = ReadByte(buffer);
                obj.DefDownResist = ReadByte(buffer);
                obj.MAtkDownResist = ReadByte(buffer);
                obj.MDefDownResist = ReadByte(buffer);
                return obj;
            }
        }
    }
}

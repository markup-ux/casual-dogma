using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataEditInfo
    {
        public byte Sex { get; set; } = 1;
        public byte Voice { get; set; } = 1;
        public ushort VoicePitch { get; set; } = 30000;
        public PawnPersonality Personality { get; set; } = PawnPersonality.Normal;
        public byte SpeechFreq { get; set; } = 1;
        public byte BodyType { get; set; } = 0;
        public byte Hair { get; set; } = 25;
        public byte Beard { get; set; } = 18;
        public byte Makeup { get; set; } = 0;
        public byte Scar { get; set; } = 0;
        public byte EyePresetNo { get; set; } = 0;
        public byte NosePresetNo { get; set; } = 0;
        public byte MouthPresetNo { get; set; } = 0;
        public byte EyebrowTexNo { get; set; } = 0;
        public byte ColorSkin { get; set; } = 0;
        public byte ColorHair { get; set; } = 41;
        public byte ColorBeard { get; set; } = 0;
        public byte ColorEyebrow { get; set; } = 41;
        public byte ColorREye { get; set; } = 18;
        public byte ColorLEye { get; set; } = 18;
        public byte ColorMakeup { get; set; } = 0;
        public ushort Sokutobu { get; set; } = 30000;
        public ushort Hitai { get; set; } = 30000;
        public ushort MimiJyouge { get; set; } = 30000;
        public ushort Kannkaku { get; set; } = 30000;
        public ushort MabisasiJyouge { get; set; } = 30000;
        public ushort HanakuchiJyouge { get; set; } = 30000;
        public ushort AgoSakiHaba { get; set; } = 30000;
        public ushort AgoZengo { get; set; } = 30000;
        public ushort AgoSakiJyouge { get; set; } = 30000;
        public ushort HitomiOokisa { get; set; } = 30000;
        public ushort MeOokisa { get; set; } = 30000;
        public ushort MeKaiten { get; set; } = 30000;
        public ushort MayuKaiten { get; set; } = 30000;
        public ushort MimiOokisa { get; set; } = 30000;
        public ushort MimiMuki { get; set; } = 30000;
        public ushort ElfMimi { get; set; } = 30000;
        public ushort MikenTakasa { get; set; } = 30000;
        public ushort MikenHaba { get; set; } = 30000;
        public ushort HohoboneRyou { get; set; } = 30000;
        public ushort HohoboneJyouge { get; set; } = 30000;
        public ushort Hohoniku { get; set; } = 30000;
        public ushort ErahoneJyouge { get; set; } = 30000;
        public ushort ErahoneHaba { get; set; } = 30000;
        public ushort HanaJyouge { get; set; } = 30000;
        public ushort HanaHaba { get; set; } = 30000;
        public ushort HanaTakasa { get; set; } = 30000;
        public ushort HanaKakudo { get; set; } = 30000;
        public ushort KuchiHaba { get; set; } = 30000;
        public ushort KuchiAtsusa { get; set; } = 30000;
        public ushort EyebrowUVOffsetX { get; set; } = 30000;
        public ushort EyebrowUVOffsetY { get; set; } = 30000;
        public ushort Wrinkle { get; set; } = 30000;
        public ushort WrinkleAlbedoBlendRate { get; set; } = 30000;
        public ushort WrinkleDetailNormalPower { get; set; } = 30000;
        public ushort MuscleAlbedoBlendRate { get; set; } = 30000;
        public ushort MuscleDetailNormalPower { get; set; } = 30000;
        public ushort Height { get; set; } = 48000;
        public ushort HeadSize { get; set; } = 40000;
        public ushort NeckOffset { get; set; } = 30000;
        public ushort NeckScale { get; set; } = 38500;
        public ushort UpperBodyScaleX { get; set; } = 40500;
        public ushort BellySize { get; set; } = 40000;
        public ushort TeatScale { get; set; } = 40000;
        public ushort TekubiSize { get; set; } = 40000;
        public ushort KoshiOffset { get; set; } = 28000;
        public ushort KoshiSize { get; set; } = 29100;
        public ushort AnkleOffset { get; set; } = 30000;
        public ushort Fat { get; set; } = 29500;
        public ushort Muscle { get; set; } = 31500;
        public ushort MotionFilter { get; set; } = 29000;

        public class Serializer : EntitySerializer<CDataEditInfo>
        {
            public override void Write(IBuffer buffer, CDataEditInfo obj)
            {
                WriteByte(buffer, obj.Sex);
                WriteByte(buffer, obj.Voice);
                WriteUInt16(buffer, obj.VoicePitch);
                WriteByte(buffer, (byte) obj.Personality);
                WriteByte(buffer, obj.SpeechFreq);
                WriteByte(buffer, obj.BodyType);
                WriteByte(buffer, obj.Hair);
                WriteByte(buffer, obj.Beard);
                WriteByte(buffer, obj.Makeup); //10
                WriteByte(buffer, obj.Scar);
                WriteByte(buffer, obj.EyePresetNo);
                WriteByte(buffer, obj.NosePresetNo);
                WriteByte(buffer, obj.MouthPresetNo);
                WriteByte(buffer, obj.EyebrowTexNo);
                WriteByte(buffer, obj.ColorSkin);
                WriteByte(buffer, obj.ColorHair);
                WriteByte(buffer, obj.ColorBeard);
                WriteByte(buffer, obj.ColorEyebrow);
                WriteByte(buffer, obj.ColorREye); //20
                WriteByte(buffer, obj.ColorLEye);
                WriteByte(buffer, obj.ColorMakeup);
                WriteUInt16(buffer, obj.Sokutobu);
                WriteUInt16(buffer, obj.Hitai);
                WriteUInt16(buffer, obj.MimiJyouge);
                WriteUInt16(buffer, obj.Kannkaku); //30
                WriteUInt16(buffer, obj.MabisasiJyouge);
                WriteUInt16(buffer, obj.HanakuchiJyouge);
                WriteUInt16(buffer, obj.AgoSakiHaba);
                WriteUInt16(buffer, obj.AgoZengo);
                WriteUInt16(buffer, obj.AgoSakiJyouge); //40
                WriteUInt16(buffer, obj.HitomiOokisa);
                WriteUInt16(buffer, obj.MeOokisa);
                WriteUInt16(buffer, obj.MeKaiten);
                WriteUInt16(buffer, obj.MayuKaiten);
                WriteUInt16(buffer, obj.MimiOokisa); //50
                WriteUInt16(buffer, obj.MimiMuki);
                WriteUInt16(buffer, obj.ElfMimi);
                WriteUInt16(buffer, obj.MikenTakasa);
                WriteUInt16(buffer, obj.MikenHaba);
                WriteUInt16(buffer, obj.HohoboneRyou); //60
                WriteUInt16(buffer, obj.HohoboneJyouge);
                WriteUInt16(buffer, obj.Hohoniku);
                WriteUInt16(buffer, obj.ErahoneJyouge);
                WriteUInt16(buffer, obj.ErahoneHaba);
                WriteUInt16(buffer, obj.HanaJyouge); //70
                WriteUInt16(buffer, obj.HanaHaba);
                WriteUInt16(buffer, obj.HanaTakasa);
                WriteUInt16(buffer, obj.HanaKakudo);
                WriteUInt16(buffer, obj.KuchiHaba);
                WriteUInt16(buffer, obj.KuchiAtsusa);
                WriteUInt16(buffer, obj.EyebrowUVOffsetX);
                WriteUInt16(buffer, obj.EyebrowUVOffsetY);
                WriteUInt16(buffer, obj.Wrinkle);
                WriteUInt16(buffer, obj.WrinkleAlbedoBlendRate);
                WriteUInt16(buffer, obj.WrinkleDetailNormalPower);
                WriteUInt16(buffer, obj.MuscleAlbedoBlendRate);
                WriteUInt16(buffer, obj.MuscleDetailNormalPower);
                WriteUInt16(buffer, obj.Height);
                WriteUInt16(buffer, obj.HeadSize);
                WriteUInt16(buffer, obj.NeckOffset);
                WriteUInt16(buffer, obj.NeckScale);
                WriteUInt16(buffer, obj.UpperBodyScaleX);
                WriteUInt16(buffer, obj.BellySize);
                WriteUInt16(buffer, obj.TeatScale);
                WriteUInt16(buffer, obj.TekubiSize);
                WriteUInt16(buffer, obj.KoshiOffset);
                WriteUInt16(buffer, obj.KoshiSize);
                WriteUInt16(buffer, obj.AnkleOffset);
                WriteUInt16(buffer, obj.Fat);
                WriteUInt16(buffer, obj.Muscle);
                WriteUInt16(buffer, obj.MotionFilter);
            }

            public override CDataEditInfo Read(IBuffer buffer)
            {
                CDataEditInfo obj = new CDataEditInfo();
                obj.Sex = ReadByte(buffer);
                obj.Voice = ReadByte(buffer);
                obj.VoicePitch = ReadUInt16(buffer);
                obj.Personality = (PawnPersonality) ReadByte(buffer);
                obj.SpeechFreq = ReadByte(buffer);
                obj.BodyType = ReadByte(buffer);
                obj.Hair = ReadByte(buffer);
                obj.Beard = ReadByte(buffer);
                obj.Makeup = ReadByte(buffer);
                obj.Scar = ReadByte(buffer);
                obj.EyePresetNo = ReadByte(buffer);
                obj.NosePresetNo = ReadByte(buffer);
                obj.MouthPresetNo = ReadByte(buffer);
                obj.EyebrowTexNo = ReadByte(buffer);
                obj.ColorSkin = ReadByte(buffer);
                obj.ColorHair = ReadByte(buffer);
                obj.ColorBeard = ReadByte(buffer);
                obj.ColorEyebrow = ReadByte(buffer);
                obj.ColorREye = ReadByte(buffer);
                obj.ColorLEye = ReadByte(buffer);
                obj.ColorMakeup = ReadByte(buffer);
                obj.Sokutobu = ReadUInt16(buffer);
                obj.Hitai = ReadUInt16(buffer);
                obj.MimiJyouge = ReadUInt16(buffer);
                obj.Kannkaku = ReadUInt16(buffer);
                obj.MabisasiJyouge = ReadUInt16(buffer);
                obj.HanakuchiJyouge = ReadUInt16(buffer);
                obj.AgoSakiHaba = ReadUInt16(buffer);
                obj.AgoZengo = ReadUInt16(buffer);
                obj.AgoSakiJyouge = ReadUInt16(buffer);
                obj.HitomiOokisa = ReadUInt16(buffer);
                obj.MeOokisa = ReadUInt16(buffer);
                obj.MeKaiten = ReadUInt16(buffer);
                obj.MayuKaiten = ReadUInt16(buffer);
                obj.MimiOokisa = ReadUInt16(buffer);
                obj.MimiMuki = ReadUInt16(buffer);
                obj.ElfMimi = ReadUInt16(buffer);
                obj.MikenTakasa = ReadUInt16(buffer);
                obj.MikenHaba = ReadUInt16(buffer);
                obj.HohoboneRyou = ReadUInt16(buffer);
                obj.HohoboneJyouge = ReadUInt16(buffer);
                obj.Hohoniku = ReadUInt16(buffer);
                obj.ErahoneJyouge = ReadUInt16(buffer);
                obj.ErahoneHaba = ReadUInt16(buffer);
                obj.HanaJyouge = ReadUInt16(buffer);
                obj.HanaHaba = ReadUInt16(buffer);
                obj.HanaTakasa = ReadUInt16(buffer);
                obj.HanaKakudo = ReadUInt16(buffer);
                obj.KuchiHaba = ReadUInt16(buffer);
                obj.KuchiAtsusa = ReadUInt16(buffer);
                obj.EyebrowUVOffsetX = ReadUInt16(buffer);
                obj.EyebrowUVOffsetY = ReadUInt16(buffer);
                obj.Wrinkle = ReadUInt16(buffer);
                obj.WrinkleAlbedoBlendRate = ReadUInt16(buffer);
                obj.WrinkleDetailNormalPower = ReadUInt16(buffer);
                obj.MuscleAlbedoBlendRate = ReadUInt16(buffer);
                obj.MuscleDetailNormalPower = ReadUInt16(buffer);
                obj.Height = ReadUInt16(buffer);
                obj.HeadSize = ReadUInt16(buffer);
                obj.NeckOffset = ReadUInt16(buffer);
                obj.NeckScale = ReadUInt16(buffer);
                obj.UpperBodyScaleX = ReadUInt16(buffer);
                obj.BellySize = ReadUInt16(buffer);
                obj.TeatScale = ReadUInt16(buffer);
                obj.TekubiSize = ReadUInt16(buffer);
                obj.KoshiOffset = ReadUInt16(buffer);
                obj.KoshiSize = ReadUInt16(buffer);
                obj.AnkleOffset = ReadUInt16(buffer);
                obj.Fat = ReadUInt16(buffer);
                obj.Muscle = ReadUInt16(buffer);
                obj.MotionFilter = ReadUInt16(buffer);
                return obj;
            }
        }
    }
}

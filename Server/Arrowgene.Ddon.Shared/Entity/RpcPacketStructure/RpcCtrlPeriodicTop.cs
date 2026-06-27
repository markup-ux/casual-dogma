using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;
using System;

namespace Arrowgene.Ddon.Shared.Entity.RpcPacketStructure
{
    public class RpcCtrlPeriodicTop : RpcPacketBase
    {
        public const int HeaderSize = 20;
        public const int GreenHpOffset = 64;
        public const int WhiteHpOffset = 66;
        public const int MinPacketSize = 72;

        public RpcCtrlPeriodicTop()
        {
        }

        public UInt64 Unk0 { get; set; }
        public bool IsEnemy { get; set; }
        public bool IsCharacter {  get; set; }
        public bool IsHuman { get; set; }
        public bool IsEnemyLarge {  get; set; }

        public double PosX { get; set; }
        public float PosY { get; set; }
        public double PosZ { get; set; }

        public UInt32 Unk1 { get; set; }
        public UInt32 Unk2 { get; set; }
        public UInt32 Unk3 { get; set; }

        public UInt16 GreenHP { get; set; }
        public UInt16 WhiteHP {  get; set; }
        public UInt16 Unk4 { get; set; }
        public UInt16 Stamina { get; set; }

        public override void Handle(Character character, RpcPacketHeader packetHeader, IBuffer buffer)
        {
            // Support only the player for now
            if (packetHeader.SearchId == 0) // SearchId == CharacterId?
            {
                RpcCtrlPeriodicTop obj = ReadPacketData(buffer);
                character.X = obj.PosX;
                character.Y = obj.PosY;
                character.Z = obj.PosZ;

                character.GreenHp = obj.GreenHP;
                character.WhiteHp = obj.WhiteHP;
            }
        }

        private RpcCtrlPeriodicTop ReadPacketData(IBuffer buffer)
        {
            RpcCtrlPeriodicTop obj = new RpcCtrlPeriodicTop();
            obj.Unk0 = ReadUInt64(buffer); // nNetMsgData::CtrlBase::stMsgCtrlBaseData.mUniqueId ?
            obj.IsEnemy = ReadBool(buffer);
            obj.IsCharacter = ReadBool(buffer);
            obj.IsHuman = ReadBool(buffer);
            obj.IsEnemyLarge = ReadBool(buffer);
            obj.PosX = ReadDouble(buffer);
            obj.PosY = ReadFloat(buffer);
            obj.PosZ = ReadDouble(buffer);
            obj.Unk1 = ReadUInt32(buffer);
            obj.Unk2 = ReadUInt32(buffer);
            obj.Unk3 = ReadUInt32(buffer);
            obj.GreenHP = ReadUInt16(buffer);
            obj.WhiteHP = ReadUInt16(buffer);
            obj.Unk4 = ReadUInt16(buffer);
            obj.Stamina = ReadUInt16(buffer);
            return obj;
        }

        /// <summary>
        /// True when <paramref name="data"/> is a player (SearchId==0) PERIODIC_TOP ctrl RPC payload.
        /// </summary>
        public static bool IsPlayerPeriodicTop(ReadOnlySpan<byte> data)
        {
            if (data.Length < MinPacketSize)
            {
                return false;
            }

            ushort msgDti = ReadUInt16Be(data, 12);
            ushort msgId = ReadUInt16Be(data, 14);
            uint searchId = ReadUInt32Be(data, 16);
            return msgDti == (ushort)RpcNetMsgDti.cNetMsgCtrlAction
                   && msgId == (ushort)RpcMsgIdControl.NET_MSG_ID_PERIODIC_TOP
                   && searchId == 0;
        }

        public static bool TryReadWhiteHp(ReadOnlySpan<byte> data, out ushort whiteHp)
        {
            whiteHp = 0;
            if (!IsPlayerPeriodicTop(data))
            {
                return false;
            }

            whiteHp = ReadUInt16Be(data, WhiteHpOffset);
            return true;
        }

        public static bool TryReadPeriodicTopHp(ReadOnlySpan<byte> data, out ushort greenHp, out ushort whiteHp)
        {
            greenHp = 0;
            whiteHp = 0;
            if (!IsPlayerPeriodicTop(data))
            {
                return false;
            }

            greenHp = ReadUInt16Be(data, GreenHpOffset);
            whiteHp = ReadUInt16Be(data, WhiteHpOffset);
            return true;
        }

        /// <summary>
        /// Rewrite recoverable HP in-place (big-endian u16 at <see cref="WhiteHpOffset"/>).
        /// </summary>
        public static bool TryWriteWhiteHp(Span<byte> data, ushort whiteHp)
        {
            if (!IsPlayerPeriodicTop(data))
            {
                return false;
            }

            WriteUInt16Be(data, WhiteHpOffset, whiteHp);
            return true;
        }

        private static ushort ReadUInt16Be(ReadOnlySpan<byte> data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        private static uint ReadUInt32Be(ReadOnlySpan<byte> data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        }

        private static void WriteUInt16Be(Span<byte> data, int offset, ushort value)
        {
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = (byte)(value & 0xFF);
        }
    }
}

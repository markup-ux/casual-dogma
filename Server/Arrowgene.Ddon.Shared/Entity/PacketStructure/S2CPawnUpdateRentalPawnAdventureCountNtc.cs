using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CPawnUpdateRentalPawnAdventureCountNtc : IPacketStructure
    {
        public PacketId Id => PacketId.S2C_PAWN_UPDATE_RENTAL_PAWN_ADVENTURE_COUNT_NTC;

        public uint PawnId { get; set; }
        public string PawnName { get; set; } = string.Empty;
        public byte AdventureCount { get; set; }

        public class Serializer : PacketEntitySerializer<S2CPawnUpdateRentalPawnAdventureCountNtc>
        {
            public override void Write(IBuffer buffer, S2CPawnUpdateRentalPawnAdventureCountNtc obj)
            {
                WriteUInt32(buffer, obj.PawnId);
                WriteMtString(buffer, obj.PawnName);
                WriteByte(buffer, obj.AdventureCount);
            }

            public override S2CPawnUpdateRentalPawnAdventureCountNtc Read(IBuffer buffer)
            {
                S2CPawnUpdateRentalPawnAdventureCountNtc obj = new S2CPawnUpdateRentalPawnAdventureCountNtc();
                obj.PawnId = ReadUInt32(buffer);
                obj.PawnName = ReadMtString(buffer);
                obj.AdventureCount = ReadByte(buffer);
                return obj;
            }
        }
    }
}

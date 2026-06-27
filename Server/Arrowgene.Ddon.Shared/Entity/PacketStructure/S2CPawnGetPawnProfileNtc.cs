using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CPawnGetPawnProfileNtc : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_PAWN_GET_PAWN_PROFILE_NTC;

        public uint CharacterId { get; set; } // Always the player character id?
        public uint PawnId { get; set; }
        public CDataCommunityCharacterBaseInfo OwnerBaseInfo { get; set; } = new();
        public CDataArisenProfile PawnProfile { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
        public uint RentalCost { get; set; }

        public class Serializer : PacketEntitySerializer<S2CPawnGetPawnProfileNtc>
        {
            public override void Write(IBuffer buffer, S2CPawnGetPawnProfileNtc obj)
            {
                WriteUInt32(buffer, obj.CharacterId);
                WriteUInt32(buffer, obj.PawnId);
                WriteEntity(buffer, obj.OwnerBaseInfo);
                WriteEntity<CDataArisenProfile>(buffer, obj.PawnProfile);
                WriteMtString(buffer, obj.Comment);
                WriteUInt32(buffer, obj.RentalCost);
            }

            public override S2CPawnGetPawnProfileNtc Read(IBuffer buffer)
            {
                S2CPawnGetPawnProfileNtc obj = new S2CPawnGetPawnProfileNtc();
                obj.CharacterId = ReadUInt32(buffer);
                obj.PawnId = ReadUInt32(buffer);
                obj.OwnerBaseInfo = ReadEntity<CDataCommunityCharacterBaseInfo>(buffer);
                obj.PawnProfile = ReadEntity<CDataArisenProfile>(buffer);
                obj.Comment = ReadMtString(buffer);
                obj.RentalCost = ReadUInt32(buffer);
                return obj;
            }
        }
    }
}

using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SProfileSetPawnProfileReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_PROFILE_SET_PAWN_PROFILE_REQ;

        public uint PawnId { get; set; }
        public CDataArisenProfile ArisenProfile { get; set; } = new();
        public string Comment { get; set; } = string.Empty;

        public class Serializer : PacketEntitySerializer<C2SProfileSetPawnProfileReq>
        {
            public override void Write(IBuffer buffer, C2SProfileSetPawnProfileReq obj)
            {
                WriteUInt32(buffer, obj.PawnId);
                WriteEntity(buffer, obj.ArisenProfile);
                WriteMtString(buffer, obj.Comment);
            }

            public override C2SProfileSetPawnProfileReq Read(IBuffer buffer)
            {
                C2SProfileSetPawnProfileReq obj = new C2SProfileSetPawnProfileReq();
                obj.PawnId = ReadUInt32(buffer);
                obj.ArisenProfile = ReadEntity<CDataArisenProfile>(buffer);
                obj.Comment = ReadMtString(buffer);
                return obj;
            }
        }
    }
}

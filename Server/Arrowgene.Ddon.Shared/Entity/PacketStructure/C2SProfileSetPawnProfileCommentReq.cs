using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SProfileSetPawnProfileCommentReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_PROFILE_SET_PAWN_PROFILE_COMMENT_REQ;

        public uint PawnId { get; set; }
        public string Comment { get; set; } = string.Empty;

        public class Serializer : PacketEntitySerializer<C2SProfileSetPawnProfileCommentReq>
        {
            public override void Write(IBuffer buffer, C2SProfileSetPawnProfileCommentReq obj)
            {
                WriteUInt32(buffer, obj.PawnId);
                WriteMtString(buffer, obj.Comment);
            }

            public override C2SProfileSetPawnProfileCommentReq Read(IBuffer buffer)
            {
                C2SProfileSetPawnProfileCommentReq obj = new C2SProfileSetPawnProfileCommentReq();
                obj.PawnId = ReadUInt32(buffer);
                obj.Comment = ReadMtString(buffer);
                return obj;
            }
        }
    }
}

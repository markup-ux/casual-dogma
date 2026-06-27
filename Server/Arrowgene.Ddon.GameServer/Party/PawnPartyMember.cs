using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Party;

public class PawnPartyMember : PartyMember
{
    public Pawn Pawn { get; set; }
    public object TimerLock { get; set; } = new();
    public uint AdventureTimer { get; set; }

    public override CDataPartyMember CDataPartyMember
    {
        get
        {
            var cdata = base.CDataPartyMember;
            cdata.CharacterListElement = Pawn.CDataCharacterListElement;
            return cdata;
        }
    }

    public S2CContextGetPartyMyPawnContextNtc GetPartyContext()
    {
        S2CContextGetPartyMyPawnContextNtc partyPlayerContextNtc = new()
        {
            PawnId = PawnId,
            Context = Pawn.CDataPartyContextPawn
        };
        partyPlayerContextNtc.Context.Base.MemberIndex = MemberIndex;
        return partyPlayerContextNtc;
    }

    public S2CContextGetPartyRentedPawnContextNtc GetS2CContextGetPartyRentedPawn_ContextNtc()
    {
        var contextNtc = new S2CContextGetPartyRentedPawnContextNtc
        {
            PawnId = PawnId,
            Context = Pawn.CDataPartyContextPawn
        };
        contextNtc.Context.Base.MemberIndex = MemberIndex;
        return contextNtc;
    }
}

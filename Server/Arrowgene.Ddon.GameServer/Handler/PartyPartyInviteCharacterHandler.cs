#nullable enable
using System.Linq;
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PartyPartyInviteCharacterHandler : GameRequestPacketHandler<C2SPartyPartyInviteCharacterReq, S2CPartyPartyInviteCharacterRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PartyPartyInviteCharacterHandler));

        public PartyPartyInviteCharacterHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CPartyPartyInviteCharacterRes Handle(GameClient client, C2SPartyPartyInviteCharacterReq request)
        {
            GameClient invitedClient = Server.ClientLookup.GetClientByCharacterId(request.CharacterId)
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PARTY_INVITE_FAIL_REASON_MEMBER_NOT_FOUND,
                $"not found CharacterId:{request.CharacterId} for party invitation");

            if (invitedClient == client)
            {
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_PARTY_INTERNAL_ERROR, $"can not invite (invitedClient == client)");
            }

            if (client.GameMode == GameMode.Normal && !invitedClient.Character.HasContentReleased(ContentsRelease.PartyPlayers))
            {
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_CONTENTS_RELEASE_NOT_PARTY_PLAY_WITH_PLAYER, "unable to invite to party (party play not unlocked)");
            }

            PartyGroup party = client.Party
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PARTY_NOT_FOUNDED, "can not invite (client.Party == null)");

            // Reject duplicate invites to the same party - spamming the button creates ghost slots.
            PartyInvitation? existingInvite = Server.PartyManager.GetPartyInvitation(invitedClient);
            if (existingInvite != null && existingInvite.Party?.Id == party.Id)
            {
                Logger.Info(client, $"[PartyId:{party.Id}] duplicate invite ignored; {invitedClient.Identity} already has a pending invite");
                return new S2CPartyPartyInviteCharacterRes
                {
                    Error = (uint)ErrorCode.ERROR_CODE_PARTY_ALREADY_INVITE
                };
            }

            PlayerPartyMember invitedMember = party.Invite(invitedClient, client);

            S2CPartyPartyInviteNtc ntc = new()
            {
                TimeoutSec = PartyManager.InvitationTimeoutSec,
                PartyListInfo = new()
                {
                    PartyId = party.Id,
                    ServerId = (uint)Server.Id,
                    MemberList = [.. party.Members.Select(x => x.CDataPartyMember)]
                }
            };

            invitedClient.Send(ntc);

            S2CPartyPartyInviteCharacterRes res = new S2CPartyPartyInviteCharacterRes
            {
                TimeoutSec = PartyManager.InvitationTimeoutSec,
                Info = new()
                {
                    PartyId = party.Id,
                    ServerId = (uint)Server.Id,
                    MemberList = [invitedMember.CDataPartyMember]
                }
            };

            Logger.Info(client, $"Invited Client:{invitedClient.Identity} to PartyId:{party.Id}");

            return res;
        }
    }
}

using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PartyPartyJoinHandler : GameRequestPacketHandler<C2SPartyPartyJoinReq, S2CPartyPartyJoinRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PartyPartyJoinHandler));

        public PartyPartyJoinHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CPartyPartyJoinRes Handle(GameClient client, C2SPartyPartyJoinReq request)
        {
            S2CPartyPartyJoinRes res = new S2CPartyPartyJoinRes();

            PartyGroup party = Server.PartyManager.GetParty(request.PartyId) 
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PARTY_NOT_FOUNDED, 
                "failed to find party (Server.PartyManager.GetParty() == null)");

            PlayerPartyMember join = party.Join(client);
            
            var partyLeader = party.Leader?.Client.Character
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PARTY_LEADER_ABSENCE);

            res.ContentNumber = party.ContentId;
            if (BoardManager.BoardIdIsExm(party.ContentId))
            {
                Server.CharacterManager.UpdateOnlineStatus(client, client.Character, OnlineStatus.Contents);
            }
            else
            {
                Server.CharacterManager.UpdateOnlineStatus(party.Leader.Client, partyLeader, OnlineStatus.PtLeader);
                if (partyLeader.CharacterId != client.Character.CharacterId)
                {
                    Server.CharacterManager.UpdateOnlineStatus(client, client.Character, OnlineStatus.PtMember);
                }
            }

            var progress = Server.Database.GetQuestProgressByType(client.Character.CommonId, QuestType.All);
            var deliveryProgressByQuest = Server.Database.GetAllQuestDeliveryProgress(client.Character.CommonId)
                .GroupBy(x => x.QuestScheduleId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var questProgress in progress)
            {
                var quest = QuestManager.GetQuestByScheduleId(questProgress.QuestScheduleId);
                if (quest != null && quest.IsPersonal)
                {
                    join.QuestState.AddNewQuest(questProgress.QuestScheduleId, questProgress.Step);

                    if (deliveryProgressByQuest.TryGetValue(questProgress.QuestScheduleId, out var deliveryItems))
                    {
                        foreach (var dp in deliveryItems)
                            join.QuestState.RestoreDeliveryProgress(questProgress.QuestScheduleId, (ItemId)dp.ItemId, dp.AmountDelivered);
                    }
                }
            }

            // Exclude JoinState.Prepare members and their pawns from all NTCs
            // they have a reserved slot but haven't accepted yet.
            var prepareOwnerIds = party.Members
                .OfType<PlayerPartyMember>()
                .Where(p => p.JoinState == JoinState.Prepare)
                .Select(p => p.Client?.Character?.CharacterId ?? 0)
                .ToHashSet();

            S2CPartyPartyJoinNtc ntc = new S2CPartyPartyJoinNtc();
            uint hostCharacterId = (party.Host?.Client != null && party.Host.Client.IsAlive)
                ? party.Host.Client.Character.CharacterId
                : partyLeader.CharacterId;
            ntc.HostCharacterId = hostCharacterId;
            ntc.LeaderCharacterId = partyLeader.CharacterId;
            foreach (PartyMember member in party.Members)
            {
                if (member is PlayerPartyMember pm && prepareOwnerIds.Contains(pm.Client?.Character?.CharacterId ?? 0))
                    continue;

                if (member is PawnPartyMember pawnMember && prepareOwnerIds.Contains(pawnMember.Pawn?.CharacterId ?? 0))
                    continue;

                ntc.PartyMembers.Add(member.CDataPartyMember);
            }

            // Only send JoinNtc to fully joined members - Prepare members haven't accepted yet.
            foreach (PartyMember member in party.Members)
            {
                if (member is PlayerPartyMember onMember && onMember.JoinState == JoinState.On)
                {
                    onMember.Client?.Send(ntc);
                }
            }

            S2CContextGetPartyPlayerContextNtc newMemberContext = join.GetPartyContext();
            if (partyLeader.CommonId != client.Character.CommonId)
            {
                // Update player position when joining from a different stage
                client.Character.StageNo = partyLeader.StageNo;
                client.Character.Stage = partyLeader.Stage;
                newMemberContext.Context.Base.StageNo = (int) partyLeader.StageNo;
            }

            if (party.Clients.Count > 0)
            {
                foreach (PartyMember member in party.Members)
                {
                    if (member.MemberIndex == join.MemberIndex)
                        continue;

                    if (member is PlayerPartyMember playerMember)
                    {
                        if (prepareOwnerIds.Contains(playerMember.Client?.Character?.CharacterId ?? 0))
                            continue;
                        client.Send(playerMember.GetPartyContext());
                    }
                    else if (member is PawnPartyMember pawnPartyMember)
                    {
                        if (prepareOwnerIds.Contains(pawnPartyMember.Pawn?.CharacterId ?? 0))
                            continue;
                        client.Send(pawnPartyMember.GetPartyContext());
                    }
                }

                foreach (PartyMember member in party.Members)
                {
                    if (member is PlayerPartyMember onMember && onMember.JoinState == JoinState.On)
                    {
                        onMember.Client?.Send(newMemberContext);
                    }
                }
            }

            res.PartyId = party.Id;

            Logger.Info(client, $"joined PartyId:{party.Id}");

            return res;
        }
    }
}

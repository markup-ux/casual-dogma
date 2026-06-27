using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PawnRentalPawnLostHandler : GameRequestPacketQueueHandler<C2SPawnRentalPawnLostReq, S2CPawnRentalPawnLostRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PawnRentalPawnLostHandler));
        
        public PawnRentalPawnLostHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketQueue Handle(GameClient client, C2SPawnRentalPawnLostReq request)
        {
            PacketQueue queue = new();

            RentalPawn pawn = (RentalPawn)client.Character.PawnById(request.PawnId, PawnType.Support);

            queue.AddRange(Server.RentalPawnManager.HandleAdventureCountDecrement(client, pawn));

            S2CPawnRentalPawnLostNtc ntc = new S2CPawnRentalPawnLostNtc()
            {
                PawnId = pawn.PawnId,
                PawnName = pawn.Name,
                AdventureCount = pawn.AdventureCount
            };
            client.Party.EnqueueToAll(ntc, queue);

            client.Enqueue(new S2CPawnRentalPawnLostRes()
            {
                PawnId = pawn.PawnId,
                PawnName = pawn.Name,
                AdventureCount = pawn.AdventureCount
            }, queue);

            var pawnMember = client.Party.GetPartyMemberByCharacter(pawn);
            if (pawnMember is not null)
            {
                // Handle serverside tracking. C2SPawnPawnLostReq is only sent to the owner, and only they can kick their own pawn, so it works out.
                client.Party.Kick(client, (byte)pawnMember.MemberIndex);

                // Free up the party slot so that the client allows new invites, if there are less than 4 people remaining.
                client.Party.EnqueueToAll(new S2CPartyPartyMemberLostNtc()
                {
                    MemberIndex = (byte)pawnMember.MemberIndex
                }, queue);
            }

            return queue;
        }
    }
}

using Arrowgene.Ddon.Database.Model;
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.GameServer.Characters
{
    public class RentalPawnManager(DdonGameServer server)
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(RentalPawnManager));

        private readonly DdonGameServer Server = server;

        public void HandleStageAreaChange(GameClient client, uint fromStage, uint toStage)
        {
            if (client.Party is null)
            {
                return;
            }

            bool fromSafe = StageManager.IsSafeArea(fromStage);
            bool toSafe = StageManager.IsSafeArea(toStage);
            if (fromSafe == toSafe)
            {
                // Base->Base or Adventure->Adventure transition, so no need to adjust the timers.
                return;
            }
            else if (fromSafe && !toSafe)
            {
                // Base->Adventure, so start the timers.
                foreach (var member in client.Party.Members)
                {
                    if (member is PawnPartyMember pawnMember 
                        && pawnMember.Pawn is RentalPawn rentalPawn
                        && rentalPawn.CharacterId == client.Character.CharacterId
                    )
                    {
                        lock(pawnMember.TimerLock)
                        {
                            if (pawnMember.AdventureTimer != 0)
                            {
                                Server.TimerManager.StartTimer(pawnMember.AdventureTimer);
                                Logger.Info($"Resuming adventure timer for {client.Character.CDataCharacterName}'s rental pawn {rentalPawn.CDataCharacterName}");
                            }
                        }                     
                    }
                }
            }
            else if (!fromSafe && toSafe)
            {
                // Adventure->Base, so pause the timers.
                foreach (var member in client.Party.Members)
                {
                    if (member is PawnPartyMember pawnMember
                        && pawnMember.Pawn is RentalPawn rentalPawn
                        && rentalPawn.CharacterId == client.Character.CharacterId
                    )
                    {
                        lock (pawnMember.TimerLock)
                        {
                            if (pawnMember.AdventureTimer != 0)
                            {
                                if (Server.GameSettings.GameServerSettings.RentalPawnAdventureTimerAutoReset)
                                {
                                    Server.TimerManager.CancelTimer(pawnMember.AdventureTimer);
                                    SetupTimer(client, pawnMember, false);
                                    Logger.Info($"Automatically resetting adventure timer for {client.Character.CDataCharacterName}'s rental pawn {rentalPawn.CDataCharacterName}");
                                }
                                else
                                {
                                    Server.TimerManager.PauseTimer(pawnMember.AdventureTimer);
                                    Logger.Info($"Pausing adventure timer for {client.Character.CDataCharacterName}'s rental pawn {rentalPawn.CDataCharacterName}");
                                }
                            }
                        }
                    }
                }
            }
        }

        public PacketQueue HandleReset(PartyGroup party)
        {
            PacketQueue queue = new();

            foreach (var member in party.Members)
            {
                if (member is PawnPartyMember pawnMember
                    && pawnMember.Pawn is RentalPawn rentalPawn
                )
                {
                    var client = Server.ClientLookup.GetClientByCharacterId(rentalPawn.CharacterId);
                    if (rentalPawn.AdventureCount == 0)
                    {
                        party.Kick(client, (byte)member.MemberIndex);
                        party.EnqueueToAll(new S2CPartyPartyMemberKickNtc() { MemberIndex = (byte)member.MemberIndex }, queue);
                    }
                    else if (pawnMember.AdventureTimer == 0)
                    {
                        // Reapply timer to any rental pawns that have finished theirs.
                        SetupTimer(client, pawnMember, false);
                    }
                }
            }

            return queue;
        }

        public void SetupTimer(GameClient client, PawnPartyMember pawnMember, bool silent = true)
        {
            uint adventureTimeLength = Server.GameSettings.GameServerSettings.RentalPawnAdventureTimer;
            lock (pawnMember.TimerLock)
            {
                pawnMember.AdventureTimer = Server.TimerManager.CreateTimer(adventureTimeLength, () =>
                {
                    Logger.Info($"Triggering adventure timer for {client.Character.CDataCharacterName}'s rental pawn {pawnMember.Pawn.CDataCharacterName}");
                    HandleAdventureCountDecrement(client, (RentalPawn)pawnMember.Pawn).Send();
                    pawnMember.AdventureTimer = 0;
                });
            }

            if (!silent)
            {
                client.Send(new S2CLobbyChatMsgNotice()
                {
                    Type = LobbyChatMsgType.ManagementGuideC,
                    Message = $"{pawnMember.Pawn.CDataCharacterName} is ready for a new adventure."
                });
            }
        }

        public PacketQueue HandleAdventureCountDecrement(GameClient client, RentalPawn pawn, DbConnection connectionIn = null)
        {
            PacketQueue queue = new();

            if (pawn.AdventureCount > 0)
            {
                pawn.AdventureCount--;
                Server.Database.UpdateRentalPawn(client.Character.CharacterId, pawn, connectionIn);
                client.Enqueue(new S2CPawnUpdateRentalPawnAdventureCountNtc()
                {
                    PawnId = pawn.PawnId,
                    PawnName = pawn.Name,
                    AdventureCount = pawn.AdventureCount,
                }, queue);
            }
           
            return queue;
        }

        public PacketQueue HandleMainPawnLeaveParty(GameClient client)
        {
            PacketQueue queue = new();
            var rentalMembers = client.Party.Members.Where(x =>
                x is PawnPartyMember pawnMember
                && pawnMember.Pawn is RentalPawn rentalPawn
                && rentalPawn.CharacterId == client.Character.CharacterId
            );
            bool hasMainPawn = client.Party.Members.Any(x =>
                x is PawnPartyMember pawnMember
                && !pawnMember.Pawn.IsRented
                && pawnMember.Pawn.CharacterId == client.Character.CharacterId
            );

            // No need to do anything if there's still a main pawn and/or there are no rental pawns.
            if (hasMainPawn || !rentalMembers.Any())
            {
                return queue;
            }

            foreach (var member in rentalMembers)
            {
                client.Party.Kick(client, (byte)member.MemberIndex);
                client.Party.EnqueueToAll(new S2CPartyPartyMemberKickNtc() { MemberIndex = (byte)member.MemberIndex }, queue);
            }

            return queue;
        }

        public void HandleCraftCountDecrement(RentalPawn rentalPawn, DbConnection connectionIn = null)
        {
            rentalPawn.CraftCount--;
            Server.Database.UpdateRentalPawn(rentalPawn.CharacterId, rentalPawn, connectionIn);

            return;
        }

        public void HandleEnemyKill(RentalPawn rentalPawn, DbConnection? connectionIn = null)
        {
            lock (rentalPawn)
            {
                rentalPawn.KillCount++;
                Server.Database.UpdateRentalPawn(rentalPawn.CharacterId, rentalPawn, connectionIn);
            }
        }

        public (bool IsRunning, uint MinutesLeft) GetAdventureTimeRemaining(PawnPartyMember pawn)
        {
            if (pawn.AdventureTimer == 0)
            {
                return (false, 0);
            }
            else
            {
                return (true, (uint)(Server.TimerManager.GetTimeLeftInSeconds(pawn.AdventureTimer) / 60));
            }
        }
    }
}

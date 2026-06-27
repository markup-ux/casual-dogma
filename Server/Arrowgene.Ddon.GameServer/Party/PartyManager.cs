using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Csv;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Party;

public class PartyManager
{
    public const uint MaxNumParties = 1000;
    public const uint InvalidPartyId = 0;
    public const ushort InvitationTimeoutSec = 30;

    private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PartyManager));

    public readonly DdonGameServer Server;

    private readonly ConcurrentStack<uint> _idPool;
    private readonly ConcurrentDictionary<uint, PartyGroup> _parties;
    private readonly ConcurrentDictionary<GameClient, PartyInvitation> _invites;

    public PartyManager(DdonGameServer server)
    {
        Server = server;
        _idPool = new ConcurrentStack<uint>();
        for (uint i = 1; i < MaxNumParties + 1; i++)
        {
            _idPool.Push(i);
        }

        _parties = new ConcurrentDictionary<uint, PartyGroup>();
        _invites = new ConcurrentDictionary<GameClient, PartyInvitation>();
    }

    public bool InviteParty(GameClient invitee, GameClient host, PartyGroup party, bool createTimeout)
    {
        // Clean up any existing invite before adding the new one (handles cross-invites and stale DC slots).
        // TryRemove is atomic, so concurrent timer expiry is safe.
        if (_invites.TryRemove(invitee, out PartyInvitation stale))
        {
            stale.CancelTimer();
            stale.Party?.Leave(invitee);
            Logger.Info($"[PartyId:{party.Id}][Invite] cleared stale invite for invitee {invitee.Identity} before re-inviting");
        }

        PartyInvitation invitation = new PartyInvitation
        {
            Invitee = invitee,
            Host = host,
            Party = party,
            Date = DateTime.UtcNow
        };

        if (!_invites.TryAdd(invitee, invitation))
        {
            throw new ResponseErrorException(ErrorCode.ERROR_CODE_PARTY_ALREADY_INVITE,
                $"[PartyId:{party.Id}][Invite] could not be invited; already has pending invite");
        }

        if (createTimeout)
        {
            invitation.StartTimer(RemoveExpiredInvite, InvitationTimeoutSec + 2);
        }

        return true;
    }

    private void NotifyPartyInviteeClearedFromSlot(PartyInvitation invitation)
    {
        if (invitation.Party == null || invitation.Invitee == null)
        {
            return;
        }

        // Capture MemberIndex before Leave() frees the slot.
        PlayerPartyMember inviteeMember = invitation.Party.GetPlayerPartyMember(invitation.Invitee);
        int memberIndex = inviteeMember?.MemberIndex ?? PartyGroup.InvalidSlotIndex;

        Logger.Info($"[NotifyPartyInviteeClearedFromSlot] PartyId:{invitation.Party.Id} " +
                    $"Invitee:{invitation.Invitee.Identity} MemberIndex:{memberIndex} " +
                    $"PartyMemberCount:{invitation.Party.MemberCount()} " +
                    $"AliveClients:{invitation.Party.Clients.Count}");

        // KickNtc must be sent before Leave() - after Leave() frees the slot, SendToAll
        // won't include the invitee and they won't see their own ghost slot removal.
        if (memberIndex != PartyGroup.InvalidSlotIndex)
        {
            Logger.Info($"[NotifyPartyInviteeClearedFromSlot] Sending KickNtc MemberIndex:{memberIndex} to all {invitation.Party.Clients.Count} alive clients");
            invitation.Party.SendToAll(new S2CPartyPartyMemberKickNtc
            {
                MemberIndex = (byte)memberIndex
            });
        }
        else
        {
            Logger.Info($"[NotifyPartyInviteeClearedFromSlot] MemberIndex is InvalidSlotIndex, skipping KickNtc");
        }

        invitation.Party.Leave(invitation.Invitee);
    }

    private void RemoveExpiredInvite(PartyInvitation invitation)
    {
        if (invitation == null)
        {
            return;
        }

        // TryRemove is atomic - if DC cleanup or InviteParty already removed this entry, we stop.
        if (_invites.TryRemove(invitation.Invitee, out _))
        {
            var ntc = new S2CPartyPartyInviteFailNtc
            {
                ErrorCode = ErrorCode.ERROR_CODE_PARTY_INVITE_FAIL_REASON_TIMEOUT,
                ServerId = (ushort)Server.Id,
                PartyId = invitation.Party?.Id ?? InvalidPartyId
            };

            invitation.Invitee?.Send(ntc);
            invitation.Host?.Send(ntc);

            NotifyPartyInviteeClearedFromSlot(invitation);

            Logger.Info($"Invitation removed due to timeout (Host: {invitation.Host?.Identity ?? "disconnected"}, Invitee: {invitation.Invitee?.Identity ?? "unknown"})");
        }
    }

    public PartyInvitation GetPartyInvitation(GameClient client)
    {
        if (!_invites.TryGetValue(client, out PartyInvitation partyInvitation))
        {
            Logger.Error(client, $"invite not found, for get");
            return null;
        }

        return partyInvitation;
    }

    public PartyInvitation TryRemovePartyInvitation(GameClient client)
    {
        _invites.TryRemove(client, out PartyInvitation partyInvitation);
        return partyInvitation;
    }

    public PartyInvitation RemovePartyInvitation(GameClient client)
    {
        if (!_invites.TryRemove(client, out PartyInvitation partyInvitation))
        {
            Logger.Error(client, $"invite not found for remove");
            return null;
        }

        return partyInvitation;
    }

    public bool CancelPartyInvitation(PartyGroup party)
    {
        PartyInvitation invitation = _invites.Values
            .Where(x => x.Party == party)
            .FirstOrDefault();

        if (invitation == null)
        {
            Logger.Info($"[CancelPartyInvitation] no active invitation found for PartyId:{party.Id}, already resolved");
            return false;
        }

        RemovePartyInvitation(invitation.Invitee);

        var ntc = new S2CPartyPartyInviteFailNtc
        {
            ErrorCode = ErrorCode.ERROR_CODE_PARTY_INVITE_HOST_CANCEL,
            ServerId = (ushort)Server.Id,
            PartyId = invitation.Party.Id
        };

        invitation.CancelTimer();

        invitation.Invitee?.Send(ntc);
        invitation.Host?.Send(ntc);

        NotifyPartyInviteeClearedFromSlot(invitation);

        Logger.Info(invitation.Invitee, "Invitation removed due to cancellation.");
        return true;
    }

    public IEnumerable<PartyGroup> GetAllParties()
    {
        return _parties.Values;
    }

    public PartyGroup GetParty(uint partyId)
    {
        if (!_parties.TryGetValue(partyId, out PartyGroup party))
        {
            Logger.Error(
                $"Could not find party by partyId {partyId} (!_parties.TryGetValue(partyId, out PartyGroup party)");
            return null;
        }

        return party;
    }

    public bool DisbandParty(uint partyId)
    {
        if (!_parties.TryRemove(partyId, out PartyGroup party))
        {
            Logger.Error($"Failed to remove partyId:{partyId} (!_parties.TryRemove(partyId, out PartyGroup party)");
            return false;
        }

        _idPool.Push(party.Id);
        LogPartyIdCount();
        return true;
    }

    public PartyGroup NewParty(ulong boardId = 0)
    {
        if (!_idPool.TryPop(out uint partyId))
        {
            Logger.Info("Id pool seemingly exhausted. Cleaning up (!_idPool.TryPop(out uint partyId)");
            RecalculateIdPool();
            // Try again a second time and error if the problem persists
            if (!_idPool.TryPop(out partyId))
            {
                Logger.Error("Could not create party, id pool exhausted (!_idPool.TryPop(out partyId)");
                return null;
            }
        }

        PartyGroup party = new PartyGroup(partyId, this, boardId);
        if (!_parties.TryAdd(partyId, party))
        {
            Logger.Error("Could not create party, failed to add new party (!_parties.TryAdd(partyId, party))");
            return null;
        }

        LogPartyIdCount();

        return party;
    }

    private void LogPartyIdCount()
    {
        Logger.Info($"Free party IDs: {_idPool.Count}/{MaxNumParties}");
    }

    private void RecalculateIdPool()
    {
        // TODO: Thread safety, logs, error handling
        foreach (KeyValuePair<uint, PartyGroup> pair in _parties)
        {
            if (pair.Value.MemberCount() == 0)
            {
                _idPool.Push(pair.Key);
                _parties.TryRemove(pair);
            }
        }

        Logger.Info($"Free party IDs: {_idPool.Count}/{MaxNumParties}");
    }

    public bool ClientsInSameParty(GameClient clientA, GameClient clientB)
    {
        if (clientA.Party == null || clientB.Party == null)
        {
            return false;
        }

        return (clientA.Party.Id == clientB.Party.Id);
    }

    public void CleanupOnExit(GameClient client)
    {
        if (client.Party != null)
        {
            client.Party.Leave(client);

            Logger.Info(client, $"Left PartyId:{client.Party.Id}");

            S2CPartyPartyLeaveNtc partyLeaveNtc = new S2CPartyPartyLeaveNtc();
            partyLeaveNtc.CharacterId = client.Character.CharacterId;
            client.Party.SendToAllExcept(partyLeaveNtc, client);

            client.Send(new S2CPartyPartyLeaveRes());
        }
    }
}

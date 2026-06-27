using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer
{
    public class RevivalManager
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(RevivalManager));

        private readonly DdonGameServer Server;
        private readonly Dictionary<uint, List<RevivalRechargePending>> PendingByCharacter = new();

        public RevivalManager(DdonGameServer server)
        {
            Server = server;
        }

        public void LoadCharacter(GameClient client)
        {
            uint characterId = client.Character.CharacterId;
            PendingByCharacter[characterId] = Server.Database.SelectRevivalRechargePending(characterId);
            ProcessPendingRecharges(client);
        }

        public void UnloadCharacter(uint characterId)
        {
            PendingByCharacter.Remove(characterId);
        }

        public void ProcessAllOnline()
        {
            foreach (GameClient client in Server.ClientLookup.GetAll().ToList())
            {
                ProcessPendingRecharges(client);
            }
        }

        public uint GetRemainingRechargeSeconds(uint characterId)
        {
            if (Server.GpCourseManager.InfiniteReviveRefresh())
            {
                return 0;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long? nextExpiry = GetPending(characterId)
                .Where(x => x.Type == RevivalRechargeType.RevivalPower && x.ExpiresAtUnix > now)
                .Select(x => (long?)x.ExpiresAtUnix)
                .FirstOrDefault();

            if (!nextExpiry.HasValue)
            {
                return 0;
            }

            return (uint)Math.Max(0, nextExpiry.Value - now);
        }

        public void OnRevivalPowerConsumed(GameClient client)
        {
            if (Server.GpCourseManager.InfiniteReviveRefresh())
            {
                return;
            }

            uint characterId = client.Character.CharacterId;
            byte current = client.Character.StatusInfo.RevivePoint;
            uint pending = CountPending(characterId, RevivalRechargeType.RevivalPower);

            if (current + pending >= GetRevivalPowerMax())
            {
                return;
            }

            ScheduleRecharge(client, RevivalRechargeType.RevivalPower);
        }

        public void OnGoldenGemstoneReviveConsumed(GameClient client)
        {
            ScheduleRecharge(client, RevivalRechargeType.GoldenGemstone);
        }

        public bool TryGrantNpcRevivalPower(GameClient client)
        {
            if (client.Character.StatusInfo.RevivePoint >= GetRevivalPowerMax())
            {
                return false;
            }

            client.Character.StatusInfo.RevivePoint++;
            Server.Database.UpdateStatusInfo(client.Character);
            SendRevivePointNtc(client);
            return true;
        }

        public void ProcessPendingRecharges(GameClient client)
        {
            if (Server.GpCourseManager.InfiniteReviveRefresh())
            {
                return;
            }

            uint characterId = client.Character.CharacterId;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            List<RevivalRechargePending> pending = GetPending(characterId);
            bool changed = false;

            foreach (RevivalRechargePending entry in pending.Where(x => x.ExpiresAtUnix <= now).OrderBy(x => x.ExpiresAtUnix).ToList())
            {
                if (!ApplyRecharge(client, entry.Type))
                {
                    continue;
                }

                Server.Database.DeleteRevivalRechargePending(entry.Id);
                pending.Remove(entry);
                changed = true;
            }

            if (changed)
            {
                PendingByCharacter[characterId] = pending;
            }
        }

        private bool ApplyRecharge(GameClient client, RevivalRechargeType type)
        {
            switch (type)
            {
                case RevivalRechargeType.RevivalPower:
                    if (client.Character.StatusInfo.RevivePoint >= GetRevivalPowerMax())
                    {
                        return true;
                    }

                    client.Character.StatusInfo.RevivePoint++;
                    Server.Database.UpdateStatusInfo(client.Character);
                    SendRevivePointNtc(client);
                    return true;

                case RevivalRechargeType.GoldenGemstone:
                    uint amount = Server.GameSettings.GameServerSettings.RevivalRechargeGoldenGemstoneAmount;
                    if (amount == 0)
                    {
                        return true;
                    }

                    client.Send(Server.WalletManager.AddToWalletNtc(
                        client,
                        client.Character,
                        WalletType.GoldenGemstones,
                        amount));
                    return true;

                default:
                    return true;
            }
        }

        private void ScheduleRecharge(GameClient client, RevivalRechargeType type)
        {
            uint characterId = client.Character.CharacterId;
            long expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                + (long)(Server.GameSettings.GameServerSettings.RevivalRechargeIntervalMinutes * 60);

            if (!Server.Database.InsertRevivalRechargePending(characterId, type, expiresAt))
            {
                Logger.Error($"Failed to schedule {type} recharge for CharacterId={characterId}");
                return;
            }

            PendingByCharacter[characterId] = Server.Database.SelectRevivalRechargePending(characterId);
        }

        private void SendRevivePointNtc(GameClient client)
        {
            S2CCharacterUpdateRevivePointNtc ntc = new()
            {
                CharacterId = client.Character.CharacterId,
                RevivePoint = client.Character.StatusInfo.RevivePoint
            };
            client.Party.SendToAllExcept(ntc, client);
        }

        private List<RevivalRechargePending> GetPending(uint characterId)
        {
            if (!PendingByCharacter.TryGetValue(characterId, out List<RevivalRechargePending> pending))
            {
                pending = Server.Database.SelectRevivalRechargePending(characterId);
                PendingByCharacter[characterId] = pending;
            }

            return pending;
        }

        private uint CountPending(uint characterId, RevivalRechargeType type)
        {
            return (uint)GetPending(characterId).Count(x => x.Type == type);
        }

        private byte GetRevivalPowerMax()
        {
            return Server.GameSettings.GameServerSettings.RevivalPowerMax;
        }
    }
}

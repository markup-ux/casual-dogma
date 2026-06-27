using System;
using Arrowgene.Ddon.Database.Model;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Networking.SAEAServer;

namespace Arrowgene.Ddon.LoginServer
{
    public class LoginClient : Client
    {
        public LoginClient(ClientHandle clientHandle, PacketFactory packetFactory) : base(clientHandle, packetFactory)
        {
            UpdateIdentity();
        }

        public void UpdateIdentity()
        {
            // ClientHandle.Identity throws ObjectDisposedException once the handle has been
            // recycled (e.g. the client disconnected). Fall back to the last known identity
            // so logging never crashes on a stale handle.
            string handleIdentity;
            try
            {
                handleIdentity = ClientHandle.Identity;
            }
            catch (ObjectDisposedException)
            {
                handleIdentity = Identity ?? "disconnected";
            }

            string newIdentity = $"[LoginClient#{Id}@{handleIdentity}]";
            if (Account != null)
            {
                newIdentity += $"[Acc:{Account.NormalName}]";
            }

            Identity = newIdentity;
        }

        public Account Account { get; set; }

        public uint SelectedCharacterId { get; set; }
    }
}

using System.IO;
using System.Net;
using System.Runtime.Serialization;
using Arrowgene.Ddon.Shared;
using Arrowgene.WebServer;

namespace Arrowgene.Ddon.WebServer
{
    [DataContract]
    public class WebServerSetting
    {
        /// <summary>
        /// Default shared secret used to authenticate the official launcher's level-sync patch.
        /// Must match the constant compiled into the launcher (Launcher/PatchAuth.cs). Change it
        /// only if you also rebuild the launcher with the same value.
        /// </summary>
        public const string DefaultPatchSharedSecret = "CasualDogma-LevelSync-Patch-2f9c8a1b7e4d4a6f";

        public WebServerSetting()
        {
            PublicWebEndPoint = new WebEndPoint();
            PublicWebEndPoint.Port = 52099;
            PublicWebEndPoint.IpAddress = IPAddress.Loopback;

            WebSetting = new WebSetting();
            WebSetting.ServerHeader = "";
            WebSetting.WebFolder = Path.Combine(Util.ExecutingDirectory(), "Files/www");

            WebSetting.WebEndpoints.Clear();
            WebEndPoint httpEndpoint = new WebEndPoint();
            httpEndpoint.Port = 52099;
            httpEndpoint.IpAddress = IPAddress.Any;
            WebSetting.WebEndpoints.Add(httpEndpoint);

            MailSetting = new MailSetting();

            RequirePatchToken = false;
            PatchSharedSecret = DefaultPatchSharedSecret;
            MinPatchVersion = 0;
        }

        public WebServerSetting(WebServerSetting webServerSetting)
        {
            PublicWebEndPoint = new WebEndPoint(webServerSetting.PublicWebEndPoint);
            WebSetting = new WebSetting(webServerSetting.WebSetting);
            MailSetting = new MailSetting(webServerSetting.MailSetting);

            RequirePatchToken = webServerSetting.RequirePatchToken;
            PatchSharedSecret = webServerSetting.PatchSharedSecret;
            MinPatchVersion = webServerSetting.MinPatchVersion;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            PublicWebEndPoint ??= new WebEndPoint();
            WebSetting ??= new WebSetting();
            MailSetting ??= new MailSetting();

            // Constructors are skipped during deserialization, so backfill the secret
            // default for configs written before the patch gate existed.
            if (string.IsNullOrEmpty(PatchSharedSecret))
            {
                PatchSharedSecret = DefaultPatchSharedSecret;
            }
        }

        [DataMember(Order = 1)]
        public WebEndPoint PublicWebEndPoint { get; set; }

        [DataMember(Order = 2)]
        public WebSetting WebSetting { get; set; }

        [DataMember(Order = 3)]
        public MailSetting MailSetting { get; set; }

        /// <summary>
        /// When true, the account login endpoint rejects any client that does not present a valid
        /// level-sync patch token. This blocks third-party launchers (pointed at this server's IP)
        /// from logging in, so only the official patched launcher can connect.
        /// </summary>
        [DataMember(Order = 4)]
        public bool RequirePatchToken { get; set; }

        /// <summary>
        /// Shared secret used to validate the launcher's patch token. Defaults to
        /// <see cref="DefaultPatchSharedSecret"/>; must match the launcher build.
        /// </summary>
        [DataMember(Order = 5)]
        public string PatchSharedSecret { get; set; }

        /// <summary>
        /// Minimum accepted patch version. Raise this to force players onto a newer launcher build.
        /// </summary>
        [DataMember(Order = 6)]
        public int MinPatchVersion { get; set; }

    }
}

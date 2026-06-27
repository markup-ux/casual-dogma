namespace Arrowgene.Ddon.Shared.Model
{
    public class RevivalRechargePending
    {
        public long Id { get; set; }
        public uint CharacterId { get; set; }
        public RevivalRechargeType Type { get; set; }
        public long ExpiresAtUnix { get; set; }
    }
}

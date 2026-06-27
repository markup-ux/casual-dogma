using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Scripting.Interfaces
{
    public abstract class IRentalPointMixin
    {
        public abstract uint GetRentalPointReward(GameClient client, RentalPawn returningPawn);
    }
}

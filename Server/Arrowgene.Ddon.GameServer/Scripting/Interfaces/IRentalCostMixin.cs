using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.GameServer.Scripting.Interfaces
{
    public abstract class IRentalCostMixin
    {
        public abstract uint GetRentalCost(GameClient client, CDataRegisterdPawnList pawnListEntry, bool isClan);
    }
}

using Arrowgene.Ddon.Shared.Model;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Scripting.Interfaces
{
    public interface IInstanceEnemyDropGenerator
    {
        public GameMode GameMode { get; }
        public List<InstancedGatheringItem> Generate(GameClient client, InstancedEnemy enemyKilled);
    }
}

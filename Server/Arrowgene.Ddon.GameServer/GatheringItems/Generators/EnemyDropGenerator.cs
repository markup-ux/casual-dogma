using Arrowgene.Ddon.Shared.Model;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.GatheringItems.Generators
{
    public class EnemyDropGenerator : IDropGenerator
    {
        private readonly DdonGameServer Server;

        public EnemyDropGenerator(DdonGameServer server)
        {
            Server = server;
        }

        public List<InstancedGatheringItem> Generate(GameClient client, InstancedEnemy enemyKilled)
        {
            var results = new List<InstancedGatheringItem>();
            foreach (var generator in Server.ScriptManager.InstanceEnemyDropModule.GetGenerators(client.GameMode))
            {
                results.AddRange(generator.Generate(client, enemyKilled));
            }
            return results;
        }
    }
}

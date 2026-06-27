using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Scripting.Interfaces
{
    public abstract class IInstanceEnemyPropertyGenerator
    {
        /// <summary>
        /// Can be used to control the order of the scripts which are executed.
        /// Scripts with a lower rank are executed before scripts with a higher
        /// rank.
        /// </summary>
        public virtual uint ScriptRank { get; } = 1;
        public abstract void ApplyChanges(GameClient client, StageLayoutId stageLayotuId, byte subGroupId, InstancedEnemy enemy);
    }
}

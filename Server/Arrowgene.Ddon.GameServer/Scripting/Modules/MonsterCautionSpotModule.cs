using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class MonsterCautionSpotModule : GameServerScriptModule
    {
        public override string ModuleRoot => Path.Combine("area_rank", "monster_caution_spots");
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        public Dictionary<QuestAreaId, List<IMonsterSpotInfo>> EnemyGroups { get; private set; }

        public MonsterCautionSpotModule()
        {
            EnemyGroups = new Dictionary<QuestAreaId, List<IMonsterSpotInfo>>();
        }

        public bool IsEnabledCautionSpotGroup(DdonGameServer server, PartyGroup party, StageLayoutId stageLayoutId, QuestAreaId areaId)
        {
            if (party.Leader == null || !EnemyGroups.ContainsKey(areaId))
            {
                return false;
            }

            var cautionSpot = EnemyGroups[areaId].Where(x => x.StageLayoutId.Equals(stageLayoutId)).FirstOrDefault();
            if (cautionSpot == null || !cautionSpot.CautionPlayer)
            {
                return false;
            }

            var leaderClient = party.Leader.Client;
            if (cautionSpot.QuestUnlockId != QuestId.None && !leaderClient.Character.HasQuestCompleted(cautionSpot.QuestUnlockId))
            {
                return false;
            }

            return server.AreaRankManager.GetEffectiveRank(leaderClient.Character, areaId) >= cautionSpot.RequiredAreaRank;
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            IMonsterSpotInfo spotInfo = (IMonsterSpotInfo)result;
            if (spotInfo == null)
            {
                return false;
            }

            spotInfo.Initialize();

            if (!EnemyGroups.ContainsKey(spotInfo.AreaId))
            {
                EnemyGroups[spotInfo.AreaId] = new List<IMonsterSpotInfo>();
            }

            // Remove if there are overlaps
            EnemyGroups[spotInfo.AreaId].RemoveAll(x => spotInfo.GetLocation().Equals(x.GetLocation()));
            // Add the new entry to the list
            EnemyGroups[spotInfo.AreaId].Add(spotInfo);

            var quest = QuestManager.GetQuestByQuestId(QuestId.WorldManageMonsterCaution);
            if (quest != null)
            {
                LibDdon.RegenerateQuest(quest);
            }

            return true;
        }
    }
}


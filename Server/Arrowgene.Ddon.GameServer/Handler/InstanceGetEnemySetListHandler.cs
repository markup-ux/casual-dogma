using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Context;
using Arrowgene.Ddon.GameServer.Enemies;
using Arrowgene.Ddon.GameServer.Enemies.Generators;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Crypto;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class InstanceGetEnemySetListHandler : GameRequestPacketQueueHandler<C2SInstanceGetEnemySetListReq, S2CInstanceGetEnemySetListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(InstanceGetEnemySetListHandler));

        // Order in list indicates priority where first item has highest priority and last item has least.
        private readonly List<IEnemySetGenerator> EnemySetGenerators = new List<IEnemySetGenerator>()
        {
            new SpawnTestingGenerator(),
            new QuestEnemySetGenerator(),
            new EpitaphRoadEnemySetGenerator(),
            new CautionSpotEnemyGenerator(),
            new WorldEnemySetGenerator(),
        };

        public InstanceGetEnemySetListHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketQueue Handle(GameClient client, C2SInstanceGetEnemySetListReq request)
        {
            PacketQueue queue = new();
            StageLayoutId stageLayoutId = request.LayoutId.AsStageLayoutId();
            byte subGroupId = request.SubGroupId;
            client.Character.Stage = stageLayoutId;

            S2CInstanceGetEnemySetListRes response = new S2CInstanceGetEnemySetListRes()
            {
                LayoutId = stageLayoutId.ToCDataStageLayoutId(),
                SubGroupId = subGroupId,
                RandomSeed = CryptoRandom.Instance.GetRandomUInt32(),
                QuestId = QuestId.None,
            };

            var instancedEnemyList = new List<InstancedEnemy>();
            foreach (var generator in EnemySetGenerators)
            {
                if (generator.GetEnemySet(Server, client, stageLayoutId, subGroupId, /* out */ instancedEnemyList, out QuestId questId))
                {
                    response.QuestId = questId;
                    break;
                }
            }

            for (var i = 0; i < instancedEnemyList.Count; i++)
            {
                var enemy = client.Party.InstanceEnemyManager.GetInstanceEnemy(stageLayoutId, instancedEnemyList[i].Index);

                if (enemy != null && (enemy.QuestScheduleId != instancedEnemyList[i].QuestScheduleId))
                {
                    var uid = ContextManager.CreateEnemyUID(enemy.Index, stageLayoutId.ToCDataStageLayoutId());
                    ContextManager.RemoveContext(client.Party, uid);
                }

                if (enemy == null || (enemy.QuestScheduleId != instancedEnemyList[i].QuestScheduleId))
                {
                    enemy = instancedEnemyList[i].CreateNewInstance();
                    if (Server.GameSettings.GameServerSettings.EnableAutomaticExpCalculationForAll)
                    {
                        enemy.ExpScheme = EnemyExpScheme.Automatic;
                    }

                    foreach (var generator in Server.ScriptManager.InstanceEnemyPropertyGeneratorModule.GetGenerators())
                    {
                        generator.ApplyChanges(client, stageLayoutId, subGroupId, enemy);
                    }

                    ExplorationRepopHelper.ConfigureSpawn(Server, client, stageLayoutId, enemy);
                    client.Party.InstanceEnemyManager.SetInstanceEnemy(stageLayoutId, enemy.Index, enemy);
                }

                if (Server.LevelSyncManager.TryApplyEnemyLevelSync(client, stageLayoutId, enemy))
                {
                    client.Party.InstanceEnemyManager.SetInstanceEnemy(stageLayoutId, enemy.Index, enemy);
                }

                enemy.StageLayoutId = stageLayoutId;

                response.EnemyList.Add(new CDataLayoutEnemyData()
                {
                    PositionIndex = enemy.Index,
                    EnemyInfo = enemy.AsCDataStageLayoutEnemyPresetEnemyInfoClient()
                });
            }

            if (subGroupId > 0 && response.EnemyList.Count > 0)
            {
                S2CInstanceEnemySubGroupAppearNtc subgroupNtc = new S2CInstanceEnemySubGroupAppearNtc()
                {
                    SubGroupId = subGroupId,
                    LayoutId = stageLayoutId.ToCDataStageLayoutId(),
                };

                client.Party.SendToAll(subgroupNtc);
            }

            if (response.EnemyList.Count > 0)
            {
                uint recommendedLevel = Server.LevelSyncManager.GetRecommendedLevel(stageLayoutId.Id);
                if (recommendedLevel > 0)
                {
                    string enemySummary = string.Join(",",
                        response.EnemyList.Select(e =>
                            $"{e.EnemyInfo.Lv}{(e.EnemyInfo.IsManualSet ? "m" : "a")}" +
                            (e.EnemyInfo.IsManualSet ? $"t{e.EnemyInfo.StartThinkTblNo}" : "")));
                    Logger.Info(
                        $"[LEVELSYNC] spawn-list stage={stageLayoutId} sub={subGroupId} " +
                        $"quest={response.QuestId} enemies=[{enemySummary}] recLv={recommendedLevel}");
                }
            }

            if (stageLayoutId.Id != 0)
            {
                StageLayoutId previousLayout = client.InstanceLayoutId;
                if (client.TryAdoptInstanceLayout(stageLayoutId))
                {
                    if (stageLayoutId.GroupId != 0)
                    {
                        Server.SupplyCacheManager.SyncCachesForLayout(client, stageLayoutId, queue);
                    }

                    if (!StageManager.IsDungeon(stageLayoutId.Id))
                    {
                        Server.SupplyCacheManager.HandleFieldLayoutLoad(
                            client,
                            stageLayoutId,
                            previousLayout,
                            queue);
                    }
                }
            }

            if (stageLayoutId.Id != 0 && !StageManager.IsDungeon(stageLayoutId.Id))
            {
                response.DropItemSetList = Server.SupplyCacheManager.GetDropSetList(client, stageLayoutId);
            }

            client.Enqueue(response, queue);
            client.RememberEnemySetListRes(stageLayoutId, response);

            Logger.Info($"StageId={stageLayoutId}, SubGroupId={request.SubGroupId}, DropSets={response.DropItemSetList.Count}");

            return queue;
        }
    }
}

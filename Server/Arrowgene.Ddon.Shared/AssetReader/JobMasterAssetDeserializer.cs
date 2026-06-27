using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;

namespace Arrowgene.Ddon.Shared.AssetReader
{
    public class JobMasterAssetDeserializer : IAssetDeserializer<JobMasterAsset>
    {
        private static readonly ILogger Logger = LogProvider.Logger(typeof(JobMasterAssetDeserializer));

        public JobMasterAsset ReadPath(string path)
        {
            Logger.Info($"Reading {path}");

            JobMasterAsset asset = new();

            string json = Util.ReadAllText(path);
            JsonDocument document = JsonDocument.Parse(json);

            foreach (var jJobMasterData in document.RootElement.EnumerateArray())
            {
                if (!Enum.TryParse(jJobMasterData.GetProperty("job").GetString(), true, out JobId jobId))
                {
                    var name = jJobMasterData.GetProperty("job_id").GetString();
                    Logger.Error($"Failed to parse JobId={name}. Skipping.");
                    continue;
                }

                if (!asset.JobOrders.ContainsKey(jobId))
                {
                    asset.JobOrders[jobId] = new Dictionary<ReleaseType, Dictionary<uint, List<CDataActiveJobOrder>>>();
                    asset.JobOrders[jobId][ReleaseType.CustomSkill] = new Dictionary<uint, List<CDataActiveJobOrder>>();
                    asset.JobOrders[jobId][ReleaseType.Augment] = new Dictionary<uint, List<CDataActiveJobOrder>>();
                }

                if (!Enum.TryParse(jJobMasterData.GetProperty("release_type").GetString(), true, out ReleaseType releaseType))
                {
                    var name = jJobMasterData.GetProperty("name").GetString();
                    Logger.Error($"Failed to parse JobTrainingReleaseType={name}. Skipping.");
                    continue;
                }

                foreach (var jSkill in jJobMasterData.GetProperty("skills").EnumerateArray())
                {
                    uint releaseId;
                    if (releaseType == ReleaseType.CustomSkill)
                    {
                        if (!Enum.TryParse(jSkill.GetProperty("name").GetString(), true, out CustomSkillId customSkillId))
                        {
                            var name = jSkill.GetProperty("name").GetString();
                            Logger.Error($"Failed to parse JobTrainingReleaseType={name}. Skipping.");
                            continue;
                        }

                        releaseId = customSkillId.ReleaseId();
                    }
                    else if (releaseType == ReleaseType.Augment)
                    {
                        releaseId = jSkill.GetProperty("id").GetUInt32();
                    }
                    else
                    {
                        throw new Exception("Invalid upgrade type!");
                    }

                    asset.JobOrders[jobId][releaseType][releaseId] = new List<CDataActiveJobOrder>();

                    var tasks = new Dictionary<uint, CDataActiveJobOrder>();
                    foreach (var jTask in jSkill.GetProperty("tasks").EnumerateArray())
                    {
                        var releaseLv = jTask.GetProperty("skill_level").GetByte();
                        if (!tasks.ContainsKey(releaseLv))
                        {
                            tasks[releaseLv] = new CDataActiveJobOrder()
                            {
                                ReleaseType = releaseType,
                                ReleaseId = releaseId,
                                ReleaseLv = releaseLv,
                            };
                        }

                        if (!Enum.TryParse(jTask.GetProperty("objective").GetString(), true, out JobOrderCondType conditionType))
                        {
                            var name = jTask.GetProperty("objective").GetString();
                            Logger.Error($"Failed to parse Objectvie={name}. Skipping.");
                            continue;
                        }

                        var condition = new CDataJobOrderProgress()
                        {
                            ConditionType = conditionType
                        };

                        if (conditionType == JobOrderCondType.Hunt)
                        {
                            if (!TryReadEnemyUIId(jTask, out var enemyUIId))
                            {
                                Logger.Error($"Failed to parse EnemyUIId. Skipping.");
                                continue;
                            }

                            condition.TargetId = (uint)enemyUIId;
                            condition.TargetRank = jTask.GetProperty("enemy_level").GetUInt32();
                            condition.TargetNum = jTask.GetProperty("enemy_count").GetUInt32();
                        }
                        else if (conditionType == JobOrderCondType.BloodOrbEnemies)
                        {
                            condition.TargetRank = jTask.GetProperty("enemy_level").GetUInt32();
                            condition.TargetNum = jTask.GetProperty("enemy_count").GetUInt32();
                        }
                        else
                        {
                            if (!Enum.TryParse(jTask.GetProperty("item_id").GetString(), true, out ItemId itemId))
                            {
                                var name = jTask.GetProperty("item_id").GetString();
                                Logger.Error($"Failed to parse ItemId={name}. Skipping.");
                                continue;
                            }

                            condition.TargetId = (uint)itemId;
                        }

                        tasks[releaseLv].JobOrderProgressList.Add(condition);
                    }

                    foreach (var (_, task) in tasks)
                    {
                        asset.JobOrders[jobId][releaseType][releaseId].Add(task);
                    }
                }
            }

            return asset;
        }

        private static bool TryReadEnemyUIId(JsonElement jTask, out EnemyUIId enemyUIId)
        {
            enemyUIId = EnemyUIId.None;
            return jTask.TryGetProperty("enemy_ui_id", out var enemyUIIdElement)
                && TryReadEnemyUIIdValue(enemyUIIdElement, out enemyUIId);
        }

        private static bool TryReadEnemyUIIdValue(JsonElement element, out EnemyUIId enemyUIId)
        {
            enemyUIId = EnemyUIId.None;

            if (element.ValueKind == JsonValueKind.Number)
            {
                if (!element.TryGetUInt32(out var value))
                {
                    return false;
                }

                enemyUIId = (EnemyUIId)value;
                return Enum.IsDefined(typeof(EnemyUIId), enemyUIId);
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var valueText = element.GetString();
            if (uint.TryParse(valueText, out var numericValue))
            {
                enemyUIId = (EnemyUIId)numericValue;
                return Enum.IsDefined(typeof(EnemyUIId), enemyUIId);
            }

            return Enum.TryParse(valueText, true, out enemyUIId);
        }
    }
}

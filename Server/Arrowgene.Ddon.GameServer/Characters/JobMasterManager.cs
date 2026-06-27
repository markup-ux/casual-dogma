using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Characters
{
    public class JobMasterManager
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(JobMasterManager));
        private static readonly Dictionary<uint, uint[]> EnemyUIIdAliases = new()
        {
            [(uint)EnemyUIId.Golgorran0] = [(uint)EnemyUIId.Golgorran0, (uint)EnemyUIId.Golgorran1],
            [(uint)EnemyUIId.Golgorran1] = [(uint)EnemyUIId.Golgorran0, (uint)EnemyUIId.Golgorran1],
        };

        private DdonGameServer Server;

        public JobMasterManager(DdonGameServer server)
        {
            Server = server;
        }

        public PacketQueue HandleEnemyKill(GameClient client, InstancedEnemy enemy, DbConnection? connectionIn = null)
        {
            PacketQueue packets = new();

            var targetId = (uint)enemy.EnemyId.GetUIId();
            if (targetId == 0)
            {
                // TODO: Should we log a message?
                // Enemy with no tracking killed
                return new();
            }

            var jobId = client.Character.ActiveCharacterJobData.Job;
            if (!client.Character.HasContentReleased(jobId.JobTrainingReleaseId()) || !client.Character.JobMasterActiveOrders.ContainsKey(jobId))
            {
                // Either job training is not released for
                // this class or there are no Active job
                // master requests, so skip
                return new();
            }

            bool isOrbEnemy = enemy.BloodOrbs > 0;

            var matchingOrders = client.Character.JobMasterActiveOrders[jobId]
                .Where(x => x.JobOrderProgressList.Any(c => TargetMatchesKilledEnemy(c, targetId, isOrbEnemy)))
                .Where(x => x.OrderAccepted)
                .ToList();
            if (matchingOrders.Count == 0)
            {
                // No order for this enemy exists
                return new();
            }

            //Kill count bonus for having a partner pawn present
            uint partnerKillBonus = Server.PartnerPawnManager.IsPartnerPawnInParty(client) ? Server.GameSettings.GameServerSettings.JobTrainingPartnerBonus : 0;

            foreach (var matchingOrder in matchingOrders)
            {
                bool updatedRecord = false;

                var matchingProgress = matchingOrder.JobOrderProgressList
                    .Where(x => TargetMatchesKilledEnemy(x, targetId, isOrbEnemy))
                    .Where(x => x.CurrentNum < x.TargetNum)
                    .Where(x => enemy.Lv >= x.TargetRank)
                    .ToList();

                foreach (var orderProgress in matchingProgress)
                {
                    updatedRecord = true;
                    orderProgress.CurrentNum += 1 + partnerKillBonus;

                    if (!Server.Database.UpsertJobMasterActiveOrdersProgress(client.Character.CharacterId, jobId, matchingOrder.ReleaseType, matchingOrder.ReleaseId, orderProgress, connectionIn))
                    {
                        throw new ResponseErrorException(ErrorCode.ERROR_CODE_DB_FAILURE, "Failed to upsert job training record");
                    }
                }

                if (updatedRecord && matchingOrder.JobOrderProgressList.All(x => x.CurrentNum >= x.TargetNum))
                {
                    // Alert the player that they completed their training
                    packets.Enqueue(client, new S2CJobOrderCompleteNtc()
                    {
                        JobId = jobId,
                        RewardLv = matchingOrder.ReleaseLv,
                        RewardNo = matchingOrder.ReleaseId,
                        RewardType = matchingOrder.ReleaseType
                    });
                }
            }

            return packets;
        }

        public List<CDataCommonU32> GetNewOrders(GameClient client, JobId jobId, DbConnection? connectionIn = null)
        {
            var newOrders = client.Character.JobMasterActiveOrders
                .Where(x => x.Key == jobId)
                .SelectMany(x => x.Value)
                .Where(x => !x.OrderAccepted)
                .ToList();

            foreach (var order in newOrders)
            {
                order.OrderAccepted = true;
                Server.Database.UpsertJobMasterActiveOrder(client.Character.CharacterId, jobId, order, connectionIn);
            }

            return newOrders.Select(x => new CDataCommonU32(x.ReleaseId)).ToList();
        }

        public List<CDataReleaseElement> GetReleasedElements(GameClient client, JobId jobId)
        {
            return client.Character.JobMasterReleasedElements[jobId];
        }

        public List<CDataReleaseElement> GetNewReleasedElements(GameClient client, JobId jobId, DbConnection? connectionIn = null)
        {
            var completedOrders = client.Character.JobMasterActiveOrders[jobId]
                    .Where(x => x.JobOrderProgressList.All(c => c.CurrentNum >= c.TargetNum)).ToList();

            var newReleasedElement = new List<CDataReleaseElement>();
            foreach (var completedOrder in completedOrders)
            {
                var releasedElement = new CDataReleaseElement()
                {
                    ReleaseId = completedOrder.ReleaseId,
                    ReleaseLv = completedOrder.ReleaseLv,
                    ReleaseType = completedOrder.ReleaseType,
                };

                if (!Server.Database.DeleteJobMasterActiveOrder(client.Character.CharacterId, jobId, completedOrder, connectionIn))
                {
                    throw new ResponseErrorException(ErrorCode.ERROR_CODE_DB_FAILURE, "Failed to delete completed job training order");
                }

                if (!Server.Database.HasJobMasterReleasedElement(client.Character.CharacterId, jobId, releasedElement, connectionIn)
                    && !Server.Database.InsertJobMasterReleasedElement(client.Character.CharacterId, jobId, releasedElement, connectionIn))
                {
                    throw new ResponseErrorException(ErrorCode.ERROR_CODE_DB_FAILURE, "Failed to insert completed job training release");
                }

                newReleasedElement.Add(releasedElement);

                // Add to internal state
                client.Character.JobMasterReleasedElements[jobId].Add(releasedElement);

                // Remove tracking for completed order
                client.Character.JobMasterActiveOrders[jobId].RemoveAll(x =>
                    x.ReleaseType == completedOrder.ReleaseType &&
                    x.ReleaseLv == completedOrder.ReleaseLv &&
                    x.ReleaseId == completedOrder.ReleaseId);
                    
                // Update existing skill
                if (completedOrder.ReleaseType == ReleaseType.CustomSkill)
                {
                    Server.CharacterManager.UnlockCustomSkill(client.Character, jobId, releasedElement.ReleaseId, releasedElement.ReleaseLv);
                }
                else if (completedOrder.ReleaseType == ReleaseType.Augment)
                {
                    Server.CharacterManager.UnlockAbility(client.Character, jobId, (AbilityId)releasedElement.ReleaseId, releasedElement.ReleaseLv);
                }
                else
                {
                    throw new Exception("Unexpected release type associated with the jt task");
                }
            }

            return newReleasedElement;
        }

        public void ScheduleCustomSkillTrainingTask(GameClient client, JobId jobId, CustomSkill customSkill, DbConnection? connectionIn = null)
        {
            if (jobId == JobId.None)
            {
                return;
            }

            var match = Server.AssetRepository.JobMasterAsset.JobOrders[jobId][ReleaseType.CustomSkill]
                .SelectMany(x => x.Value)
                .Where(x => x.ReleaseId == customSkill.SkillId)
                .Where(x => x.ReleaseLv == customSkill.SkillLv + 1)
                .FirstOrDefault();
            if (match == null)
            {
                return;
            }

            // Create or refresh the order. Re-learning/upgrading around an already active task should be idempotent.
            if (!Server.Database.UpsertJobMasterActiveOrder(client.Character.CharacterId, jobId, CloneOrder(match), connectionIn))
            {
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_DB_FAILURE, "Failed to upsert custom skill job training order");
            }

            // Update the stored orders
            client.Character.JobMasterActiveOrders[jobId] = GetJobMasterActiveOrders(client.Character, jobId, connectionIn);
        }

        public void ScheduleAbilityTrainingTask(GameClient client, JobId jobId, Ability ability, DbConnection? connectionIn = null)
        {
            if (jobId == JobId.None)
            {
                return;
            }

            var match = Server.AssetRepository.JobMasterAsset.JobOrders[jobId][ReleaseType.Augment]
                .SelectMany(x => x.Value)
                .Where(x => x.ReleaseLv == (ability.AbilityLv + 1) && x.ReleaseId == (uint)ability.AbilityId)
                .FirstOrDefault();
            if (match == null)
            {
                return;
            }

            // Create or refresh the order. Re-learning/upgrading around an already active task should be idempotent.
            if (!Server.Database.UpsertJobMasterActiveOrder(client.Character.CharacterId, jobId, CloneOrder(match), connectionIn))
            {
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_DB_FAILURE, "Failed to upsert augment job training order");
            }

            // Update the stored orders
            client.Character.JobMasterActiveOrders[jobId] = GetJobMasterActiveOrders(client.Character, jobId, connectionIn);
        }

        public List<CDataActiveJobOrder> GetJobMasterActiveOrders(Character character, JobId jobId, DbConnection? connectionIn)
        {
            var results = Server.Database.GetJobMasterActiveOrders(character.CharacterId, jobId, connectionIn);
            List<CDataActiveJobOrder> activeOrders = new();
            foreach (var activeOrder in results)
            {
                var progress = Server.Database.GetJobMasterActiveOrderProgress(character.CharacterId, jobId, activeOrder.ReleaseType, activeOrder.ReleaseId, connectionIn);
                var template = GetOrderTemplate(jobId, activeOrder);
                if (template == null)
                {
                    Logger.Info($"No job master asset template found for CharacterId={character.CharacterId}, JobId={jobId}, ReleaseType={activeOrder.ReleaseType}, ReleaseId={activeOrder.ReleaseId}, ReleaseLv={activeOrder.ReleaseLv}. Removing stale active order.");
                    if (!Server.Database.DeleteJobMasterActiveOrder(character.CharacterId, jobId, activeOrder, connectionIn))
                    {
                        throw new ResponseErrorException(ErrorCode.ERROR_CODE_DB_FAILURE, "Failed to delete stale job training order");
                    }
                    continue;
                }

                var hydratedOrder = CloneOrder(template);
                hydratedOrder.OrderAccepted = activeOrder.OrderAccepted;
                ApplyStoredProgress(hydratedOrder, progress);
                activeOrders.Add(hydratedOrder);
            }
            return activeOrders;
        }

        public PacketQueue HandleAreaChange(GameClient client)
        {
            var packets = new PacketQueue();

            var jobId = client.Character.ActiveCharacterJobData.Job;
            if (!client.Character.HasContentReleased(jobId.JobTrainingReleaseId()) || !client.Character.JobMasterActiveOrders.ContainsKey(jobId))
            {
                return new();
            }

            var ntc = new S2CJobMasterMarkTargetsNtc();
            foreach (var order in client.Character.JobMasterActiveOrders[jobId])
            {
                if (!order.OrderAccepted)
                {
                    continue;
                }

                foreach (var target in order.JobOrderProgressList)
                {
                    if (target.CurrentNum >= target.TargetNum)
                    {
                        continue;
                    }

                    foreach (var targetId in GetEquivalentEnemyUIIds(target.TargetId))
                    {
                        ntc.TargetList.Add(new CDataJobMasterTargetData()
                        {
                            ReleaseType = order.ReleaseType,
                            ReleaseLv = order.ReleaseLv,
                            ReleaseId = order.ReleaseId,
                            Condition = target.ConditionType,
                            TargetId = targetId,
                            TargetRank = target.TargetRank,
                        });
                    }
                }
            }
            packets.Enqueue(client, ntc);

            return packets;
        }

        private static bool TargetMatchesKilledEnemy(CDataJobOrderProgress target, uint killedTargetId, bool isOrbEnemy)
        {
            return (target.ConditionType == JobOrderCondType.BloodOrbEnemies && isOrbEnemy)
                || GetEquivalentEnemyUIIds(target.TargetId).Contains(killedTargetId);
        }

        private static uint[] GetEquivalentEnemyUIIds(uint targetId)
        {
            return EnemyUIIdAliases.GetValueOrDefault(targetId, [targetId]);
        }

        private CDataActiveJobOrder GetOrderTemplate(JobId jobId, CDataActiveJobOrder activeOrder)
        {
            if (!Server.AssetRepository.JobMasterAsset.JobOrders.TryGetValue(jobId, out var jobOrders)
                || !jobOrders.TryGetValue(activeOrder.ReleaseType, out var releaseOrders)
                || !releaseOrders.TryGetValue(activeOrder.ReleaseId, out var orders))
            {
                return null;
            }

            return orders
                .Where(x => x.ReleaseType == activeOrder.ReleaseType)
                .Where(x => x.ReleaseId == activeOrder.ReleaseId)
                .Where(x => x.ReleaseLv == activeOrder.ReleaseLv)
                .FirstOrDefault();
        }

        private static CDataActiveJobOrder CloneOrder(CDataActiveJobOrder source)
        {
            return new CDataActiveJobOrder()
            {
                ReleaseType = source.ReleaseType,
                ReleaseId = source.ReleaseId,
                ReleaseLv = source.ReleaseLv,
                OrderAccepted = source.OrderAccepted,
                JobOrderProgressList = source.JobOrderProgressList.Select(CloneProgress).ToList()
            };
        }

        private static CDataJobOrderProgress CloneProgress(CDataJobOrderProgress source)
        {
            return new CDataJobOrderProgress()
            {
                ConditionType = source.ConditionType,
                TargetId = source.TargetId,
                TargetRank = source.TargetRank,
                TargetNum = source.TargetNum,
                CurrentNum = source.CurrentNum,
            };
        }

        private static void ApplyStoredProgress(CDataActiveJobOrder order, List<CDataJobOrderProgress> storedProgress)
        {
            var progressLookup = storedProgress
                .GroupBy(x => (x.ConditionType, x.TargetId, x.TargetRank))
                .ToDictionary(x => x.Key, x => x.Max(y => y.CurrentNum));

            foreach (var condition in order.JobOrderProgressList)
            {
                if (!progressLookup.TryGetValue((condition.ConditionType, condition.TargetId, condition.TargetRank), out var currentNum))
                {
                    continue;
                }

                condition.CurrentNum = Math.Min(currentNum, condition.TargetNum);
            }
        }
    }
}

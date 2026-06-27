using Arrowgene.Ddon.GameServer.Quests;
using Arrowgene.Ddon.Shared;
using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Scripting.Interfaces
{
    public abstract class IQuest
    {
        public IQuest()
        {
            Processes = new Dictionary<ushort, QuestProcess>();
            RewardItems = new List<QuestRewardItem>();
            WalletRewards = new List<QuestWalletReward>();
            PointRewards = new List<QuestPointReward>();
            RepeatClearRewardItems = new List<QuestRewardItem>();
            RepeatClearWalletRewards = new List<QuestWalletReward>();
            RepeatClearPointRewards = new List<QuestPointReward>();
            FirstClearRewardItems = new List<QuestRewardItem>();
            FirstClearWalletRewards = new List<QuestWalletReward>();
            FirstClearPointRewards = new List<QuestPointReward>();
            PeriodFirstClearRewardItems = new List<QuestRewardItem>();
            PeriodFirstClearWalletRewards = new List<QuestWalletReward>();
            PeriodFirstClearPointRewards = new List<QuestPointReward>();
            HelperRewardItems = new List<QuestRewardItem>();
            HelperWalletRewards = new List<QuestWalletReward>();
            HelperPointRewards = new List<QuestPointReward>();
            EnemyGroups = new Dictionary<uint, QuestEnemyGroup>();
            MissionParams = new QuestMissionParams();
            QuestLayoutSetInfoSetList = new List<QuestLayoutFlagSetInfo>();
            OrderConditions = new List<QuestOrderCondition>();
            ServerActions = new List<QuestServerAction>();
            ContentsReleased = new HashSet<QuestUnlock>();
            WorldManageUnlocks = new Dictionary<QuestId, List<QuestFlagInfo>>();
            QuestProgressWork = new List<QuestProgressWork>();

            if (OverrideEnemySpawn == null)
            {
                OverrideEnemySpawn = (QuestType == QuestType.Main);
            }

            if (EnableCancel == null)
            {
                EnableCancel = (QuestType != QuestType.Tutorial && QuestType != QuestType.Main);
            }

            if (QuestType == QuestType.WildHunt && QuestOrderBackgroundImage == 0)
            {
                throw new Exception("Quests of the type 'Wild Hunt' require a background image defined");
            }

            AdventureGuideCategory ??= QuestUtils.DetermineQuestAdventureCategory(QuestId, QuestType);
            IsImportant ??= QuestUtils.DetermineIfQuestIsImportant(AdventureGuideCategory.Value);
        }

        public string Path { get; set; }
        public abstract QuestType QuestType { get; }
        public abstract QuestId QuestId { get; }
        public virtual QuestId NextQuestId { get; } = QuestId.None;
        public virtual uint VariantId { get; } = 0;
        public virtual uint QuestScheduleId {
            get
            {
                return LibDdon.Assets.QuestScheduleIdAsset[QuestId] + VariantId;
            }
        }
        public virtual QuestAreaId QuestAreaId { get; } = QuestAreaId.None;
        public QuestSource QuestSource { get; } = QuestSource.Script;
        public virtual bool Enabled { get; } = true;
        public virtual uint QuestOrderBackgroundImage { get; } = 0;
        public virtual StageInfo StageInfo { get; } = Stage.Invalid;
        public virtual uint NewsImageId { get; } = 0;
        public abstract ushort RecommendedLevel { get; }
        public abstract byte MinimumItemRank { get; }
        public abstract bool IsDiscoverable { get; }
        public virtual bool? OverrideEnemySpawn { get; } = null;
        public virtual bool? EnableCancel { get; protected set; } = null;
        public virtual bool ResetPlayerAfterQuest { get; } = false;

        public virtual bool? IsImportant { get; } = null;
        public virtual QuestAdventureGuideCategory? AdventureGuideCategory { get; } = null;

        protected Dictionary<ushort, QuestProcess> Processes { get; set; }
        protected List<QuestRewardItem> RewardItems { get; set; }
        protected List<QuestWalletReward> WalletRewards { get; set; }
        protected List<QuestPointReward> PointRewards { get; set; }
        protected List<QuestRewardItem> RepeatClearRewardItems { get; set; }
        protected List<QuestWalletReward> RepeatClearWalletRewards { get; set; }
        protected List<QuestPointReward> RepeatClearPointRewards { get; set; }
        protected List<QuestRewardItem> FirstClearRewardItems { get; set; }
        protected List<QuestWalletReward> FirstClearWalletRewards { get; set; }
        protected List<QuestPointReward> FirstClearPointRewards { get; set; }
        protected List<QuestRewardItem> PeriodFirstClearRewardItems { get; set; }
        protected List<QuestWalletReward> PeriodFirstClearWalletRewards { get; set; }
        protected List<QuestPointReward> PeriodFirstClearPointRewards { get; set; }
        protected List<QuestRewardItem> HelperRewardItems { get; set; }
        protected List<QuestWalletReward> HelperWalletRewards { get; set; }
        protected List<QuestPointReward> HelperPointRewards { get; set; }
        protected Dictionary<uint, QuestEnemyGroup> EnemyGroups { get; set; }
        protected List<QuestLayoutFlagSetInfo> QuestLayoutSetInfoSetList { get; set; }
        protected QuestMissionParams MissionParams { get; set; }
        protected List<QuestOrderCondition> OrderConditions { get; set; }
        protected List<QuestServerAction> ServerActions { get; set; }
        public HashSet<QuestUnlock> ContentsReleased { get; set; }
        public Dictionary<QuestId, List<QuestFlagInfo>> WorldManageUnlocks { get; set; }
        public List<QuestProgressWork> QuestProgressWork { get; set; }

        public void Initialize(string path)
        {
            Path = path;

            InitializeState();
            InitializeRewards();
            InitializeEnemyGroups();
            InitializeBlocks();
        }

        protected virtual void InitializeState()
        {
        }

        protected virtual void InitializeRewards()
        {
        }

        protected virtual void InitializeEnemyGroups()
        {
        }

        protected virtual void InitializeBlocks()
        {
        }

        public virtual bool AcceptRequirementsMet(GameClient client)
        {
            return Enabled;
        }

        public virtual bool ShowInAdventureGuide(GameClient client)
        {
            return Enabled;
        }

        public virtual void InitializeInstanceState(QuestState questState)
        {
        }

        public void AddItemReward(QuestRewardItem reward)
        {
            if (reward != null)
            {
                RewardItems.Add(reward);
            }
        }

        public void AddItemReward(QuestRewardBucket bucket, QuestRewardItem reward)
        {
            if (reward == null)
            {
                return;
            }

            switch (bucket)
            {
                case QuestRewardBucket.Normal:
                    RewardItems.Add(reward);
                    break;
                case QuestRewardBucket.RepeatClear:
                    RepeatClearRewardItems.Add(reward);
                    break;
                case QuestRewardBucket.FirstClear:
                    FirstClearRewardItems.Add(reward);
                    break;
                case QuestRewardBucket.PeriodFirstClear:
                    PeriodFirstClearRewardItems.Add(reward);
                    break;
                case QuestRewardBucket.Helper:
                    HelperRewardItems.Add(reward);
                    break;
            }
        }

        public void AddFixedItemReward(ItemId itemId, ushort amount, bool isHidden = false)
        {
            AddItemReward(QuestFixedRewardItem.Create(itemId, amount, isHidden));
        }

        public void AddFixedItemReward(uint itemId, ushort amount)
        {
            AddFixedItemReward((ItemId) itemId, amount);
        }

        public void AddFixedItemReward(ItemId itemId, ushort amount, QuestRewardBucket bucket, QuestItemInstance instance = null, bool isHidden = false)
        {
            if (instance == null)
            {
                AddItemReward(bucket, QuestFixedRewardItem.Create(itemId, amount, isHidden));
                return;
            }

            var reward = new QuestInstancedFixedRewardItem(isHidden);
            reward.LootPool.Add(instance.ToLootPoolItem(itemId, amount));
            AddItemReward(bucket, reward);
        }

        public void AddFixedItemReward(uint itemId, ushort amount, QuestRewardBucket bucket, QuestItemInstance instance = null, bool isHidden = false)
        {
            AddFixedItemReward((ItemId)itemId, amount, bucket, instance, isHidden);
        }

        public void AddFixedInstancedItemReward(ItemId itemId, ushort amount, uint color = 0, uint plusValue = 0, uint safetySetting = 0, bool isHidden = false)
        {
            AddItemReward(QuestInstancedFixedRewardItem.Create(itemId, amount, color, plusValue, safetySetting, isHidden));
        }

        public void AddRandomChanceItemReward(List<(ItemId ItemId, ushort Amount, double Chance)> items, bool isHidden = false)
        {
            AddItemReward(QuestRandomChanceRewardItem.Create(items, isHidden));
        }

        public void AddRandomChanceItemReward(List<(ItemId ItemId, ushort Amount, double Chance)> items, QuestRewardBucket bucket, bool isHidden = false)
        {
            AddItemReward(bucket, QuestRandomChanceRewardItem.Create(items, isHidden));
        }

        public void AddRandomFixedItemReward(List<(ItemId ItemId, ushort Amount)> items, bool isHidden = false)
        {
            AddItemReward(QuestRandomFixedRewardItem.Create(items, isHidden));
        }

        public void AddRandomFixedItemReward(List<(ItemId ItemId, ushort Amount)> items, QuestRewardBucket bucket, bool isHidden = false)
        {
            AddItemReward(bucket, QuestRandomFixedRewardItem.Create(items, isHidden));
        }

        public void AddSelectItemReward(List<(ItemId ItemId, ushort Amount)> items, bool isHidden = false)
        {
            AddItemReward(QuestSelectRewardItem.Create(items, isHidden));
        }

        public void AddSelectItemReward(List<(ItemId ItemId, ushort Amount)> items, QuestRewardBucket bucket, bool isHidden = false)
        {
            AddItemReward(bucket, QuestSelectRewardItem.Create(items, isHidden));
        }

        public void AddPointReward(PointType pointType, uint amount)
        {
            PointRewards.Add(QuestPointReward.Create(pointType, amount));
        }

        public void AddPointReward(PointType pointType, uint amount, QuestRewardBucket bucket)
        {
            var reward = QuestPointReward.Create(pointType, amount);
            switch (bucket)
            {
                case QuestRewardBucket.Normal:
                    PointRewards.Add(reward);
                    break;
                case QuestRewardBucket.RepeatClear:
                    RepeatClearPointRewards.Add(reward);
                    break;
                case QuestRewardBucket.FirstClear:
                    FirstClearPointRewards.Add(reward);
                    break;
                case QuestRewardBucket.PeriodFirstClear:
                    PeriodFirstClearPointRewards.Add(reward);
                    break;
                case QuestRewardBucket.Helper:
                    HelperPointRewards.Add(reward);
                    break;
            }
        }

        public void AddWalletReward(WalletType walletType, uint amount)
        {
            WalletRewards.Add(QuestWalletReward.Create(walletType, amount));
        }

        public void AddWalletReward(WalletType walletType, uint amount, QuestRewardBucket bucket)
        {
            var reward = QuestWalletReward.Create(walletType, amount);
            switch (bucket)
            {
                case QuestRewardBucket.Normal:
                    WalletRewards.Add(reward);
                    break;
                case QuestRewardBucket.RepeatClear:
                    RepeatClearWalletRewards.Add(reward);
                    break;
                case QuestRewardBucket.FirstClear:
                    FirstClearWalletRewards.Add(reward);
                    break;
                case QuestRewardBucket.PeriodFirstClear:
                    PeriodFirstClearWalletRewards.Add(reward);
                    break;
                case QuestRewardBucket.Helper:
                    HelperWalletRewards.Add(reward);
                    break;
            }
        }

        // Repeat-clear reward helpers: call these from InitializeRewards()
        public void AddRepeatClearItemReward(QuestRewardItem reward)
        {
            if (reward != null)
                RepeatClearRewardItems.Add(reward);
        }

        public void AddRepeatClearFixedItemReward(ItemId itemId, ushort amount, bool isHidden = false)
        {
            AddRepeatClearItemReward(QuestFixedRewardItem.Create(itemId, amount, isHidden));
        }

        public void AddRepeatClearFixedInstancedItemReward(ItemId itemId, ushort amount, uint color = 0, uint plusValue = 0, uint safetySetting = 0, bool isHidden = false)
        {
            AddRepeatClearItemReward(QuestInstancedFixedRewardItem.Create(itemId, amount, color, plusValue, safetySetting, isHidden));
        }

        public void AddRepeatClearRandomFixedItemReward(List<(ItemId ItemId, ushort Amount)> items, bool isHidden = false)
        {
            AddRepeatClearItemReward(QuestRandomFixedRewardItem.Create(items, isHidden));
        }

        public void AddRepeatClearRandomChanceItemReward(List<(ItemId ItemId, ushort Amount, double Chance)> items, bool isHidden = false)
        {
            AddRepeatClearItemReward(QuestRandomChanceRewardItem.Create(items, isHidden));
        }

        public void AddRepeatClearPointReward(PointType pointType, uint amount)
        {
            RepeatClearPointRewards.Add(QuestPointReward.Create(pointType, amount));
        }

        public void AddRepeatClearWalletReward(WalletType walletType, uint amount)
        {
            RepeatClearWalletRewards.Add(QuestWalletReward.Create(walletType, amount));
        }

        public void AddFirstClearItemReward(QuestRewardItem reward)
        {
            if (reward != null)
                FirstClearRewardItems.Add(reward);
        }

        public void AddFirstClearFixedItemReward(ItemId itemId, ushort amount, bool isHidden = false)
        {
            AddFirstClearItemReward(QuestFixedRewardItem.Create(itemId, amount, isHidden));
        }

        public void AddFirstClearFixedInstancedItemReward(ItemId itemId, ushort amount, uint color = 0, uint plusValue = 0, uint safetySetting = 0, bool isHidden = false)
        {
            AddFirstClearItemReward(QuestInstancedFixedRewardItem.Create(itemId, amount, color, plusValue, safetySetting, isHidden));
        }

        public void AddFirstClearRandomFixedItemReward(List<(ItemId ItemId, ushort Amount)> items, bool isHidden = false)
        {
            AddFirstClearItemReward(QuestRandomFixedRewardItem.Create(items, isHidden));
        }

        public void AddFirstClearRandomChanceItemReward(List<(ItemId ItemId, ushort Amount, double Chance)> items, bool isHidden = false)
        {
            AddFirstClearItemReward(QuestRandomChanceRewardItem.Create(items, isHidden));
        }

        public void AddFirstClearPointReward(PointType pointType, uint amount)
        {
            FirstClearPointRewards.Add(QuestPointReward.Create(pointType, amount));
        }

        public void AddFirstClearWalletReward(WalletType walletType, uint amount)
        {
            FirstClearWalletRewards.Add(QuestWalletReward.Create(walletType, amount));
        }

        public void AddPeriodFirstClearItemReward(QuestRewardItem reward)
        {
            if (reward != null)
                PeriodFirstClearRewardItems.Add(reward);
        }

        public void AddPeriodFirstClearFixedItemReward(ItemId itemId, ushort amount, bool isHidden = false)
        {
            AddPeriodFirstClearItemReward(QuestFixedRewardItem.Create(itemId, amount, isHidden));
        }

        public void AddPeriodFirstClearFixedInstancedItemReward(ItemId itemId, ushort amount, uint color = 0, uint plusValue = 0, uint safetySetting = 0, bool isHidden = false)
        {
            AddPeriodFirstClearItemReward(QuestInstancedFixedRewardItem.Create(itemId, amount, color, plusValue, safetySetting, isHidden));
        }

        public void AddPeriodFirstClearRandomFixedItemReward(List<(ItemId ItemId, ushort Amount)> items, bool isHidden = false)
        {
            AddPeriodFirstClearItemReward(QuestRandomFixedRewardItem.Create(items, isHidden));
        }

        public void AddPeriodFirstClearRandomChanceItemReward(List<(ItemId ItemId, ushort Amount, double Chance)> items, bool isHidden = false)
        {
            AddPeriodFirstClearItemReward(QuestRandomChanceRewardItem.Create(items, isHidden));
        }

        public void AddPeriodFirstClearPointReward(PointType pointType, uint amount)
        {
            PeriodFirstClearPointRewards.Add(QuestPointReward.Create(pointType, amount));
        }

        public void AddPeriodFirstClearWalletReward(WalletType walletType, uint amount)
        {
            PeriodFirstClearWalletRewards.Add(QuestWalletReward.Create(walletType, amount));
        }

        public void AddHelperItemReward(QuestRewardItem reward)
        {
            if (reward != null)
                HelperRewardItems.Add(reward);
        }

        public void AddHelperFixedItemReward(ItemId itemId, ushort amount, bool isHidden = false)
        {
            AddHelperItemReward(QuestFixedRewardItem.Create(itemId, amount, isHidden));
        }

        public void AddHelperFixedInstancedItemReward(ItemId itemId, ushort amount, uint color = 0, uint plusValue = 0, uint safetySetting = 0, bool isHidden = false)
        {
            AddHelperItemReward(QuestInstancedFixedRewardItem.Create(itemId, amount, color, plusValue, safetySetting, isHidden));
        }

        public void AddHelperRandomFixedItemReward(List<(ItemId ItemId, ushort Amount)> items, bool isHidden = false)
        {
            AddHelperItemReward(QuestRandomFixedRewardItem.Create(items, isHidden));
        }

        public void AddHelperRandomChanceItemReward(List<(ItemId ItemId, ushort Amount, double Chance)> items, bool isHidden = false)
        {
            AddHelperItemReward(QuestRandomChanceRewardItem.Create(items, isHidden));
        }

        public void AddHelperPointReward(PointType pointType, uint amount)
        {
            HelperPointRewards.Add(QuestPointReward.Create(pointType, amount));
        }

        public void AddHelperWalletReward(WalletType walletType, uint amount)
        {
            HelperWalletRewards.Add(QuestWalletReward.Create(walletType, amount));
        }

        public void AddQuestOrderCondition(QuestOrderConditionType type, int param01 = 0, int param02 = 0)
        {
            AddQuestOrderCondition(new QuestOrderCondition()
            {
                Type = type,
                Param01 = param01,
                Param02 = param02,
            });
        }

        public void AddQuestOrderCondition(QuestOrderCondition orderCondition)
        {
            OrderConditions.Add(orderCondition);
        }

        protected QuestEnemyGroup GetEnemyGroup(uint enemyGroupId)
        {
            return EnemyGroups[enemyGroupId];
        }

        public void AddEnemies(uint enemyGroupId, StageInfo stageInfo, uint groupId, byte subGroupId, QuestEnemyPlacementType placementType, List<InstancedEnemy> enemies, QuestTargetType targetType = QuestTargetType.EnemyForOrder)
        {
            if (!EnemyGroups.ContainsKey(enemyGroupId))
            {
                EnemyGroups[enemyGroupId] = new QuestEnemyGroup()
                {
                    StageLayoutId = stageInfo.AsStageLayoutId(groupId),
                    SubGroupId = subGroupId,
                    GroupId = enemyGroupId,
                    PlacementType = placementType,
                    TargetType = targetType
                };
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                enemy.StageLayoutId = stageInfo.AsStageLayoutId(groupId);
                enemy.QuestEnemyGroupId = enemyGroupId;
                enemy.QuestScheduleId = QuestScheduleId;

                if (placementType == QuestEnemyPlacementType.Automatic)
                {
                    enemy.Index = (byte) i;
                }
            }

            QuestLayoutSetInfoSetList.Add(new QuestLayoutFlagSetInfo()
            {
                FlagNo = enemyGroupId,
                GroupId = groupId,
                StageNo = stageInfo.StageNo,
            });

            EnemyGroups[enemyGroupId].Enemies.AddRange(enemies);
        }

        public void AddEnemies(uint id, StageInfo stageInfo, uint groupId, QuestEnemyPlacementType placementType, List<InstancedEnemy> enemies)
        {
            AddEnemies(id, stageInfo, groupId, 0, placementType, enemies);
        }

        public void AddServerAction(QuestSeverActionType actionType, OmInstantValueAction action, ulong key, uint value, StageInfo stageInfo, uint groupId)
        {
            ServerActions.Add(new QuestServerAction()
            {
                ActionType = actionType,
                OmInstantValueAction = action,
                Key = key,
                Value = value,
                StageLayoutId = stageInfo.AsStageLayoutId(0, groupId)
            });
        }

        public void AddContentsRelease(ContentsRelease contentsReleaseId)
        {
            ContentsReleased.Add(new QuestUnlock()
            {
                ReleaseId = contentsReleaseId
            });
        }

        public void AddWorldManageUnlock(QuestFlagInfo questFlagInfo)
        {
            if (!WorldManageUnlocks.ContainsKey(questFlagInfo.QuestId))
            {
                WorldManageUnlocks[questFlagInfo.QuestId] = new List<QuestFlagInfo>();
            }
            WorldManageUnlocks[questFlagInfo.QuestId].Add(questFlagInfo);
        }

        public void AddQuestProgressWork(QuestProgressWork questWork)
        {
            QuestProgressWork.Add(questWork);
        }

        private QuestProcess AddProcess(QuestProcess process)
        {
            if (Processes.ContainsKey(process.ProcessNo))
            {
                throw new Exception($"The process {process.ProcessNo} is defined multiple times in '{Path}'");
            }
            Processes[process.ProcessNo] = process;
            return process;
        }

        public QuestProcess AddNewProcess(ushort processNo)
        {
            return AddProcess(new QuestProcess(processNo, QuestScheduleId)
            {
                EnemyGroups = EnemyGroups
            });
        }

        private void ValidateQuestParams()
        {
            if (QuestType == QuestType.Tutorial)
            {
                if (StageInfo == Stage.Invalid)
                {
                    throw new Exception($"The tutorial quest '{Path}' requires a valid StageId to be assigned.");
                }
            }
            else if (QuestType == QuestType.World)
            {
                if (QuestAreaId == QuestAreaId.None)
                {
                    throw new Exception($"The world quest '{Path}' requires a valid QuestAreaId to be assigned.");
                }
            }
        }

        public Quest GenerateQuest(DdonGameServer server)
        {
            // Make sure certain required params are present based on the quest type
            ValidateQuestParams();

            var assetData = new QuestAssetData()
            {
                BaseLevel = RecommendedLevel,
                Discoverable = IsDiscoverable,
                Enabled = Enabled,
                EnemyGroups = EnemyGroups,
                NewsImageId = NewsImageId,
                MinimumItemRank = MinimumItemRank,
                NextQuestId = NextQuestId,
                OverrideEnemySpawn = OverrideEnemySpawn.Value,
                EnableCancel = EnableCancel.Value,
                QuestAreaId = QuestAreaId,
                QuestId = QuestId,
                VariantIndex = VariantId,
                QuestOrderBackgroundImage = QuestOrderBackgroundImage,
                IsImportant = IsImportant.Value,
                AdventureGuideCategory = AdventureGuideCategory.Value,
                QuestSource = QuestSource,
                QuestType = QuestType,
                PointRewards = PointRewards,
                Processes = Processes.Values.ToList(),
                RewardItems = RewardItems,
                RewardCurrency = WalletRewards,
                RepeatClearRewardItems = RepeatClearRewardItems,
                RepeatClearRewardCurrency = RepeatClearWalletRewards,
                RepeatClearPointRewards = RepeatClearPointRewards,
                FirstClearRewardItems = FirstClearRewardItems,
                FirstClearRewardCurrency = FirstClearWalletRewards,
                FirstClearPointRewards = FirstClearPointRewards,
                PeriodFirstClearRewardItems = PeriodFirstClearRewardItems,
                PeriodFirstClearRewardCurrency = PeriodFirstClearWalletRewards,
                PeriodFirstClearPointRewards = PeriodFirstClearPointRewards,
                HelperRewardItems = HelperRewardItems,
                HelperRewardCurrency = HelperWalletRewards,
                HelperPointRewards = HelperPointRewards,
                StageLayoutId = StageInfo.AsStageLayoutId(0, 0),
                ResetPlayerAfterQuest = ResetPlayerAfterQuest,
                MissionParams = MissionParams,
                QuestLayoutSetInfoFlags = QuestLayoutSetInfoSetList,
                OrderConditions = OrderConditions,
                ServerActions = ServerActions,
                ContentsReleased = ContentsReleased,
                WorldManageUnlocks = WorldManageUnlocks,
                QuestProgressWork = QuestProgressWork,
            };

            // TODO: Generate a ScriptedQuest instead which is more customizable
            return GenericQuest.FromAsset(server, assetData, this);
        }
    }
}

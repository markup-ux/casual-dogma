using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Arrowgene.Ddon.Shared.AssetReader
{
    public class QuestAssetDeserializer : IDirectoryAssetHandler
    {
        private static readonly ILogger Logger = LogProvider.Logger(typeof(QuestAssetDeserializer));

        private QuestDropItemAsset _QuestDrops;
        private AssetCommonDeserializer _CommonEnemyDeserializer;
        private Dictionary<QuestId, uint> _QuestScheduleIdAsset;
        private readonly QuestAsset _liveAsset;

        public QuestAssetDeserializer(AssetCommonDeserializer commonEnemyDeserializer, QuestDropItemAsset questDrops, Dictionary<QuestId, uint> scheduleIdAsset, QuestAsset liveAsset)
        {
            _QuestDrops = questDrops;
            _CommonEnemyDeserializer = commonEnemyDeserializer;
            _QuestScheduleIdAsset = scheduleIdAsset;
            _liveAsset = liveAsset;

            // Force this class to be invoked so we can look up flags during deserialization
            QuestFlags.InvokeTypeInitializer();
        }

        public string DirectoryKey => AssetRepository.QuestAssestKey;
        public string Filter => "*.json";
        public object LiveAsset => _liveAsset;

        public bool OnFileChanged(string filePath)
        {
            var freshEntry = new QuestAsset();
            if (!LoadQuestFromFile(filePath, freshEntry))
                return false;
            OnFileRemoved(filePath);
            _liveAsset.Quests.AddRange(freshEntry.Quests);
            return true;
        }

        public void OnFileRemoved(string filePath)
        {
            _liveAsset.Quests.RemoveAll(q => q.SourceFile == filePath);
        }

        public bool LoadQuestsFromDirectory(string path, QuestAsset questAssets)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                Logger.Error($"The directory '{path}' does not exist");
                return false;
            }

            Logger.Info($"Reading quest files from {path}");
            foreach (var file in info.EnumerateFiles())
            {
                LoadQuestFromFile(file.FullName, questAssets);
            }

            return true;
        }

        public bool LoadQuestFromFile(string filePath, QuestAsset questAssets)
        {
            Logger.Info($"{filePath}");

            string json = Util.ReadAllText(filePath);
            JsonDocument document = JsonDocument.Parse(json);

            var jQuest = document.RootElement;
            if (!Enum.TryParse(jQuest.GetProperty("state_machine").GetString(), true, out QuestStateMachineType questStateMachineType))
            {
                Logger.Error($"Expected key 'state_machine' not in the root of the document. Unable to parse {filePath}.");
                return false;
            }

            if (questStateMachineType != QuestStateMachineType.GenericStateMachine)
            {
                Logger.Error($"Unsupported QuestStateMachineType '{questStateMachineType}'. Unable to parse {filePath}.");
                return false;
            }

            QuestAssetData assetData = new QuestAssetData()
            {
                QuestSource = QuestSource.Json,
                SourceFile = filePath
            };

            if (!ParseQuest(assetData, jQuest))
            {
                Logger.Error($"Unable to parse '{filePath}'. Skipping.");
                return false;
            }

            questAssets.Quests.Add(assetData);
            return true;
        }

        private bool ParseQuest(QuestAssetData assetData, JsonElement jQuest)
        {
            if (!Enum.TryParse(jQuest.GetProperty("type").GetString(), true, out QuestType questType))
            {
                Logger.Error($"Unable to parse the quest type. Skipping.");
                return false;
            }

            assetData.QuestType = questType;
            assetData.QuestId = (QuestId)jQuest.GetProperty("quest_id").GetUInt32();
            assetData.BaseLevel = jQuest.GetProperty("base_level").GetUInt16();
            assetData.MinimumItemRank = jQuest.GetProperty("minimum_item_rank").GetByte();
            assetData.Discoverable = jQuest.GetProperty("discoverable").GetBoolean();

            assetData.QuestAreaId = QuestAreaId.None;
            if (jQuest.TryGetProperty("area_id", out JsonElement jAreaId)
                && Enum.TryParse(jAreaId.GetString(), true, out QuestAreaId areaId))
            {
                assetData.QuestAreaId = areaId;
            }

            assetData.StageLayoutId = StageLayoutId.Invalid;
            if ((questType == QuestType.Tutorial) || (questType == QuestType.Substory))
            {
                assetData.StageLayoutId = AssetCommonDeserializer.ParseStageId(jQuest.GetProperty("stage_id"));
            }

            assetData.NewsImageId = 0;
            if (jQuest.TryGetProperty("news_image", out JsonElement jNewsImage))
            {
                assetData.NewsImageId = jNewsImage.GetUInt32();
            }

            assetData.NextQuestId = 0;
            if (jQuest.TryGetProperty("next_quest", out JsonElement jNextQuest))
            {
                assetData.NextQuestId = (QuestId)jNextQuest.GetUInt32();
            }

            assetData.VariantIndex = 0;
            if (jQuest.TryGetProperty("variant_index", out JsonElement jVariantIndex))
            {
                assetData.VariantIndex = jVariantIndex.GetUInt32();
            }

            if (assetData.VariantIndex > 127)
            {
                Logger.Error($"Invalid variant number {assetData.VariantIndex} > 127 for quest {assetData.QuestId}.");
                return false;
            }

            if (_QuestScheduleIdAsset.TryGetValue(assetData.QuestId, out var baseScheduleId))
            {
                assetData.QuestScheduleId = baseScheduleId + assetData.VariantIndex;
            }
            else
            {
                Logger.Error($"Missing base scheduleId in asset for quest {assetData.QuestId}.");
                return false;
            }

            assetData.OverrideEnemySpawn = (assetData.QuestType == QuestType.Main || assetData.QuestType == QuestType.ExtremeMission);
            if (jQuest.TryGetProperty("override_enemy_spawn", out JsonElement jOverrideEnemySpawn))
            {
                assetData.OverrideEnemySpawn = jOverrideEnemySpawn.GetBoolean();
            }

            if (jQuest.TryGetProperty("quest_layout_set_info_flags", out JsonElement jLayoutSetInfoFlags))
            {
                foreach (var layoutFlag in jLayoutSetInfoFlags.EnumerateArray())
                {
                    assetData.QuestLayoutSetInfoFlags.Add(QuestLayoutFlagSetInfo.FromQuestAssetJson(layoutFlag));
                }
            }

            if (jQuest.TryGetProperty("quest_layout_flags", out JsonElement jLayoutFlags))
            {
                foreach (var layoutFlag in jLayoutFlags.EnumerateArray())
                {
                    assetData.QuestLayoutFlags.Add(new QuestLayoutFlag() { FlagId = layoutFlag.GetUInt32() });
                }
            }

            assetData.ResetPlayerAfterQuest = false;
            if (jQuest.TryGetProperty("reset_player_after_quest", out JsonElement jResetPlayerAfterQuest))
            {
                assetData.ResetPlayerAfterQuest = true;
            }

            if (questType == QuestType.ExtremeMission)
            {
                if (!jQuest.TryGetProperty("mission_params", out JsonElement jMissionParams))
                {
                    Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Missing 'mission_params'. Skipping.");
                    return false;
                }

                ParseMissionParams(assetData, jMissionParams);
            }

            assetData.QuestOrderBackgroundImage = 0;
            if (questType == QuestType.WildHunt)
            {
                if (!jQuest.TryGetProperty("order_background_id", out JsonElement jOrderBackgroundId))
                {
                    Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Missing 'order_background_id'. Skipping.");
                    return false;
                }

                assetData.QuestOrderBackgroundImage = jOrderBackgroundId.GetUInt32();
                if (assetData.QuestOrderBackgroundImage == 0)
                {
                    Logger.Error($"The value of 'order_background_id' must be > 0, for the quest '{assetData.QuestId}'. Skipping.");
                    return false;
                }
            }

            assetData.AdventureGuideCategory = QuestUtils.DetermineQuestAdventureCategory(assetData.QuestId, assetData.QuestType);
            if (jQuest.TryGetProperty("adventure_guide_category", out JsonElement jAdventureGuideCategory))
            {
                if (!Enum.TryParse(jAdventureGuideCategory.GetString(), true, out QuestAdventureGuideCategory adventureGuideCategory))
                {
                    Logger.Error($"The value of 'adventure_guide_category' '{jAdventureGuideCategory.GetString()}' for '{assetData.QuestId}' is invalid. Skipping.");
                    return false;
                }
                assetData.AdventureGuideCategory = adventureGuideCategory;
            }

            assetData.IsImportant = QuestUtils.DetermineIfQuestIsImportant(assetData.AdventureGuideCategory);
            if (jQuest.TryGetProperty("is_important", out JsonElement jIsImportant))
            {
                assetData.IsImportant = jIsImportant.GetBoolean();
            }

            if (jQuest.TryGetProperty("contents_release", out JsonElement jContentsReleaseList))
            {
                if (!ParseContentsRelease(assetData.ContentsReleased, jContentsReleaseList))
                {
                    return false;
                }

                foreach (var contentRelease in assetData.ContentsReleased)
                {
                    if (contentRelease.FlagInfo != null)
                    {
                        if (!assetData.WorldManageUnlocks.ContainsKey(contentRelease.FlagInfo.QuestId))
                        {
                            assetData.WorldManageUnlocks[contentRelease.FlagInfo.QuestId] = new List<QuestFlagInfo>();
                        }
                        assetData.WorldManageUnlocks[contentRelease.FlagInfo.QuestId].Add(contentRelease.FlagInfo);
                    }
                }
            }

            assetData.Enabled = true;
            if (jQuest.TryGetProperty("enabled", out JsonElement jQuestEnabled))
            {
                assetData.Enabled = jQuestEnabled.GetBoolean();
            }

            if (questType == QuestType.Light)
            {
                if (!jQuest.TryGetProperty("light_quest_details", out JsonElement jLightQuestDetails))
                {
                    Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Missing 'light_quest_details'. Skipping.");
                    return false;
                }

                ParseLightQuestDetails(assetData, jLightQuestDetails);
            }
            
            ParseRewards(assetData, jQuest);

            if (!ParseServerActions(assetData, jQuest))
            {
                return false;
            }

            if (!ParseOrderCondition(assetData, jQuest))
            {
                return false;
            }

            if (!_CommonEnemyDeserializer.ParseEnemyGroups(assetData.QuestScheduleId, _QuestDrops, assetData.EnemyGroups, jQuest))
            {
                Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Skipping.");
                return false;
            }

            if (jQuest.TryGetProperty("blocks", out JsonElement jBlocksV1))
            {
                QuestProcess questProcess = new QuestProcess(0, assetData.QuestScheduleId);
                if (!ParseBlocks(questProcess, jBlocksV1))
                {
                    Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Skipping.");
                    return false;
                }
                assetData.Processes.Add(questProcess);
            }
            else if (jQuest.TryGetProperty("processes", out JsonElement jProcesses))
            {
                ushort ProcessNo = 0;
                foreach (var jProcess in jProcesses.EnumerateArray())
                {
                    QuestProcess questProcess = new QuestProcess(ProcessNo, assetData.QuestScheduleId);

                    var jBlocks = jProcess.GetProperty("blocks");
                    if (!ParseBlocks(questProcess, jBlocks))
                    {
                        Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Skipping.");
                        return false;
                    }
                    assetData.Processes.Add(questProcess);
                    ProcessNo += 1;
                }
            }

            return true;
        }

        private bool ParseOrderCondition(QuestAssetData assetData, JsonElement quest)
        {
            if (!quest.TryGetProperty("order_conditions", out JsonElement jOrderConditions))
            {
                return true;
            }

            foreach (var condition in jOrderConditions.EnumerateArray())
            {
                if (!Enum.TryParse(condition.GetProperty("type").GetString(), true, out QuestOrderConditionType orderConditionType))
                {
                    Logger.Error($"Unable to parse order condition type for '{assetData.QuestId}'. Skipping.");
                    return false;
                }

                QuestOrderCondition questOrderCondition = new QuestOrderCondition()
                {
                    Type = orderConditionType
                };

                if (condition.TryGetProperty("Param1", out JsonElement jParam1))
                {
                    questOrderCondition.Param01 = jParam1.GetInt32();
                }

                if (condition.TryGetProperty("Param2", out JsonElement jParam2))
                {
                    questOrderCondition.Param02 = jParam2.GetInt32();
                }

                assetData.OrderConditions.Add(questOrderCondition);
            }

            return true;
        }

        private void ParseRewards(QuestAssetData assetData, JsonElement quest)
        {
            uint randomRewards = 0;
            foreach (var reward in quest.GetProperty("rewards").EnumerateArray())
            {
                var rewardType = reward.GetProperty("type").GetString();
                var rewardBucket = ParseRewardBucket(reward, rewardType);
                var normalizedRewardType = NormalizeRewardType(rewardType);
                var forceInstanced = rewardType.StartsWith("instanced_");

                switch (normalizedRewardType)
                {
                    case "fixed":
                        AddItemRewardToBucket(assetData, rewardBucket, ParseFixedRewardItem(reward, forceInstanced));
                        break;
                    case "random":
                    {
                        if (rewardBucket == QuestRewardBucket.Normal && randomRewards >= 4)
                        {
                            Logger.Error("Client only supports a maximum of 4 random rewards per quest. Skipping.");
                            continue;
                        }

                        if (rewardBucket == QuestRewardBucket.Normal)
                        {
                            randomRewards += 1;
                        }

                        AddItemRewardToBucket(assetData, rewardBucket, ParseRandomRewardItem(reward, forceInstanced));
                        break;
                    }
                    case "select":
                        AddItemRewardToBucket(assetData, rewardBucket, ParseSelectRewardItem(reward, forceInstanced));
                        break;
                    case "exp":
                        AddPointRewardToBucket(assetData, rewardBucket, new QuestPointReward()
                        {
                            PointType = PointType.ExperiencePoints,
                            Amount = reward.GetProperty("amount").GetUInt32()
                        });
                        break;
                    case "pp":
                        AddPointRewardToBucket(assetData, rewardBucket, new QuestPointReward()
                        {
                            PointType = PointType.PlayPoints,
                            Amount = reward.GetProperty("amount").GetUInt32()
                        });
                        break;
                    case "jp":
                        AddPointRewardToBucket(assetData, rewardBucket, new QuestPointReward()
                        {
                            PointType = PointType.JobPoints,
                            Amount = reward.GetProperty("amount").GetUInt32()
                        });
                        break;
                    case "ap":
                        AddPointRewardToBucket(assetData, rewardBucket, new QuestPointReward()
                        {
                            PointType = PointType.AreaPoints,
                            Amount = reward.GetProperty("amount").GetUInt32()
                        });
                        if (rewardBucket == QuestRewardBucket.Normal)
                        {
                            assetData.LightQuestDetail.GetAp = reward.GetProperty("amount").GetUInt32();
                        }
                        break;
                    case "wallet":
                        if (!Enum.TryParse(reward.GetProperty("wallet_type").GetString(), true, out WalletType walletType))
                        {
                            continue;
                        }
                        AddWalletRewardToBucket(assetData, rewardBucket, new QuestWalletReward()
                        {
                            WalletType = walletType,
                            Amount = reward.GetProperty("amount").GetUInt32()
                        });
                        break;
                    default:
                        /* NOT IMPLEMENTED */
                        break;
                }
            }
        }

        private static string NormalizeRewardType(string rewardType)
        {
            return rewardType switch
            {
                "repeat" => "random",
                "instanced_repeat" => "random",
                "first_clear" => "fixed",
                "instanced_first_clear" => "fixed",
                "period_first_clear" => "fixed",
                "instanced_period_first_clear" => "fixed",
                "helper" => "fixed",
                "instanced_helper" => "fixed",
                "instanced_fixed" => "fixed",
                "instanced_select" => "select",
                _ => rewardType,
            };
        }

        private static QuestRewardBucket ParseRewardBucket(JsonElement reward, string rewardType)
        {
            if (reward.TryGetProperty("bucket", out var bucket))
            {
                return bucket.GetString() switch
                {
                    "normal" => QuestRewardBucket.Normal,
                    "repeat" => QuestRewardBucket.RepeatClear,
                    "first_clear" => QuestRewardBucket.FirstClear,
                    "period_first_clear" => QuestRewardBucket.PeriodFirstClear,
                    "helper" => QuestRewardBucket.Helper,
                    var value => throw new JsonException($"Unknown quest reward bucket '{value}'."),
                };
            }

            return rewardType switch
            {
                "repeat" => QuestRewardBucket.RepeatClear,
                "instanced_repeat" => QuestRewardBucket.RepeatClear,
                "first_clear" => QuestRewardBucket.FirstClear,
                "instanced_first_clear" => QuestRewardBucket.FirstClear,
                "period_first_clear" => QuestRewardBucket.PeriodFirstClear,
                "instanced_period_first_clear" => QuestRewardBucket.PeriodFirstClear,
                "helper" => QuestRewardBucket.Helper,
                "instanced_helper" => QuestRewardBucket.Helper,
                _ => QuestRewardBucket.Normal,
            };
        }

        private static void AddItemRewardToBucket(QuestAssetData assetData, QuestRewardBucket bucket, QuestRewardItem rewardItem)
        {
            switch (bucket)
            {
                case QuestRewardBucket.Normal:
                    assetData.RewardItems.Add(rewardItem);
                    break;
                case QuestRewardBucket.RepeatClear:
                    assetData.RepeatClearRewardItems.Add(rewardItem);
                    break;
                case QuestRewardBucket.FirstClear:
                    assetData.FirstClearRewardItems.Add(rewardItem);
                    break;
                case QuestRewardBucket.PeriodFirstClear:
                    assetData.PeriodFirstClearRewardItems.Add(rewardItem);
                    break;
                case QuestRewardBucket.Helper:
                    assetData.HelperRewardItems.Add(rewardItem);
                    break;
            }
        }

        private static void AddPointRewardToBucket(QuestAssetData assetData, QuestRewardBucket bucket, QuestPointReward reward)
        {
            switch (bucket)
            {
                case QuestRewardBucket.Normal:
                    assetData.PointRewards.Add(reward);
                    break;
                case QuestRewardBucket.RepeatClear:
                    assetData.RepeatClearPointRewards.Add(reward);
                    break;
                case QuestRewardBucket.FirstClear:
                    assetData.FirstClearPointRewards.Add(reward);
                    break;
                case QuestRewardBucket.PeriodFirstClear:
                    assetData.PeriodFirstClearPointRewards.Add(reward);
                    break;
                case QuestRewardBucket.Helper:
                    assetData.HelperPointRewards.Add(reward);
                    break;
            }
        }

        private static void AddWalletRewardToBucket(QuestAssetData assetData, QuestRewardBucket bucket, QuestWalletReward reward)
        {
            switch (bucket)
            {
                case QuestRewardBucket.Normal:
                    assetData.RewardCurrency.Add(reward);
                    break;
                case QuestRewardBucket.RepeatClear:
                    assetData.RepeatClearRewardCurrency.Add(reward);
                    break;
                case QuestRewardBucket.FirstClear:
                    assetData.FirstClearRewardCurrency.Add(reward);
                    break;
                case QuestRewardBucket.PeriodFirstClear:
                    assetData.PeriodFirstClearRewardCurrency.Add(reward);
                    break;
                case QuestRewardBucket.Helper:
                    assetData.HelperRewardCurrency.Add(reward);
                    break;
            }
        }

        private QuestRewardItem ParseFixedRewardItem(JsonElement reward, bool forceInstanced = false)
        {
            if (forceInstanced || HasInstancedLootPoolItems(reward))
            {
                return ParseInstancedFixedRewardItem(reward);
            }

            var rewardItem = new QuestFixedRewardItem();
            foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
            {
                rewardItem.LootPool.Add(new FixedLootPoolItem()
                {
                    ItemId = AssetCommonDeserializer.ParseItemId(item.GetProperty("item_id")),
                    Num = item.GetProperty("num").GetUInt16(),
                });
            }
            return rewardItem;
        }

        private QuestRewardItem ParseRandomRewardItem(JsonElement reward, bool forceInstanced = false)
        {
            var hasInstancedItems = forceInstanced || HasInstancedLootPoolItems(reward);
            var hasChanceItems = HasChanceLootPoolItems(reward);
            QuestRandomRewardItem rewardItem;

            if (hasInstancedItems)
            {
                rewardItem = hasChanceItems
                    ? new QuestInstancedRandomChanceRewardItem()
                    : new QuestInstancedRandomFixedRewardItem();

                foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
                {
                    var poolItem = ParseInstancedLootPoolItem(item, hasChanceItems);
                    rewardItem.LootPool.Add(poolItem);
                }

                return rewardItem;
            }

            if (hasChanceItems)
            {
                rewardItem = new QuestRandomChanceRewardItem();
                foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
                {
                    rewardItem.LootPool.Add(new ChanceLootPoolItem()
                    {
                        ItemId = AssetCommonDeserializer.ParseItemId(item.GetProperty("item_id")),
                        Num = item.GetProperty("num").GetUInt16(),
                        Chance = item.TryGetProperty("chance", out var chance) ? chance.GetDouble() : 0.0
                    });
                }
                return rewardItem;
            }

            rewardItem = new QuestRandomFixedRewardItem();
            foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
            {
                rewardItem.LootPool.Add(new FixedLootPoolItem()
                {
                    ItemId = AssetCommonDeserializer.ParseItemId(item.GetProperty("item_id")),
                    Num = item.GetProperty("num").GetUInt16(),
                });
            }
            return rewardItem;
        }

        private QuestRewardItem ParseSelectRewardItem(JsonElement reward, bool forceInstanced = false)
        {
            if (forceInstanced || HasInstancedLootPoolItems(reward))
            {
                return ParseInstancedSelectRewardItem(reward);
            }

            var rewardItem = new QuestSelectRewardItem();
            foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
            {
                rewardItem.LootPool.Add(new SelectLootPoolItem()
                {
                    ItemId = AssetCommonDeserializer.ParseItemId(item.GetProperty("item_id")),
                    Num = item.GetProperty("num").GetUInt16(),
                });
            }
            return rewardItem;
        }

        private QuestInstancedFixedRewardItem ParseInstancedFixedRewardItem(JsonElement reward)
        {
            var rewardItem = new QuestInstancedFixedRewardItem();
            foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
            {
                rewardItem.LootPool.Add(ParseInstancedLootPoolItem(item));
            }
            return rewardItem;
        }

        private QuestInstancedSelectRewardItem ParseInstancedSelectRewardItem(JsonElement reward)
        {
            var rewardItem = new QuestInstancedSelectRewardItem();
            foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
            {
                rewardItem.LootPool.Add(ParseInstancedLootPoolItem(item));
            }
            return rewardItem;
        }

        private static bool HasInstancedLootPoolItems(JsonElement reward)
        {
            return reward.GetProperty("loot_pool").EnumerateArray().Any(HasInstanceData);
        }

        private static bool HasChanceLootPoolItems(JsonElement reward)
        {
            return reward.GetProperty("loot_pool").EnumerateArray().Any(item => item.TryGetProperty("chance", out _));
        }

        private static bool HasInstanceData(JsonElement item)
        {
            return item.TryGetProperty("instance", out _)
                || item.TryGetProperty("color", out _)
                || item.TryGetProperty("plus_value", out _)
                || item.TryGetProperty("safety_setting", out _)
                || item.TryGetProperty("crests", out _);
        }

        private InstancedLootPoolItem ParseInstancedLootPoolItem(JsonElement item, bool parseChance = false)
        {
            InstancedLootPoolItem poolItem = parseChance
                ? new InstancedChanceLootPoolItem()
                : new InstancedLootPoolItem();

            poolItem.ItemId = AssetCommonDeserializer.ParseItemId(item.GetProperty("item_id"));
            poolItem.Num = item.GetProperty("num").GetUInt16();

            var instance = item;
            if (item.TryGetProperty("instance", out var instanceProp))
            {
                instance = instanceProp;
            }

            if (instance.TryGetProperty("color", out var colorProp)) poolItem.Color = colorProp.GetUInt32();
            if (instance.TryGetProperty("plus_value", out var plusProp)) poolItem.PlusValue = plusProp.GetUInt32();
            if (instance.TryGetProperty("safety_setting", out var safetyProp)) poolItem.SafetySetting = safetyProp.GetUInt32();
            if (parseChance && item.TryGetProperty("chance", out var chanceProp))
            {
                ((InstancedChanceLootPoolItem)poolItem).Chance = chanceProp.GetDouble();
            }

            if (instance.TryGetProperty("crests", out var crestsProp))
            {
                foreach (var crest in crestsProp.EnumerateArray())
                {
                    poolItem.Crests.Add(new StagedRewardItemCrest
                    {
                        Slot = crest.GetProperty("slot").GetUInt32(),
                        CrestId = crest.GetProperty("crest_id").GetUInt32(),
                        Level = crest.TryGetProperty("level", out var lvl) ? lvl.GetUInt32() : 0u,
                    });
                }
            }

            return poolItem;
        }

        private bool ParseBlocks(QuestProcess questProcess, JsonElement jBlocks)
        {
            ushort blockIndex = 1;
            foreach (var jblock in jBlocks.EnumerateArray())
            {
                QuestBlock questBlock = new QuestBlock();

                if (!Enum.TryParse(jblock.GetProperty("type").GetString(), true, out QuestBlockType questBlockType))
                {
                    var blockTypeName = jblock.GetProperty("type").GetString();
                    Logger.Error($"Unable to parse the quest block type '{blockTypeName}' of {questProcess.ProcessNo}.0.{blockIndex}.");
                    return false;
                }

                questBlock.QuestScheduleId = questProcess.QuestScheduleId;
                questBlock.ProcessNo = questProcess.ProcessNo;
                questBlock.BlockType = questBlockType;
                questBlock.BlockNo = blockIndex;
                questBlock.AnnounceType = QuestAnnounceType.None;

                questBlock.ShouldStageJump = false;
                if (jblock.TryGetProperty("stage_jump", out JsonElement jStageJump))
                {
                    questBlock.ShouldStageJump = jStageJump.GetBoolean();
                }

                if (jblock.TryGetProperty("announce_type", out JsonElement jUpdateAnnounce))
                {
                    if (!Enum.TryParse(jUpdateAnnounce.GetString(), true, out QuestAnnounceType announceType))
                    {
                        Logger.Error($"Unable to parse the quest announce type of BlockNo={blockIndex}.");
                        return false;
                    }

                    // Handles special pseudo announce types
                    QuestBlock.EvaluateAnnounceType(questBlock, announceType);
                }

                if (jblock.TryGetProperty("checkpoint", out JsonElement jCheckpoint))
                {
                    questBlock.IsCheckpoint = jCheckpoint.GetBoolean();
                }

                ParseAnnoucementSubtypes(questBlock, jblock);

                if (jblock.TryGetProperty("stage_id", out JsonElement jStageId))
                {
                    questBlock.StageLayoutId = AssetCommonDeserializer.ParseStageId(jStageId);
                }

                questBlock.SubGroupId = 0;
                if (jblock.TryGetProperty("subgroup_id", out JsonElement jSubGroupId))
                {
                    questBlock.SubGroupId = jSubGroupId.GetUInt16();
                }

                if (jblock.TryGetProperty("hand_items", out JsonElement jHandItems))
                {
                    foreach (var item in jHandItems.EnumerateArray())
                    {
                        questBlock.HandPlayerItems.Add(new QuestItem()
                        {
                            ItemId = (ItemId)item.GetProperty("id").GetUInt32(),
                            Amount = item.GetProperty("amount").GetUInt32()
                        });
                    }
                }

                if (jblock.TryGetProperty("consume_items", out JsonElement jConsumeItems))
                {
                    foreach (var item in jConsumeItems.EnumerateArray())
                    {
                        questBlock.ConsumePlayerItems.Add(new QuestItem()
                        {
                            ItemId = (ItemId)item.GetProperty("id").GetUInt32(),
                            Amount = item.GetProperty("amount").GetUInt32()
                        });
                    }
                }

                questBlock.BgmStop = false;
                if (jblock.TryGetProperty("bgm_stop", out JsonElement jBgmStop))
                {
                    questBlock.BgmStop = jBgmStop.GetBoolean();
                }

                if (jblock.TryGetProperty("flags", out JsonElement jFlags))
                {
                    // {"type": "MyQst", "operation": "Set", "value": 4}
                    foreach (var jFlag in jFlags.EnumerateArray())
                    {
                        var questFlag = ParseQuestFlag(jFlag);
                        if (questFlag == null)
                        {
                            Logger.Error($"Unable to parse the quest flags of BlockNo={blockIndex}.");
                            return false;
                        }
                        questBlock.QuestFlags.Add(questFlag);
                    }
                }

                if (jblock.TryGetProperty("checkpoint_flags", out JsonElement jCheckpointFlags))
                {
                    // {"type": "MyQst", "operation": "Set", "value": 4}
                    foreach (var jFlag in jCheckpointFlags.EnumerateArray())
                    {
                        var questFlag = ParseQuestFlag(jFlag);
                        if (questFlag == null)
                        {
                            Logger.Error($"Unable to parse the checkpoint quest flags of BlockNo={blockIndex}.");
                            return false;
                        }
                        questBlock.CheckpointQuestFlags.Add(questFlag);
                    }
                }

                questBlock.QuestCameraEvent.HasCameraEvent = false;
                if (jblock.TryGetProperty("camera_event", out JsonElement jCameraEvent))
                {
                    questBlock.QuestCameraEvent.HasCameraEvent = true;
                    questBlock.QuestCameraEvent.EventNo = jCameraEvent.GetProperty("event_no").GetInt32();
                }

                questBlock.ShowMarker = true;
                if (jblock.TryGetProperty("show_marker", out JsonElement jShowMarker))
                {
                    questBlock.ShowMarker = jShowMarker.GetBoolean();
                }

                if (jblock.TryGetProperty("contents_release", out JsonElement jContentsReleased))
                {
                    if (!ParseContentsRelease(questBlock.ContentsReleased, jContentsReleased, questBlock))
                    {
                        return false;
                    }
                }

                switch (questBlockType)
                {
                    case QuestBlockType.IsStageNo:
                        break;
                    case QuestBlockType.NpcTalkAndOrder:
                    case QuestBlockType.NewNpcTalkAndOrder:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block of BlockNo={blockIndex}.");
                                return false;
                            }
                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                MsgId = jblock.GetProperty("message_id").GetInt32(),
                                StageId = questBlock.StageLayoutId
                            });
                        }
                        break;
                    case QuestBlockType.PartyGather:
                        {
                            var jLocation = jblock.GetProperty("location");
                            questBlock.PartyGatherPoint.x = jLocation.GetProperty("x").GetInt32();
                            questBlock.PartyGatherPoint.y = jLocation.GetProperty("y").GetInt32();
                            questBlock.PartyGatherPoint.z = jLocation.GetProperty("z").GetInt32();
                        }
                        break;
                    case QuestBlockType.IsGatherPartyInStage:
                        break;
                    case QuestBlockType.DiscoverEnemy:
                    case QuestBlockType.SeekOutEnemiesAtMarkedLocation:
                    case QuestBlockType.KillGroup:
                    case QuestBlockType.SpawnGroup:
                    case QuestBlockType.WeakenGroup:
                    case QuestBlockType.DestroyGroup:
                        {
                            questBlock.ResetGroup = true;
                            if (jblock.TryGetProperty("reset_group", out JsonElement jResetGroup))
                            {
                                questBlock.ResetGroup = jResetGroup.GetBoolean();
                            }

                            if (jblock.TryGetProperty("percent", out JsonElement jPercent))
                            {
                                questBlock.EnemyHpPrecent = jPercent.GetInt32();
                            }

                            foreach (var groupId in jblock.GetProperty("groups").EnumerateArray())
                            {
                                questBlock.EnemyGroupIds.Add(groupId.GetUInt32());
                            }
                        }
                        break;
                    case QuestBlockType.TalkToNpc:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block of BlockNo={blockIndex}.");
                                return false;
                            }

                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                MsgId = jblock.GetProperty("message_id").GetInt32(),
                                StageId = AssetCommonDeserializer.ParseStageId(jblock.GetProperty("stage_id"))
                            });
                        }
                        break;
                    case QuestBlockType.NewTalkToNpc:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block of BlockNo={blockIndex}.");
                                return false;
                            }

                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                MsgId = jblock.GetProperty("message_id").GetInt32(),
                                StageId = AssetCommonDeserializer.ParseStageId(jblock.GetProperty("stage_id"))
                            });

                            questBlock.NpcOrderDetails[0].QuestId = QuestId.None;
                            if (jblock.TryGetProperty("quest_id", out JsonElement jOrderQuestId))
                            {
                                questBlock.NpcOrderDetails[0].QuestId = (QuestId)jOrderQuestId.GetUInt32();
                            }
                        }
                        break;
                    case QuestBlockType.TouchNpc:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block @ index {blockIndex - 1}.");
                                return false;
                            }

                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                StageId = AssetCommonDeserializer.ParseStageId(jblock.GetProperty("stage_id"))
                            });
                        }
                        break;
                    case QuestBlockType.IsQuestOrdered:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("quest_type").GetString(), true, out QuestType questType))
                            {
                                Logger.Error($"Unable to parse the quest type in block of BlockNo={blockIndex}.");
                                return false;
                            }

                            questBlock.QuestOrderDetails.QuestType = questType;

                            questBlock.QuestOrderDetails.QuestId = QuestId.None;
                            if (jblock.TryGetProperty("quest_id", out JsonElement jOrderQuestId))
                            {
                                questBlock.QuestOrderDetails.QuestId = (QuestId)jOrderQuestId.GetUInt32();
                            }
                        }
                        break;
                    case QuestBlockType.MyQstFlags:
                        {
                            if (jblock.TryGetProperty("set_flags", out JsonElement jSetFlags))
                            {
                                foreach (var jMyQstFlag in jSetFlags.EnumerateArray())
                                {
                                    questBlock.MyQstSetFlags.Add(jMyQstFlag.GetUInt32());
                                }
                            }

                            if (jblock.TryGetProperty("check_flags", out JsonElement jCheckFlags))
                            {
                                foreach (var jMyQstFlag in jCheckFlags.EnumerateArray())
                                {
                                    questBlock.MyQstCheckFlags.Add(jMyQstFlag.GetUInt32());
                                }
                            }
                        }
                        break;
                    case QuestBlockType.CollectItem:
                        break;
                    case QuestBlockType.OmInteractEvent:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("quest_type").GetString(), true, out OmQuestType questType))
                            {
                                Logger.Error($"Unable to parse the quest type in block of BlockNo={blockIndex}.");
                                return false;
                            }
                            questBlock.OmInteractEvent.QuestType = questType;

                            if (!Enum.TryParse(jblock.GetProperty("interact_type").GetString(), true, out OmInteractType interactType))
                            {
                                Logger.Error($"Unable to parse the quest type in block of BlockNo={blockIndex}.");
                                return false;
                            }
                            questBlock.OmInteractEvent.InteractType = interactType;

                            if (jblock.TryGetProperty("quest_id", out JsonElement jQuestId))
                            {
                                questBlock.OmInteractEvent.QuestId = (QuestId)jQuestId.GetUInt32();
                            }
                        }
                        break;
                    case QuestBlockType.DeliverItems:
                    case QuestBlockType.NewDeliverItems:
                    {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block of BlockNo={blockIndex}.");
                                return false;
                            }

                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                MsgId = jblock.GetProperty("message_id").GetInt32(),
                                StageId = AssetCommonDeserializer.ParseStageId(jblock.GetProperty("stage_id"))
                            });

                            foreach (var item in jblock.GetProperty("items").EnumerateArray())
                            {
                                questBlock.DeliveryRequests.Add(new QuestItem()
                                {
                                    ItemId = (ItemId)item.GetProperty("id").GetUInt32(),
                                    Amount = item.GetProperty("amount").GetUInt32()
                                });
                            }
                        }
                        break;
                    case QuestBlockType.ExtendTime:
                        {
                            questBlock.TimeAmount = jblock.GetProperty("amount").GetUInt32();
                        }
                        break;
                    case QuestBlockType.PlayEvent:
                        {
                            questBlock.QuestEvent.EventId = jblock.GetProperty("event_id").GetInt32();

                            questBlock.QuestEvent.JumpType = QuestJumpType.After;
                            if (jblock.TryGetProperty("jump_type", out JsonElement jJumpType))
                            {
                                if (!Enum.TryParse(jJumpType.GetString(), true, out QuestJumpType jumpType))
                                {
                                    Logger.Error($"Unable to parse the event jump type in block of BlockNo={blockIndex}.");
                                    return false;
                                }
                                questBlock.QuestEvent.JumpType = jumpType;
                            }

                            if (jblock.TryGetProperty("jump_stage_id", out JsonElement jStageJumpId))
                            {
                                questBlock.QuestEvent.JumpStageId = AssetCommonDeserializer.ParseStageId(jStageJumpId);
                            }

                            if (jblock.TryGetProperty("start_pos_no", out JsonElement jStartPosNo))
                            {
                                questBlock.QuestEvent.StartPosNo = jStartPosNo.GetInt32();
                            }
                        }
                        break;
                    case QuestBlockType.KillTargetEnemies:
                        {
                            // The KillTargetEnemies/EmDieLight machinery expects to be given enemy name IDs, not raw enemy IDs.
                            // Quest writers can provide either; if they give a hex number it will be converted to the proper enemy name ID automatically. 
                            var enemyIdString = jblock.GetProperty("enemy_id").GetString();
                            if (enemyIdString.Contains('x'))
                            {
                                var enemyId = (EnemyId)Convert.ToUInt32(enemyIdString, 16);
                                questBlock.TargetEnemy.EnemyId = enemyId.GetUIId();
                            }
                            else
                            {
                                questBlock.TargetEnemy.EnemyId = (EnemyUIId)Convert.ToUInt32(enemyIdString);
                            }

                            questBlock.TargetEnemy.Level = jblock.GetProperty("level").GetUInt32();
                            questBlock.TargetEnemy.Amount = jblock.GetProperty("amount").GetUInt32();
                        }
                        break;
                    case QuestBlockType.ReturnCheckpoint:
                        {
                            questBlock.CheckpointDetails.ProcessNo = jblock.GetProperty("process_no").GetUInt16();
                            questBlock.CheckpointDetails.BlockNo = jblock.GetProperty("block_no").GetUInt16();
                        }
                        break;
                    case QuestBlockType.DeliverItemsLight:
                        {
                            foreach (var item in jblock.GetProperty("items").EnumerateArray())
                            {
                                questBlock.DeliveryRequests.Add(new QuestItem()
                                {
                                    ItemId = (ItemId)item.GetProperty("id").GetUInt32(),
                                    Amount = item.GetProperty("amount").GetUInt32()
                                });
                            }
                        }
                        break;
                    case QuestBlockType.SceHitIn:
                    case QuestBlockType.SceHitOut:
                        {
                            // Uses marker visibilty from the common field
                            questBlock.SceNo = jblock.GetProperty("sce_no").GetUInt32();
                            questBlock.StageLayoutId = AssetCommonDeserializer.ParseStageId(jblock.GetProperty("stage_id"));
                        }
                        break;
                    case QuestBlockType.Raw:
                        break;
                    case QuestBlockType.DummyBlock:
                        /* Filler block which might do some meta things like announce or set/check flags */
                        break;
                    default:
                        Logger.Error($"Unsupported QuestBlockType {questBlockType} @ index {blockIndex}.");
                        return false;
                }

                if (!ParseRawBlock(jblock, questBlock))
                {
                    Logger.Error($"Unable to parse RawBlock commands in block @ index {blockIndex}.");
                    return false;
                }

                questProcess.Blocks[blockIndex] = questBlock;

                blockIndex += 1;
            }

            // Add an implicit EndBlock
            questProcess.Blocks[blockIndex] = new QuestBlock()
            {
                BlockType = (questProcess.ProcessNo == 0) ? QuestBlockType.End : QuestBlockType.None,
                ProcessNo = questProcess.ProcessNo,
                BlockNo = blockIndex,
                SequenceNo = 1,
                AnnounceType = QuestAnnounceType.None
            };

            return true;
        }

        private void ParseAnnoucementSubtypes(QuestBlock questBlock, JsonElement jBlock)
        {
            var announcements = questBlock.Announcements;

            announcements.GeneralAnnounceId = 0;
            if (jBlock.TryGetProperty("general_announce", out JsonElement jGeneralAnnounce))
            {
                announcements.GeneralAnnounceId = jGeneralAnnounce.GetInt32();
            }

            announcements.StageStart = 0;
            if (jBlock.TryGetProperty("stage_start", out JsonElement jStageStart))
            {
                announcements.StageStart = jStageStart.GetInt32();
            }

            announcements.StageClear = 0;
            if (jBlock.TryGetProperty("stage_clear", out JsonElement jStageClear))
            {
                announcements.StageClear = jStageClear.GetInt32();
            }

            announcements.EndContentsPurpose = 0;
            if (jBlock.TryGetProperty("end_contents_purpose", out JsonElement jEndContentsPurpose))
            {
                announcements.EndContentsPurpose = jEndContentsPurpose.GetInt32();
            }

            if (jBlock.TryGetProperty("caution", out JsonElement jCaution))
            {
                announcements.Caution = jCaution.GetBoolean();
        }
        }

        private QuestFlag ParseQuestFlag(JsonElement jFlag)
        {
            QuestFlag questFlag = new QuestFlag();

            if (!Enum.TryParse(jFlag.GetProperty("type").GetString(), true, out QuestFlagType type))
            {
                Logger.Error($"Invalid QuestFlagType");
                return null;
            }

            if (!Enum.TryParse(jFlag.GetProperty("action").GetString(), true, out QuestFlagAction action))
            {
                Logger.Error($"Invalid QuestFlagAction");
                return null;
            }

            if (jFlag.TryGetProperty("quest_id", out JsonElement jQuestId))
            {
                questFlag.QuestId = jQuestId.GetInt32();
            }

            questFlag.Type = type;
            questFlag.Action = action;
            questFlag.Value = jFlag.GetProperty("value").GetInt32();

            return questFlag;
        }

        private bool ParseMissionParams(QuestAssetData assetData, JsonElement jMissionParams)
        {

            if (!jMissionParams.TryGetProperty("group", out JsonElement jGroup))
            {
                Logger.Error($"Missing required member 'group' from Extreme Mission config.");
                return false;
            }
            assetData.MissionParams.Group = jGroup.GetUInt32();

            if (!jMissionParams.TryGetProperty("phase_groups", out JsonElement jPhaseGroups))
            {
                Logger.Error($"Missing required member 'phase_groups' from Extreme Mission config.");
                return false;
            }

            foreach (var element in jPhaseGroups.EnumerateArray())
            {
                assetData.MissionParams.QuestPhaseGroupIdList.Add(new CDataCommonU32() { Value = element.GetUInt32() });
            }

            assetData.MissionParams.StartPos = 0;
            if (jMissionParams.TryGetProperty("start_pos", out JsonElement jStartPos))
            {
                assetData.MissionParams.StartPos = jStartPos.GetByte();
            }

            assetData.MissionParams.MinimumMembers = 4;
            if (jMissionParams.TryGetProperty("minimum_members", out JsonElement jMinimumMembers))
            {
                assetData.MissionParams.MinimumMembers = jMinimumMembers.GetUInt32();
            }

            assetData.MissionParams.MaximumMembers = 4;
            if (jMissionParams.TryGetProperty("maximum_members", out JsonElement jMaximumMembers))
            {
                assetData.MissionParams.MaximumMembers = jMaximumMembers.GetUInt32();
            }

            assetData.MissionParams.LootDistribution = QuestLootDistribution.Normal;
            if (jMissionParams.TryGetProperty("loot_distribution", out JsonElement jLootDistribution))
            {
                if (!Enum.TryParse(jLootDistribution.GetString(), true, out QuestLootDistribution lootDistribution))
                {
                    Logger.Error("Invalid 'loot_distribution' from ExtremeMission config.");
                    return false;
                }
                assetData.MissionParams.LootDistribution = lootDistribution;
            }

            assetData.MissionParams.PlaytimeInSeconds = 1200;
            if (jMissionParams.TryGetProperty("playtime", out JsonElement jPlaytime))
            {
                assetData.MissionParams.PlaytimeInSeconds = jPlaytime.GetUInt32();
            }

            assetData.MissionParams.IsSolo = false;
            if (jMissionParams.TryGetProperty("solo_only", out JsonElement jIsSoloOnly))
            {
                assetData.MissionParams.IsSolo = jIsSoloOnly.GetBoolean();
            }

            assetData.MissionParams.MaxPawns = 3;
            if (jMissionParams.TryGetProperty("max_pawns", out JsonElement jMaxPawns))
            {
                assetData.MissionParams.MaxPawns = jMaxPawns.GetUInt32();
            }

            assetData.MissionParams.ArmorAllowed = true;
            assetData.MissionParams.JewelryAllowed = true;
            if (jMissionParams.TryGetProperty("restrictions", out JsonElement jRestrictions))
            {
                if (jMissionParams.TryGetProperty("armor", out JsonElement jArmorAllowed))
                {
                    assetData.MissionParams.ArmorAllowed = jArmorAllowed.GetBoolean();
                }

                if (jMissionParams.TryGetProperty("jewelry", out JsonElement jJewelryAllowed))
                {
                    assetData.MissionParams.JewelryAllowed = jJewelryAllowed.GetBoolean();
                }
            }

            return true;
        }

        private bool ParseServerActions(QuestAssetData assetData, JsonElement quest)
        {
            if (!quest.TryGetProperty("server_actions", out JsonElement jServerActions))
            {
                // It is optional to provide this list
                return true;
            }

            foreach (var jServerAction in jServerActions.EnumerateArray())
            {
                var action = new QuestServerAction();

                if (!jServerAction.TryGetProperty("action_type", out JsonElement jActionType))
                {
                    Logger.Error("Unable to find the server action type. Exiting.");
                    return false;
                }

                if (!Enum.TryParse(jActionType.GetString(), true, out QuestSeverActionType actionType))
                {
                    Logger.Error("Unable to decode the server action type. Exiting.");
                    return false;
                }

                action.ActionType = actionType;
                if (actionType == QuestSeverActionType.OmSetInstantValue)
                {
                    if (!jServerAction.TryGetProperty("instant_value_action", out JsonElement jInstantValueAction))
                    {
                        Logger.Error("Failed to locate the instant_value_action field. Exiting.");
                        return false;
                    }

                    if (!Enum.TryParse(jInstantValueAction.ToString(), true, out OmInstantValueAction instantValueAction))
                    {
                        Logger.Error("Failed to decode the instant_value_action field. Exiting.");
                    }
                    action.OmInstantValueAction = instantValueAction;
                    action.Key = jServerAction.GetProperty("key").GetUInt64();
                    action.Value = jServerAction.GetProperty("value").GetUInt32();
                    action.StageLayoutId = AssetCommonDeserializer.ParseStageId(jServerAction.GetProperty("stage_id"));
                }

                assetData.ServerActions.Add(action);
            }

            return true;
        }

        private bool ParseRawBlock(JsonElement jBlock, QuestBlock questBlock)
        {
            if (jBlock.TryGetProperty("check_commands", out JsonElement jCheckCommands))
            {
                var jCheckCommandList = jCheckCommands.EnumerateArray().ToList();
                if (jCheckCommandList.Count > 0)
                {
                    if (jCheckCommands[0].ValueKind == JsonValueKind.Array)
                    {
                        // New way which supports OR conditions
                        foreach (var jCheckGroup in jCheckCommandList)
                        {
                            List<CDataQuestCommand> checkCommands = new List<CDataQuestCommand>();
                            foreach (var jCheckCommand in jCheckGroup.EnumerateArray())
                            {
                                CDataQuestCommand command = new CDataQuestCommand();
                                if (!Enum.TryParse(jCheckCommand.GetProperty("type").GetString(), true, out QuestCommandCheckType type))
                                {
                                    return false;
                                }

                                command.Command = (ushort)type;
                                ParseCommandParams(jCheckCommand, command);

                                checkCommands.Add(command);
                            }

                            if (checkCommands.Count > 0)
                            {
                                questBlock.CheckCommands.Add(checkCommands);
                            }
                        }
                    }
                    else
                    {
                        // Legacy Way
                        List<CDataQuestCommand> checkCommands = new List<CDataQuestCommand>();
                        foreach (var jCheckCommand in jCheckCommandList)
                        {
                            CDataQuestCommand command = new CDataQuestCommand();
                            if (!Enum.TryParse(jCheckCommand.GetProperty("type").GetString(), true, out QuestCommandCheckType type))
                            {
                                return false;
                            }

                            command.Command = (ushort)type;
                            ParseCommandParams(jCheckCommand, command);

                            checkCommands.Add(command);
                        }

                        if (checkCommands.Count > 0)
                        {
                            questBlock.CheckCommands.Add(checkCommands);
                        }
                    }
                }
            }

            if (jBlock.TryGetProperty("result_commands", out JsonElement jResultCommands))
            {
                foreach (var jResultCommand in jResultCommands.EnumerateArray())
                {
                    CDataQuestCommand command = new CDataQuestCommand();
                    if (!Enum.TryParse(jResultCommand.GetProperty("type").GetString(), true, out QuestResultCommand type))
                    {
                        return false;
                    }

                    command.Command = (ushort)type;
                    ParseCommandParams(jResultCommand, command);

                    questBlock.ResultCommands.Add(command);
                }
            }

            return true;
        }

        private void ParseCommandParams(JsonElement jCommand, CDataQuestCommand command)
        {
            List<string> commandParams = new List<string>() { "Param1", "Param2", "Param3", "Param4" };
            for (int i = 0; i < commandParams.Count; i++)
            {
                int paramValue = 0;
                if (jCommand.TryGetProperty(commandParams[i], out JsonElement jParam))
                {
                    paramValue = jParam.GetInt32();
                }

                switch (i)
                {
                    case 0:
                        command.Param01 = paramValue;
                        break;
                    case 1:
                        command.Param02 = paramValue;
                        break;
                    case 2:
                        command.Param03 = paramValue;
                        break;
                    case 3:
                        command.Param04 = paramValue;
                        break;
                }
            }
        }
   
        private bool ParseLightQuestDetails(QuestAssetData assetData, JsonElement jLightQuestDetails)
        {
            if (!jLightQuestDetails.TryGetProperty("board_type", out JsonElement jBoardType))
            {
                Logger.Error($"Missing required member 'board_type' from LightQuest config.");
                return false;
            }
            assetData.LightQuestDetail.BoardType = jBoardType.GetUInt32();

            if (!jLightQuestDetails.TryGetProperty("board_id", out JsonElement jBoardId))
            {
                Logger.Error($"Missing required member 'board_id' from LightQuest config.");
                return false;
            }
            assetData.LightQuestDetail.BoardId = jBoardId.GetUInt32();

            if (jLightQuestDetails.TryGetProperty("area_id", out JsonElement jAreaId))
            {
                assetData.LightQuestDetail.AreaId = jAreaId.GetUInt32();
            }

            if (jLightQuestDetails.TryGetProperty("order_limit", out JsonElement jOrderLimit))
            {
                assetData.LightQuestDetail.OrderLimit = jOrderLimit.GetUInt32();
            }

            if (jLightQuestDetails.TryGetProperty("get_cp", out JsonElement jGetCP))
            {
                assetData.LightQuestDetail.GetCp = jGetCP.GetUInt32();
            }

            return true;
        }

        private bool ParseContentsRelease(HashSet<QuestUnlock> contentsReleased, JsonElement jContentsReleaseList, QuestBlock questBlock = null)
        {
            foreach (var jContentsReleaseId in jContentsReleaseList.EnumerateArray())
            {
                var unlock = new QuestUnlock();

                unlock.ReleaseId = ContentsRelease.None;
                if (jContentsReleaseId.TryGetProperty("type", out JsonElement jReleaseId))
                {
                    if (!Enum.TryParse(jReleaseId.GetString(), true, out ContentsRelease releaseId))
                    {
                        Logger.Error($"Unable to parse contents release element. Skipping.");
                        return false;
                    }
                    unlock.ReleaseId = releaseId;
                }

                unlock.TutorialId = TutorialId.None;
                if (jContentsReleaseId.TryGetProperty("tutorial_id", out JsonElement jTutorialId))
                {
                    if (!Enum.TryParse(jTutorialId.GetString(), true, out TutorialId tutorialId))
                    {
                        Logger.Error($"Unable to parse tutorial id for relased element. Skipping.");
                        return false;
                    }
                    unlock.TutorialId = tutorialId;
                }

                if (jContentsReleaseId.TryGetProperty("flag_info", out JsonElement jFlagInfo))
                {
                    var flagInfo = QuestFlags.GetFlagInfoFromName(jFlagInfo.GetString());
                    if (flagInfo == null)
                    {
                        Logger.Error($"Unable to locate a QuestFlagInfo with the name '{jFlagInfo.GetString()}'. Skipping.");
                        return false;
                    }
                    unlock.FlagInfo = flagInfo;

                    
                    if (questBlock != null)
                    {
                        questBlock.WorldManageUnlocks.Add(flagInfo);
                        questBlock.AddQuestFlag(QuestFlagAction.Set, flagInfo);
                    }
                }

                contentsReleased.Add(unlock);
            }

            return true;
        }
    }
}

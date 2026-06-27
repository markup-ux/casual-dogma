using Arrowgene.Ddon.Shared.Entity.Structure;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Arrowgene.Ddon.Shared.Model.Quest
{
    [Flags]
    public enum QuestBoxRewardFlags : uint
    {
        None = 0,
        RepeatClear = 1 << 0,
        FirstClear = 1 << 1,
        PeriodFirstClear = 1 << 2,
        HelperBonus = 1 << 3,
    }

    public enum QuestRewardBucket
    {
        Normal,
        RepeatClear,
        FirstClear,
        PeriodFirstClear,
        Helper,
    }

    public abstract class LootPoolItem
    {
        public ushort Num { get; set; }
        public ItemId ItemId { get; set; }

        public virtual string GetUID()
        {
            return CreateUID(ItemId, Num);
        }

        public virtual string GetUID(QuestRewardType rewardType, bool isHelp = false, uint selectGroupId = 0)
        {
            return CreateUID(ItemId, Num, rewardType, isHelp, selectGroupId);
        }

        public static string CreateUID(ItemId itemId, ushort num)
        {
            return CreateUIDInternal(itemId, num, null, false, 0);
        }

        public static string CreateUID(ItemId itemId, ushort num, QuestRewardType rewardType, bool isHelp = false, uint selectGroupId = 0)
        {
            return CreateUIDInternal(itemId, num, rewardType, isHelp, selectGroupId);
        }

        private static string CreateUIDInternal(ItemId itemId, ushort num, QuestRewardType? rewardType, bool isHelp, uint selectGroupId)
        {
            IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
            hash.AppendData(BitConverter.GetBytes((uint)itemId));
            hash.AppendData(BitConverter.GetBytes(num));

            if (rewardType != null || isHelp || selectGroupId != 0)
            {
                hash.AppendData(BitConverter.GetBytes((uint)(rewardType ?? QuestRewardType.Unknown)));
                hash.AppendData(BitConverter.GetBytes(isHelp));
                hash.AppendData(BitConverter.GetBytes(selectGroupId));
            }

            return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", string.Empty).Substring(0, 8);
        }
    }

    public class FixedLootPoolItem : LootPoolItem
    {
        public static FixedLootPoolItem Create(ItemId itemId, ushort num)
        {
            return new FixedLootPoolItem()
            {
                ItemId = itemId,
                Num = num,
            };
        }
    }

    public class SelectLootPoolItem : LootPoolItem
    {
        public static SelectLootPoolItem Create(ItemId itemId, ushort num)
        {
            return new SelectLootPoolItem()
            {
                ItemId = itemId,
                Num = num,
            };
        }
    }

    public class ChanceLootPoolItem : LootPoolItem
    {
        public double Chance { get; set; }

        public static ChanceLootPoolItem Create(ItemId itemId, ushort num, double chance)
        {
            return new ChanceLootPoolItem()
            {
                ItemId = itemId,
                Num = num,
                Chance = chance,
            };
        }
    }

    public abstract class QuestRewardItem
    {
        public QuestRewardType RewardType { get; protected set; }
        public bool IsHidden { get; protected set; }

        public QuestRewardItem(QuestRewardType rewardType, bool isHidden)
        {
            RewardType = rewardType;
            IsHidden = isHidden;
            LootPool = new List<LootPoolItem>();
        }

        public List<LootPoolItem> LootPool { get; set; }

        public virtual List<CDataRewardItem> AsCDataRewardItems()
        {
            List<CDataRewardItem> rewards = new List<CDataRewardItem>();

            foreach (var item in LootPool)
            {
                rewards.Add(new CDataRewardItem()
                {
                    ItemId = item.ItemId,
                    Num = item.Num,
                });
            }

            return rewards;
        }

        public virtual List<CDataRewardBoxItem> AsCDataRewardBoxItems(QuestRewardType? rewardTypeOverride = null, bool isHelp = false, uint selectGroupId = 0)
        {
            List<CDataRewardBoxItem> rewards = new List<CDataRewardBoxItem>();
            foreach (var item in LootPool)
            {
                rewards.Add(new CDataRewardBoxItem()
                {
                    ItemId = item.ItemId,
                    Num = item.Num,
                    Type = (byte)(rewardTypeOverride ?? RewardType),
                    IsHelp = isHelp,
                    SelectGroupId = selectGroupId,
                    UID = item.GetUID(rewardTypeOverride ?? RewardType, isHelp, selectGroupId)
                });
            }
            return rewards;
        }
    }

    public class QuestFixedRewardItem : QuestRewardItem
    {
        public QuestFixedRewardItem(bool isHidden = false) : base(QuestRewardType.Fixed, isHidden)
        {
        }

        public static QuestFixedRewardItem Create(ItemId itemId, ushort num, bool isHidden = false)
        {
            return new QuestFixedRewardItem(isHidden)
            {
                LootPool = new List<LootPoolItem>()
                {
                    FixedLootPoolItem.Create(itemId, num)
                }
            };
        }
    }

    public abstract class QuestRandomRewardItem : QuestRewardItem
    {
        public int ItemIndex { get; protected set; }

        public QuestRandomRewardItem(bool isHidden) : base(QuestRewardType.Random, isHidden)
        {
        }

        public QuestRandomRewardItem(int itemIndex, bool isHidden) : base(QuestRewardType.Random, isHidden)
        {
            ItemIndex = itemIndex;
        }

        public override List<CDataRewardBoxItem> AsCDataRewardBoxItems(QuestRewardType? rewardTypeOverride = null, bool isHelp = false, uint selectGroupId = 0)
        {
            return new List<CDataRewardBoxItem>()
            {
                AsCDataRewardBoxItem(rewardTypeOverride, isHelp, selectGroupId)
            };
        }

        public virtual CDataRewardBoxItem AsCDataRewardBoxItem(QuestRewardType? rewardTypeOverride = null, bool isHelp = false, uint selectGroupId = 0)
        {

            var item = LootPool[ItemIndex];
            return new CDataRewardBoxItem()
            {
                ItemId = item.ItemId,
                Num = item.Num,
                Type = (byte)(rewardTypeOverride ?? RewardType),
                IsHelp = isHelp,
                SelectGroupId = selectGroupId,
                UID = item.GetUID(rewardTypeOverride ?? RewardType, isHelp, selectGroupId),
            };
        }

        public virtual CDataRewardBoxItem AsCDataRewardBoxItem(int index, QuestRewardType? rewardTypeOverride = null, bool isHelp = false, uint selectGroupId = 0)
        {
            var item = LootPool[index];
            return new CDataRewardBoxItem()
            {
                ItemId = item.ItemId,
                Num = item.Num,
                Type = (byte)(rewardTypeOverride ?? RewardType),
                IsHelp = isHelp,
                SelectGroupId = selectGroupId,
                UID = item.GetUID(rewardTypeOverride ?? RewardType, isHelp, selectGroupId),
            };
        }

        public abstract int Roll();
    }

    public class QuestRandomFixedRewardItem : QuestRandomRewardItem
    {
        public QuestRandomFixedRewardItem(bool isHidden = false) : base(isHidden)
        {
        }

        public QuestRandomFixedRewardItem(int itemIndex, bool isHidden = false) : base(itemIndex, isHidden)
        {
        }

        public static QuestRandomFixedRewardItem Create(List<(ItemId ItemId, ushort Num)> items, bool isHidden = false)
        {
            var reward = new QuestRandomFixedRewardItem(isHidden);
            foreach (var item in items)
            {
                reward.LootPool.Add(FixedLootPoolItem.Create(item.ItemId, item.Num));
            }
            return reward;
        }

        public override int Roll()
        {
            ItemIndex = Random.Shared.Next(0, LootPool.Count);
            return ItemIndex;
        }
    }

    public class QuestRandomChanceRewardItem : QuestRandomRewardItem
    {
        public QuestRandomChanceRewardItem(bool isHidden = false) : base(isHidden)
        {
        }

        public QuestRandomChanceRewardItem(int itemIndex, bool isHidden = false) : base(itemIndex, isHidden)
        {
        }

        public static QuestRandomChanceRewardItem Create(List<(ItemId ItemId, ushort Num, double Chance)> items, bool isHidden = false)
        {
            var reward = new QuestRandomChanceRewardItem(isHidden);
            foreach (var item in items)
            {
                reward.LootPool.Add(ChanceLootPoolItem.Create(item.ItemId, item.Num, item.Chance));
            }
            return reward;
        }

        public override int Roll()
        {
            ItemIndex = RollInternal();
            return ItemIndex;
        }

        private int RollInternal()
        {
            Random rnd = new Random();
            double target = rnd.NextDouble();

            double sum = 0.0;
            for (int i = 0; i < LootPool.Count; i++)
            {
                ChanceLootPoolItem item = (ChanceLootPoolItem)LootPool[i];
                sum += item.Chance;
                if (target <= sum)
                {
                    return i;
                }
            }

            return 0;
        }
    }

    public class QuestSelectRewardItem : QuestRewardItem
    {
        public QuestSelectRewardItem(bool isHidden = false) : base(QuestRewardType.Select, isHidden)
        {
        }

        public static QuestSelectRewardItem Create(List<(ItemId ItemId, ushort Num)> items, bool isHidden = false)
        {
            var reward = new QuestSelectRewardItem(isHidden);
            foreach (var item in items)
            {
                reward.LootPool.Add(SelectLootPoolItem.Create(item.ItemId, item.Num));
            }
            return reward;
        }
    }

    public class QuestBoxRewards
    {
        public uint UniqRewardId { get; set; }
        public uint CharacterCommonId { get; set; }
        public uint QuestScheduleId { get; set; }
        public int NumRandomRewards { get; set; }
        public List<int> RandomRewardIndices { get; set; }
        public bool IsRepeatReward { get; set; }
        public QuestBoxRewardFlags RewardFlags { get; set; }
        public List<CDataRewardBoxItem> RewardItemList { get; set; }

        public QuestBoxRewards()
        {
            RandomRewardIndices = new List<int>();
            RewardItemList = new List<CDataRewardBoxItem>();
        }
    }
}

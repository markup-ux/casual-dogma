using System.Collections.Generic;
using System.Data.Common;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    protected static readonly string[] RewardBoxFields = new[]
    {
        /* uniq_reward_id */ "character_common_id", "quest_schedule_id", "num_random_rewards", "random_reward0_index", "random_reward1_index", "random_reward2_index",
        "random_reward3_index", "is_repeat_reward", "reward_flags"
    };

    protected static readonly string[] RewardBoxItemFields = new[]
    {
        /* reward_box_item_id */ "uniq_reward_id", "item_id", "num", "uid", "type", "is_charge", "is_help", "select_group_id", "is_instance"
    };

    private readonly int MAX_RANDOM_REWARDS = 4;

    private readonly string SqlDeleteRewardBoxItem =
        "DELETE FROM \"ddon_reward_box\" WHERE \"uniq_reward_id\"=@uniq_reward_id AND \"character_common_id\"=@character_common_id;";

    private readonly string SqlInsertRewardBoxItems = $"INSERT INTO \"ddon_reward_box\" ({BuildQueryField(RewardBoxFields)}) VALUES ({BuildQueryInsert(RewardBoxFields)});";

    private readonly string SqlInsertRewardBoxItem = $"INSERT INTO \"ddon_reward_box_item\" ({BuildQueryField(RewardBoxItemFields)}) VALUES ({BuildQueryInsert(RewardBoxItemFields)});";

    private readonly string SqlSelectRewardBoxItems =
        $"SELECT \"uniq_reward_id\", {BuildQueryField(RewardBoxFields)} FROM \"ddon_reward_box\" WHERE \"character_common_id\" = @character_common_id;";

    private readonly string SqlSelectRewardBoxItemList =
        $"SELECT \"reward_box_item_id\", {BuildQueryField(RewardBoxItemFields)} FROM \"ddon_reward_box_item\" WHERE \"uniq_reward_id\" = @uniq_reward_id ORDER BY \"reward_box_item_id\";";

    public override bool InsertBoxRewardItems(uint commonId, QuestBoxRewards rewards, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            foreach (var reward in rewards.RewardItemList)
            {
                if (reward.IsInstance && reward.StagedItem == null)
                {
                    return false;
                }
            }

            int rowsAffected = ExecuteNonQuery(connection, SqlInsertRewardBoxItems, command =>
            {
                AddParameter(command, "character_common_id", commonId);
                AddParameter(command, "quest_schedule_id", rewards.QuestScheduleId);
                AddParameter(command, "num_random_rewards", rewards.NumRandomRewards);

                int i;
                for (i = 0; i < rewards.NumRandomRewards; i++) AddParameter(command, $"random_reward{i}_index", rewards.RandomRewardIndices[i]);

                for (; i < MAX_RANDOM_REWARDS; i++) AddParameter(command, $"random_reward{i}_index", 0);

                AddParameter(command, "is_repeat_reward", rewards.IsRepeatReward ? 1 : 0);
                AddParameter(command, "reward_flags", (uint)rewards.RewardFlags);
            }, out long autoIncrement);

            if (rowsAffected != 1 || autoIncrement < 0)
            {
                return false;
            }

            uint uniqRewardId = (uint)autoIncrement;
            foreach (var reward in rewards.RewardItemList)
            {
                if (!InsertBoxRewardItemWithStagedItem(uniqRewardId, reward, connection))
                {
                    return false;
                }
            }

            return true;
        });
    }

    public override bool InsertBoxRewardItem(uint uniqRewardId, CDataRewardBoxItem reward, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return InsertBoxRewardItemWithStagedItem(uniqRewardId, reward, connection);
        });
    }

    private bool InsertBoxRewardItemWithStagedItem(uint uniqRewardId, CDataRewardBoxItem reward, DbConnection connection)
    {
        if (reward.IsInstance && reward.StagedItem == null)
        {
            return false;
        }

        long rewardBoxItemId = InsertBoxRewardItemInternal(uniqRewardId, reward, connection);
        if (rewardBoxItemId < 0)
        {
            return false;
        }

        if (!reward.IsInstance)
        {
            return true;
        }

        reward.StagedItem.RewardBoxItemId = rewardBoxItemId;
        return InsertStagedItem(reward.StagedItem, connection);
    }

    private long InsertBoxRewardItemInternal(uint uniqRewardId, CDataRewardBoxItem reward, DbConnection connection)
    {
        int rows = ExecuteNonQuery(connection, SqlInsertRewardBoxItem, command =>
        {
            AddRewardBoxItemParameters(command, uniqRewardId, reward);
        }, out long autoIncrement);
        return rows == 1 ? autoIncrement : -1;
    }

    public override List<QuestBoxRewards> SelectBoxRewardItems(uint commonId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            List<QuestBoxRewards> results = new();
            ExecuteReader(connection, SqlSelectRewardBoxItems,
                command => { AddParameter(command, "@character_common_id", commonId); }, reader =>
                {
                    while (reader.Read())
                    {
                        QuestBoxRewards result = ReadDatabaseQuestBoxReward(reader);
                        results.Add(result);
                    }
                });

            foreach (var result in results)
            {
                result.RewardItemList = SelectRewardBoxItemList(result.UniqRewardId, connection);
            }

            return results;
        });
    }

    public override bool DeleteBoxRewardItem(uint commonId, uint uniqId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlDeleteRewardBoxItem, command =>
            {
                AddParameter(command, "@character_common_id", commonId);
                AddParameter(command, "@uniq_reward_id", uniqId);
            }) == 1;
        });
    }

    private QuestBoxRewards ReadDatabaseQuestBoxReward(DbDataReader reader)
    {
        QuestBoxRewards obj = new();
        obj.UniqRewardId = GetUInt32(reader, "uniq_reward_id");
        obj.CharacterCommonId = GetUInt32(reader, "character_common_id");
        obj.NumRandomRewards = GetInt32(reader, "num_random_rewards");
        obj.QuestScheduleId = GetUInt32(reader, "quest_schedule_id");

        for (int i = 0; i < obj.NumRandomRewards; i++) obj.RandomRewardIndices.Add(GetInt32(reader, $"random_reward{i}_index"));
        obj.IsRepeatReward = GetInt32(reader, "is_repeat_reward") != 0;
        obj.RewardFlags = (QuestBoxRewardFlags)GetUInt32(reader, "reward_flags");
        if (obj.RewardFlags == QuestBoxRewardFlags.None && obj.IsRepeatReward)
        {
            obj.RewardFlags = QuestBoxRewardFlags.RepeatClear;
        }

        return obj;
    }

    private List<CDataRewardBoxItem> SelectRewardBoxItemList(uint uniqRewardId, DbConnection connection)
    {
        List<CDataRewardBoxItem> results = new();
        ExecuteReader(connection, SqlSelectRewardBoxItemList,
            command => { AddParameter(command, "@uniq_reward_id", uniqRewardId); }, reader =>
            {
                while (reader.Read())
                {
                    results.Add(new CDataRewardBoxItem()
                    {
                        RewardBoxItemId = GetInt64(reader, "reward_box_item_id"),
                        ItemId = (ItemId)GetUInt32(reader, "item_id"),
                        Num = GetUInt16(reader, "num"),
                        UID = GetString(reader, "uid"),
                        Type = GetByte(reader, "type"),
                        IsCharge = GetInt32(reader, "is_charge") != 0,
                        IsHelp = GetInt32(reader, "is_help") != 0,
                        SelectGroupId = GetUInt32(reader, "select_group_id"),
                        IsInstance = GetInt32(reader, "is_instance") != 0,
                    });
                }
            });
        return results;
    }

    private void AddRewardBoxItemParameters(DbCommand command, uint uniqRewardId, CDataRewardBoxItem reward)
    {
        AddParameter(command, "uniq_reward_id", uniqRewardId);
        AddParameter(command, "item_id", (uint)reward.ItemId);
        AddParameter(command, "num", reward.Num);
        AddParameter(command, "uid", reward.UID);
        AddParameter(command, "type", reward.Type);
        AddParameter(command, "is_charge", reward.IsCharge ? 1 : 0);
        AddParameter(command, "is_help", reward.IsHelp ? 1 : 0);
        AddParameter(command, "select_group_id", reward.SelectGroupId);
        AddParameter(command, "is_instance", reward.IsInstance ? 1 : 0);
    }
}

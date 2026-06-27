using Arrowgene.Ddon.Shared.Model.Quest;
using System.Collections.Generic;
using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    private static readonly string[] StagedItemFields = new[]
    {
        "uid", "reward_box_item_id", "item_id", "num", "color", "plus_value", "safety_setting"
    };

    private static readonly string[] StagedItemCrestFields = new[]
    {
        "uid", "slot", "crest_id", "level"
    };

    private readonly string SqlInsertStagedItem =
        $"INSERT INTO \"ddon_reward_staged_item\" ({BuildQueryField(StagedItemFields)}) VALUES ({BuildQueryInsert(StagedItemFields)});";

    private readonly string SqlInsertStagedItemCrest =
        $"INSERT INTO \"ddon_reward_staged_item_crest\" ({BuildQueryField(StagedItemCrestFields)}) VALUES ({BuildQueryInsert(StagedItemCrestFields)});";

    private readonly string SqlSelectStagedItem =
        $"SELECT {BuildQueryField(StagedItemFields)} FROM \"ddon_reward_staged_item\" WHERE \"uid\" = @uid;";

    private readonly string SqlSelectStagedItemCrests =
        $"SELECT {BuildQueryField(StagedItemCrestFields)} FROM \"ddon_reward_staged_item_crest\" WHERE \"uid\" = @uid;";

    private readonly string SqlDeleteStagedItem =
        "DELETE FROM \"ddon_reward_staged_item\" WHERE \"uid\" = @uid;";

    public override bool InsertStagedItem(StagedRewardItem item, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            int rows = ExecuteNonQuery(connection, SqlInsertStagedItem, command =>
            {
                AddParameter(command, "uid", item.Uid);
                AddParameter(command, "reward_box_item_id", item.RewardBoxItemId);
                AddParameter(command, "item_id", item.ItemId);
                AddParameter(command, "num", item.Num);
                AddParameter(command, "color", item.Color);
                AddParameter(command, "plus_value", item.PlusValue);
                AddParameter(command, "safety_setting", item.SafetySetting);
            });

            if (rows != 1) return false;

            foreach (var crest in item.Crests)
            {
                if (!InsertStagedItemCrest(crest, connection)) return false;
            }

            return true;
        });
    }

    public override bool InsertStagedItemCrest(StagedRewardItemCrest crest, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlInsertStagedItemCrest, command =>
            {
                AddParameter(command, "uid", crest.Uid);
                AddParameter(command, "slot", crest.Slot);
                AddParameter(command, "crest_id", crest.CrestId);
                AddParameter(command, "level", crest.Level);
            }) == 1;
        });
    }

    public override StagedRewardItem? SelectStagedItem(string uid, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            StagedRewardItem? result = null;
            ExecuteReader(connection, SqlSelectStagedItem,
                command => { AddParameter(command, "@uid", uid); },
                reader =>
                {
                    if (reader.Read())
                    {
                        result = ReadStagedItem(reader);
                    }
                });

            if (result != null)
            {
                result.Crests = SelectStagedItemCrests(uid, connection);
            }

            return result;
        });
    }

    public override bool DeleteStagedItem(string uid, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlDeleteStagedItem, command =>
            {
                AddParameter(command, "@uid", uid);
            }) == 1;
        });
    }

    private List<StagedRewardItemCrest> SelectStagedItemCrests(string uid, DbConnection connection)
    {
        List<StagedRewardItemCrest> results = new();
        ExecuteReader(connection, SqlSelectStagedItemCrests,
            command => { AddParameter(command, "@uid", uid); },
            reader =>
            {
                while (reader.Read())
                {
                    results.Add(new StagedRewardItemCrest
                    {
                        Uid = GetString(reader, "uid"),
                        Slot = GetUInt32(reader, "slot"),
                        CrestId = GetUInt32(reader, "crest_id"),
                        Level = GetUInt32(reader, "level"),
                    });
                }
            });
        return results;
    }

    private StagedRewardItem ReadStagedItem(DbDataReader reader)
    {
        return new StagedRewardItem
        {
            Uid = GetString(reader, "uid"),
            RewardBoxItemId = GetInt64(reader, "reward_box_item_id"),
            ItemId = GetUInt32(reader, "item_id"),
            Num = GetUInt32(reader, "num"),
            Color = GetUInt32(reader, "color"),
            PlusValue = GetUInt32(reader, "plus_value"),
            SafetySetting = GetUInt32(reader, "safety_setting"),
        };
    }
}

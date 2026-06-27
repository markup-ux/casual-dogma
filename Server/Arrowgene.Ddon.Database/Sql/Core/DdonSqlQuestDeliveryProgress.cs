using System.Collections.Generic;
using System.Data.Common;
using Arrowgene.Ddon.Shared.Model.Quest;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    private readonly string SqlUpsertQuestDeliveryProgress =
        """
        INSERT INTO "ddon_quest_delivery_progress" ("character_common_id", "quest_schedule_id", "item_id", "amount_delivered")
            VALUES (@character_common_id, @quest_schedule_id, @item_id, @amount_delivered)
            ON CONFLICT ("character_common_id", "quest_schedule_id", "item_id")
            DO UPDATE SET "amount_delivered" = EXCLUDED."amount_delivered";
        """;

    private readonly string SqlDeleteQuestDeliveryProgress =
        "DELETE FROM \"ddon_quest_delivery_progress\" WHERE \"character_common_id\" = @character_common_id AND \"quest_schedule_id\" = @quest_schedule_id;";

    private readonly string SqlSelectAllQuestDeliveryProgress =
        "SELECT \"character_common_id\", \"quest_schedule_id\", \"item_id\", \"amount_delivered\" FROM \"ddon_quest_delivery_progress\" WHERE \"character_common_id\" = @character_common_id;";

    public override bool UpsertQuestDeliveryProgress(uint characterCommonId, uint questScheduleId, uint itemId, uint amountDelivered, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlUpsertQuestDeliveryProgress, cmd =>
            {
                AddParameter(cmd, "character_common_id", characterCommonId);
                AddParameter(cmd, "quest_schedule_id", questScheduleId);
                AddParameter(cmd, "item_id", itemId);
                AddParameter(cmd, "amount_delivered", amountDelivered);
            }) == 1;
        });
    }

    public override bool DeleteQuestDeliveryProgress(uint characterCommonId, uint questScheduleId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlDeleteQuestDeliveryProgress, cmd =>
            {
                AddParameter(cmd, "character_common_id", characterCommonId);
                AddParameter(cmd, "quest_schedule_id", questScheduleId);
            }) >= 0;
        });
    }

    public override List<QuestDeliveryProgress> GetAllQuestDeliveryProgress(uint characterCommonId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            List<QuestDeliveryProgress> results = new();
            ExecuteReader(connection, SqlSelectAllQuestDeliveryProgress,
                cmd => AddParameter(cmd, "@character_common_id", characterCommonId),
                reader =>
                {
                    while (reader.Read())
                    {
                        results.Add(new QuestDeliveryProgress
                        {
                            CharacterCommonId = GetUInt32(reader, "character_common_id"),
                            QuestScheduleId = GetUInt32(reader, "quest_schedule_id"),
                            ItemId = GetUInt32(reader, "item_id"),
                            AmountDelivered = GetUInt32(reader, "amount_delivered")
                        });
                    }
                });
            return results;
        });
    }
}

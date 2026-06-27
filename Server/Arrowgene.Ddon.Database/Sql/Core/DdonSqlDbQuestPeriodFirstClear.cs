using System.Collections.Generic;
using System.Data.Common;
using Arrowgene.Ddon.Shared.Model.Quest;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    private readonly string SqlInsertQuestPeriodFirstClear =
        "INSERT INTO \"ddon_quest_period_first_clear\" (\"character_common_id\", \"quest_type\", \"quest_schedule_id\") VALUES (@character_common_id, @quest_type, @quest_schedule_id) ON CONFLICT DO NOTHING;";

    private readonly string SqlHasQuestPeriodFirstClear =
        "SELECT COUNT(*) FROM \"ddon_quest_period_first_clear\" WHERE \"character_common_id\" = @character_common_id AND \"quest_type\" = @quest_type AND \"quest_schedule_id\" = @quest_schedule_id;";

    private readonly string SqlSelectQuestPeriodFirstClears =
        "SELECT \"quest_schedule_id\" FROM \"ddon_quest_period_first_clear\" WHERE \"character_common_id\" = @character_common_id AND \"quest_type\" = @quest_type;";

    private readonly string SqlSelectAllQuestPeriodFirstClears =
        "SELECT \"quest_type\", \"quest_schedule_id\" FROM \"ddon_quest_period_first_clear\" WHERE \"character_common_id\" = @character_common_id;";

    private readonly string SqlDeleteQuestPeriodFirstClears =
        "DELETE FROM \"ddon_quest_period_first_clear\" WHERE \"quest_type\" = @quest_type;";

    public override bool InsertQuestPeriodFirstClear(uint commonId, QuestType questType, uint questScheduleId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlInsertQuestPeriodFirstClear, command =>
            {
                AddParameter(command, "@character_common_id", commonId);
                AddParameter(command, "@quest_type", (uint) questType);
                AddParameter(command, "@quest_schedule_id", questScheduleId);
            }) >= 0;
        });
    }

    public override bool HasQuestPeriodFirstClear(uint commonId, QuestType questType, uint questScheduleId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            long count = 0;
            ExecuteReader(connection, SqlHasQuestPeriodFirstClear,
                command =>
                {
                    AddParameter(command, "@character_common_id", commonId);
                    AddParameter(command, "@quest_type", (uint) questType);
                    AddParameter(command, "@quest_schedule_id", questScheduleId);
                },
                reader =>
                {
                    if (reader.Read())
                        count = reader.GetInt64(0);
                });
            return count > 0;
        });
    }

    public override bool DeleteQuestPeriodFirstClears(QuestType questType, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlDeleteQuestPeriodFirstClears, command =>
            {
                AddParameter(command, "@quest_type", (uint) questType);
            }) >= 0;
        });
    }

    public override HashSet<uint> SelectQuestPeriodFirstClears(uint commonId, QuestType questType, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            var result = new HashSet<uint>();
            ExecuteReader(connection, SqlSelectQuestPeriodFirstClears,
                command =>
                {
                    AddParameter(command, "@character_common_id", commonId);
                    AddParameter(command, "@quest_type", (uint) questType);
                },
                reader =>
                {
                    while (reader.Read())
                        result.Add(GetUInt32(reader, "quest_schedule_id"));
                });
            return result;
        });
    }

    public override Dictionary<QuestType, HashSet<uint>> SelectQuestPeriodFirstClears(uint commonId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            var result = new Dictionary<QuestType, HashSet<uint>>();
            ExecuteReader(connection, SqlSelectAllQuestPeriodFirstClears,
                command => { AddParameter(command, "@character_common_id", commonId); },
                reader =>
                {
                    while (reader.Read())
                    {
                        QuestType questType = (QuestType)GetUInt32(reader, "quest_type");
                        if (!result.TryGetValue(questType, out var periodFirstClears))
                        {
                            periodFirstClears = new HashSet<uint>();
                            result[questType] = periodFirstClears;
                        }

                        periodFirstClears.Add(GetUInt32(reader, "quest_schedule_id"));
                    }
                });
            return result;
        });
    }

}

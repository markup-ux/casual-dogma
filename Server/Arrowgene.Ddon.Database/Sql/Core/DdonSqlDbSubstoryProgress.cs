using Arrowgene.Ddon.Shared.Model.Quest;
using System.Collections.Generic;
using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core
{
    public partial class DdonSqlDb : SqlDb
    {
        private static readonly string[] DdonSubstoryProgressFields =
        [
            "character_id", "substory_group_id", "sequence_step", "is_complete"
        ];

        private readonly string SqlInsertSubstoryProgress =
            $"INSERT INTO \"ddon_substory_progress\" ({BuildQueryField(DdonSubstoryProgressFields)}) VALUES ({BuildQueryInsert(DdonSubstoryProgressFields)});";

        private readonly string SqlSelectSubstoryProgress =
            $"SELECT {BuildQueryField(DdonSubstoryProgressFields)} FROM \"ddon_substory_progress\" WHERE \"character_id\"=@character_id;";

        private readonly string SqlUpsertSubstoryProgress =
            "INSERT INTO \"ddon_substory_progress\" (\"character_id\", \"substory_group_id\", \"sequence_step\", \"is_complete\") VALUES (@character_id, @substory_group_id, @sequence_step, @is_complete) " +
            "ON CONFLICT (\"character_id\", \"substory_group_id\") DO UPDATE SET \"sequence_step\"=@sequence_step, \"is_complete\"=@is_complete;";

        public override Dictionary<QuestSubstoryGroupId, SubstoryProgress> SelectSubstoryProgress(uint characterId, DbConnection? connectionIn = null)
        {
            return ExecuteQuerySafe(connectionIn, connection =>
            {
                var result = new Dictionary<QuestSubstoryGroupId, SubstoryProgress>();
                ExecuteReader(connection, SqlSelectSubstoryProgress,
                    command => AddParameter(command, "@character_id", characterId),
                    reader =>
                    {
                        while (reader.Read())
                        {
                            var progress = new SubstoryProgress
                            {
                                SubstoryGroupId = (QuestSubstoryGroupId)GetUInt32(reader, "substory_group_id"),
                                SequenceStep = GetInt32(reader, "sequence_step"),
                                IsComplete = GetBoolean(reader, "is_complete")
                            };
                            result[progress.SubstoryGroupId] = progress;
                        }
                    });
                return result;
            });
        }

        public override bool UpsertSubstoryProgress(uint characterId, SubstoryProgress progress, DbConnection? connectionIn = null)
        {
            return ExecuteQuerySafe(connectionIn, connection =>
            {
                return ExecuteNonQuery(connection, SqlUpsertSubstoryProgress, command =>
                {
                    AddParameter(command, "@character_id", characterId);
                    AddParameter(command, "@substory_group_id", (uint)progress.SubstoryGroupId);
                    AddParameter(command, "@sequence_step", progress.SequenceStep);
                    AddParameter(command, "@is_complete", progress.IsComplete);
                }) == 1;
            });
        }
    }
}

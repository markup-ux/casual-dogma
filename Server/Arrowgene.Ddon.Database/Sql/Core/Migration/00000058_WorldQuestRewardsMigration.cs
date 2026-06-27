using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration
{
    public class WorldQuestRewardsMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
    {
        public uint From => 57;
        public uint To => 58;

        public bool Migrate(IDatabase db, DbConnection conn)
        {
            string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_world_quest_rewards.sql");
            db.Execute(conn, adaptedSchema, true);
            return true;
        }
    }
}

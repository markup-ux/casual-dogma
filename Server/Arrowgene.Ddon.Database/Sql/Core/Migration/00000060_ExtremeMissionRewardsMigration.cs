using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration
{
    public class ExtremeMissionRewardsMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
    {
        public uint From => 59;
        public uint To => 60;

        public bool Migrate(IDatabase db, DbConnection conn)
        {
            string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_extreme_mission_rewards.sql");
            db.Execute(conn, adaptedSchema, true);
            return true;
        }
    }
}

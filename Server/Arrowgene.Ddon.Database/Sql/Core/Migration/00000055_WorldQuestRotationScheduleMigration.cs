using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration
{
    public class WorldQuestRotationScheduleMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
    {
        public uint From => 54;
        public uint To => 55;

        public bool Migrate(IDatabase db, DbConnection conn)
        {
            string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_world_quest_rotation.sql");
            db.Execute(conn, adaptedSchema, true);
            return true;
        }
    }
}

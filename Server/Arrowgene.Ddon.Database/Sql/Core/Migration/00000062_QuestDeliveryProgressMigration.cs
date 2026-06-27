using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration
{
    public class QuestDeliveryProgressMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
    {
        public uint From => 61;
        public uint To => 62;

        public bool Migrate(IDatabase db, DbConnection conn)
        {
            string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_quest_delivery_progress.sql");
            db.Execute(conn, adaptedSchema, true);
            return true;
        }
    }
}

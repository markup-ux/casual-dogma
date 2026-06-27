using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration
{
    public class CraftAddStatusMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
    {
        public uint From => 50;
        public uint To => 51;

        public bool Migrate(IDatabase db, DbConnection conn)
        {
            string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_equipment_limit_break_datatype.sql");
            db.Execute(conn, adaptedSchema, true);
            return true;
        }
    }
}

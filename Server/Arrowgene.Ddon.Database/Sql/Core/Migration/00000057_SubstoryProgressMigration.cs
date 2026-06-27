using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration
{
    public class SubstoryProgressMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
    {
        public uint From => 56;
        public uint To => 57;

        public bool Migrate(IDatabase db, DbConnection conn)
        {
            string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_substory_progress.sql");
            db.Execute(conn, adaptedSchema);
            return true;
        }
    }
}

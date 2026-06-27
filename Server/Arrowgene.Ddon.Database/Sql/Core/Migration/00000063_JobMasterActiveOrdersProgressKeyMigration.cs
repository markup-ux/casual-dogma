using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration;

public class JobMasterActiveOrdersProgressKeyMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
{
    public uint From => 62;
    public uint To => 63;

    public bool Migrate(IDatabase db, DbConnection conn)
    {
        string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/ddon_job_master_active_orders_progress_key_migration.sql");
        db.Execute(conn, adaptedSchema, true);
        return true;
    }
}

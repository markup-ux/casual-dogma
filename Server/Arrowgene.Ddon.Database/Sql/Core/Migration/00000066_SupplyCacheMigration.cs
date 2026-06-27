using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration;

public class SupplyCacheMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
{
    public uint From => 65;
    public uint To => 66;

    public bool Migrate(IDatabase db, DbConnection conn)
    {
        string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_supply_cache.sql");
        db.Execute(conn, adaptedSchema, true);
        return true;
    }
}

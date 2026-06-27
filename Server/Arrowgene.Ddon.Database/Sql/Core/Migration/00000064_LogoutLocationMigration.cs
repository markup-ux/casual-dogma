using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration;

public class LogoutLocationMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
{
    public uint From => 63;
    public uint To => 64;

    public bool Migrate(IDatabase db, DbConnection conn)
    {
        string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_logout_location.sql");
        db.Execute(conn, adaptedSchema, true);
        return true;
    }
}

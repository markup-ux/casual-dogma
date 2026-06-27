using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration;

public class RevivalRechargeMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
{
    public uint From => 64;
    public uint To => 65;

    public bool Migrate(IDatabase db, DbConnection conn)
    {
        string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_revival_recharge.sql");
        db.Execute(conn, adaptedSchema, true);
        return true;
    }
}

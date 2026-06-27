using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core.Migration
{
    public class RentalPawnMigration(DatabaseSetting databaseSetting) : IMigrationStrategy
    {
        public uint From => 48;
        public uint To => 49;

        public bool Migrate(IDatabase db, DbConnection conn)
        {
            string adaptedSchema = DdonDatabaseBuilder.GetAdaptedSchema(databaseSetting, "Script/migration_pawn_rental.sql");
            db.Execute(conn, adaptedSchema, true);
            return true;
        }
    }
}

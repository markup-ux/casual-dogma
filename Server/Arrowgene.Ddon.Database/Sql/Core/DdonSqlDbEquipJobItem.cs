using System.ComponentModel;
using System.Data.Common;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    protected static readonly string[] CDataEquipJobItemFields = new[]
    {
        "item_uid", "character_common_id", "job", "equip_slot"
    };

    private static readonly string SqlSelectEquipJobItems =
        $"SELECT {BuildQueryField(CDataEquipJobItemFields)} FROM \"ddon_equip_job_item\" WHERE \"character_common_id\" = @character_common_id AND \"job\" = @job;";

    private static readonly string SqlSelectEquipJobItemsByCharacter =
        $"SELECT {BuildQueryField(CDataEquipJobItemFields)} FROM \"ddon_equip_job_item\" WHERE \"character_common_id\" = @character_common_id;";

    private readonly string SqlUpsertEquipJobItem =
        $@"INSERT INTO ""ddon_equip_job_item"" ({BuildQueryField(CDataEquipJobItemFields)}) VALUES ({BuildQueryInsert(CDataEquipJobItemFields)}) ON CONFLICT (character_common_id, job, equip_slot) DO UPDATE SET item_uid = EXCLUDED.item_uid;";

    private const string SqlDeleteEquipJobItem =
        "DELETE FROM \"ddon_equip_job_item\" WHERE \"character_common_id\"=@character_common_id AND \"job\"=@job AND \"equip_slot\"=@equip_slot;";

    public override bool UpsertEquipJobItem(string itemUId, uint commonId, JobId job, ushort slotNo, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlUpsertEquipJobItem, command =>
            {
                AddParameter(command, "item_uid", itemUId);
                AddParameter(command, "character_common_id", commonId);
                AddParameter(command, "job", (byte)job);
                AddParameter(command, "equip_slot", slotNo);
            }) == 1;
        });
    }

    public override bool DeleteEquipJobItem(uint commonId, JobId job, ushort slotNo)
    {
        return ExecuteNonQuery(SqlDeleteEquipJobItem, command =>
        {
            AddParameter(command, "character_common_id", commonId);
            AddParameter(command, "job", (byte)job);
            AddParameter(command, "equip_slot", slotNo);
        }) == 1;
    }
}

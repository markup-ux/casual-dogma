using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    /* ddon_equipment_limit_break */
    protected static readonly string[] EquipmentLimitBreakFields = new[]
    {
        "character_id", "item_uid", "effect_id", "unk1", "effect_type", "unk0"
    };

    // Identify key vs. non‑key fields for the upsert
    protected static readonly string[] EquipmentLimitBreakKeyFields = new[]
    {
        "character_id", "item_uid", "effect_type"
    };

    protected static readonly string[] EquipmentLimitBreakNonKeyFields
        = EquipmentLimitBreakFields.Except(EquipmentLimitBreakKeyFields).ToArray();

    protected readonly string SqlInsertEquipmentLimitBreakRecord =
        $"INSERT INTO \"ddon_equipment_limit_break\" ({BuildQueryField(EquipmentLimitBreakFields)}) VALUES ({BuildQueryInsert(EquipmentLimitBreakFields)});";

    protected readonly string SqlSelectEquipmentLimitBreakRecord =
        $"SELECT {BuildQueryField(EquipmentLimitBreakFields)} FROM \"ddon_equipment_limit_break\" WHERE \"item_uid\"=@item_uid;";

    protected readonly string SqlUpdateEquipmentLimitBreakRecord =
        $"UPDATE \"ddon_equipment_limit_break\" SET {BuildQueryUpdate(EquipmentLimitBreakFields)} WHERE \"character_id\"=@character_id AND \"item_uid\"=@item_uid;";

    private readonly string SqlUpsertEquipmentLimitBreakRecord =
        $"""
         INSERT INTO "ddon_equipment_limit_break" ({BuildQueryField(EquipmentLimitBreakFields)}) 
                        VALUES ({BuildQueryInsert(EquipmentLimitBreakFields)}) 
                        ON CONFLICT ("character_id","item_uid","effect_type") 
                        DO UPDATE SET {BuildQueryUpdateWithPrefix("EXCLUDED.", EquipmentLimitBreakNonKeyFields)};
         """;

    public override bool HasEquipmentLimitBreakRecord(uint characterId, string itemUID, DbConnection? connectionIn = null)
    {
        bool foundRecord = false;
        ExecuteQuerySafe(connectionIn, connection =>
        {
            ExecuteReader(connection, SqlSelectEquipmentLimitBreakRecord, command =>
            {
                AddParameter(command, "character_id", characterId);
                AddParameter(command, "item_uid", itemUID);
            }, reader => { foundRecord = reader.Read(); });
        });
        return foundRecord;
    }

    /// <summary>
    ///     Insert or update in one round‑trip using Postgres ON CONFLICT.
    ///     Returns true if exactly one row was inserted or updated.
    /// </summary>
    public override bool UpsertEquipmentLimitBreakRecord(uint characterId, string itemUID, CDataAddStatusParam statusParam, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlUpsertEquipmentLimitBreakRecord, command =>
            {
                AddParameter(command, "character_id", characterId);
                AddParameter(command, "item_uid", itemUID);
                AddParameter(command, "effect_id", statusParam.EnhanceId);
                AddParameter(command, "unk1", statusParam.Unk1);
                AddParameter(command, "effect_type", (byte)statusParam.EnhanceType);
                AddParameter(command, "unk0", statusParam.Unk0);
            }) == 1;
        });
    }

    public override List<CDataAddStatusParam> GetEquipmentLimitBreakRecord(string itemUID, DbConnection? connectionIn = null)
    {
        List<CDataAddStatusParam> results = new();
        ExecuteQuerySafe(connectionIn, connection =>
        {
            ExecuteReader(connection, SqlSelectEquipmentLimitBreakRecord, command => { AddParameter(command, "item_uid", itemUID); }, reader =>
            {
                while (reader.Read())
                {
                    results.Add(new CDataAddStatusParam
                    {
                        EnhanceId = GetUInt16(reader, "effect_id"),
                        Unk1 = GetUInt16(reader, "unk1"),
                        EnhanceType = (EquipEnhanceType)GetByte(reader, "effect_type"),
                        Unk0 = GetByte(reader, "unk0")
                    });
                }
            });
        });
        return results;
    }
}

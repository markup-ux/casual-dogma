using Arrowgene.Ddon.Shared.Model;
using System.Collections.Generic;
using System.Data.Common;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    protected static readonly string[] RevivalRechargePendingFields =
    {
        "id", "character_id", "recharge_type", "expires_at"
    };

    private readonly string SqlSelectRevivalRechargePending =
        $"SELECT {BuildQueryField(RevivalRechargePendingFields)} FROM \"ddon_revival_recharge_pending\" WHERE \"character_id\"=@character_id ORDER BY \"expires_at\" ASC;";

    private readonly string SqlInsertRevivalRechargePending =
        $"INSERT INTO \"ddon_revival_recharge_pending\" (\"character_id\", \"recharge_type\", \"expires_at\") VALUES (@character_id, @recharge_type, @expires_at);";

    private readonly string SqlDeleteRevivalRechargePending =
        "DELETE FROM \"ddon_revival_recharge_pending\" WHERE \"id\"=@id;";

    public override List<RevivalRechargePending> SelectRevivalRechargePending(uint characterId, DbConnection? connectionIn = null)
    {
        List<RevivalRechargePending> results = new();
        ExecuteQuerySafe(connectionIn, connection =>
        {
            ExecuteReader(connection, SqlSelectRevivalRechargePending, command =>
            {
                AddParameter(command, "character_id", characterId);
            }, reader =>
            {
                while (reader.Read())
                {
                    results.Add(ReadRevivalRechargePending(reader));
                }
            });
        });
        return results;
    }

    public override bool InsertRevivalRechargePending(uint characterId, RevivalRechargeType type, long expiresAtUnix, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlInsertRevivalRechargePending, command =>
            {
                AddParameter(command, "character_id", characterId);
                AddParameter(command, "recharge_type", (byte)type);
                AddParameter(command, "expires_at", expiresAtUnix);
            }) == 1;
        });
    }

    public override bool DeleteRevivalRechargePending(long id, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlDeleteRevivalRechargePending, command =>
            {
                AddParameter(command, "id", id);
            }) == 1;
        });
    }

    private RevivalRechargePending ReadRevivalRechargePending(DbDataReader reader)
    {
        return new RevivalRechargePending
        {
            Id = GetInt64(reader, "id"),
            CharacterId = GetUInt32(reader, "character_id"),
            Type = (RevivalRechargeType)GetByte(reader, "recharge_type"),
            ExpiresAtUnix = GetInt64(reader, "expires_at")
        };
    }
}

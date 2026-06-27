using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Database.Sql.Core;

public partial class DdonSqlDb : SqlDb
{
    private static readonly string[] SupplyCacheFields =
    [
        "id", "map_id", "layer_no", "group_id", "x", "y", "z", "rotation", "set_id", "created", "updated"
    ];

    private static readonly string[] SupplyCacheItemFields =
    [
        "cache_id", "slot", "serialized_item"
    ];

    private static readonly string SqlInsertSupplyCache =
        "INSERT INTO \"ddon_supply_cache\" (\"map_id\", \"layer_no\", \"group_id\", \"x\", \"y\", \"z\", \"rotation\", \"set_id\", \"created\", \"updated\") " +
        "VALUES (@map_id, @layer_no, @group_id, @x, @y, @z, @rotation, @set_id, @created, @updated);";

    private static readonly string SqlUpdateSupplyCacheTimestamp =
        "UPDATE \"ddon_supply_cache\" SET \"updated\" = @updated WHERE \"id\" = @id;";

    private static readonly string SqlUpdateSupplyCachePosition =
        "UPDATE \"ddon_supply_cache\" SET \"x\" = @x, \"y\" = @y, \"z\" = @z, \"rotation\" = @rotation, \"layer_no\" = @layer_no, \"group_id\" = @group_id, \"updated\" = @updated WHERE \"id\" = @id;";

    private static readonly string SqlSelectAllSupplyCaches =
        $"SELECT {BuildQueryField(SupplyCacheFields)} FROM \"ddon_supply_cache\";";

    private static readonly string SqlSelectSupplyCacheItems =
        $"SELECT {BuildQueryField(SupplyCacheItemFields)} FROM \"ddon_supply_cache_item\" WHERE \"cache_id\" = @cache_id ORDER BY \"slot\";";

    private static readonly string SqlInsertSupplyCacheItem =
        $"INSERT INTO \"ddon_supply_cache_item\" ({BuildQueryField(SupplyCacheItemFields)}) VALUES ({BuildQueryInsert(SupplyCacheItemFields)});";

    private static readonly string SqlUpdateSupplyCacheItem =
        "UPDATE \"ddon_supply_cache_item\" SET \"serialized_item\" = @serialized_item WHERE \"cache_id\" = @cache_id AND \"slot\" = @slot;";

    private static readonly string SqlDeleteSupplyCacheItem =
        "DELETE FROM \"ddon_supply_cache_item\" WHERE \"cache_id\" = @cache_id AND \"slot\" = @slot;";

    private static readonly string SqlDeleteSupplyCache =
        "DELETE FROM \"ddon_supply_cache\" WHERE \"id\" = @id;";

    private static readonly string SqlDeleteAllSupplyCaches =
        "DELETE FROM \"ddon_supply_cache\";";

    public override List<SupplyCache> SelectAllSupplyCaches(DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            List<SupplyCache> caches = [];
            ExecuteReader(connection, SqlSelectAllSupplyCaches, _ => { }, reader =>
            {
                while (reader.Read())
                {
                    caches.Add(ReadSupplyCache(reader));
                }
            });

            foreach (SupplyCache cache in caches)
            {
                cache.Items = SelectSupplyCacheItems(cache.Id, connection);
            }

            return caches;
        });
    }

    public override long InsertSupplyCache(SupplyCache cache, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            ExecuteNonQuery(connection, SqlInsertSupplyCache, command =>
            {
                AddParameter(command, "map_id", cache.MapId);
                AddParameter(command, "layer_no", cache.LayerNo);
                AddParameter(command, "group_id", cache.GroupId);
                AddParameter(command, "x", (float)cache.X);
                AddParameter(command, "y", cache.Y);
                AddParameter(command, "z", (float)cache.Z);
                AddParameter(command, "rotation", cache.Rotation);
                AddParameter(command, "set_id", 0U);
                AddParameter(command, "created", cache.Created);
                AddParameter(command, "updated", cache.Updated);
            }, out long cacheId, true);

            cache.Id = cacheId;
            cache.SetId = SupplyCache.MakeSetId(cacheId);
            ExecuteNonQuery(connection, "UPDATE \"ddon_supply_cache\" SET \"set_id\" = @set_id WHERE \"id\" = @id;", command =>
            {
                AddParameter(command, "set_id", cache.SetId);
                AddParameter(command, "id", cacheId);
            });
            return cacheId;
        });
    }

    public override bool UpdateSupplyCacheTimestamp(long cacheId, DateTime updated, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlUpdateSupplyCacheTimestamp, command =>
            {
                AddParameter(command, "updated", updated);
                AddParameter(command, "id", cacheId);
            }) == 1;
        });
    }

    public override bool UpdateSupplyCachePosition(SupplyCache cache, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlUpdateSupplyCachePosition, command =>
            {
                AddParameter(command, "x", (float)cache.X);
                AddParameter(command, "y", cache.Y);
                AddParameter(command, "z", (float)cache.Z);
                AddParameter(command, "rotation", cache.Rotation);
                AddParameter(command, "layer_no", cache.LayerNo);
                AddParameter(command, "group_id", cache.GroupId);
                AddParameter(command, "updated", cache.Updated);
                AddParameter(command, "id", cache.Id);
            }) == 1;
        });
    }

    public override bool UpsertSupplyCacheItem(long cacheId, ushort slot, SupplyCacheItemData itemData, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            string serialized = JsonSerializer.Serialize(itemData);
            if (ExecuteNonQuery(connection, SqlUpdateSupplyCacheItem, command =>
                {
                    AddParameter(command, "serialized_item", serialized);
                    AddParameter(command, "cache_id", cacheId);
                    AddParameter(command, "slot", slot);
                }) == 1)
            {
                return true;
            }

            return ExecuteNonQuery(connection, SqlInsertSupplyCacheItem, command =>
            {
                AddParameter(command, "cache_id", cacheId);
                AddParameter(command, "slot", slot);
                AddParameter(command, "serialized_item", serialized);
            }) == 1;
        });
    }

    public override bool DeleteSupplyCacheItem(long cacheId, ushort slot, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlDeleteSupplyCacheItem, command =>
            {
                AddParameter(command, "cache_id", cacheId);
                AddParameter(command, "slot", slot);
            }) == 1;
        });
    }

    public override bool DeleteSupplyCache(long cacheId, DbConnection? connectionIn = null)
    {
        return ExecuteQuerySafe(connectionIn, connection =>
        {
            return ExecuteNonQuery(connection, SqlDeleteSupplyCache, command =>
            {
                AddParameter(command, "id", cacheId);
            }) == 1;
        });
    }

    public override void DeleteAllSupplyCaches(DbConnection? connectionIn = null)
    {
        ExecuteQuerySafe(connectionIn, connection =>
        {
            ExecuteNonQuery(connection, SqlDeleteAllSupplyCaches, _ => { });
        });
    }

    private List<(ushort Slot, SupplyCacheItemData Item)> SelectSupplyCacheItems(long cacheId, DbConnection connection)
    {
        List<(ushort Slot, SupplyCacheItemData Item)> items = [];
        ExecuteReader(connection, SqlSelectSupplyCacheItems, command =>
        {
            AddParameter(command, "cache_id", cacheId);
        }, reader =>
        {
            while (reader.Read())
            {
                ushort slot = GetUInt16(reader, "slot");
                string serialized = GetString(reader, "serialized_item");
                SupplyCacheItemData? itemData = JsonSerializer.Deserialize<SupplyCacheItemData>(serialized);
                if (itemData != null)
                {
                    items.Add((slot, itemData));
                }
            }
        });
        return items;
    }

    private SupplyCache ReadSupplyCache(DbDataReader reader)
    {
        return new SupplyCache
        {
            Id = GetInt64(reader, "id"),
            MapId = GetUInt32(reader, "map_id"),
            LayerNo = GetByte(reader, "layer_no"),
            GroupId = GetUInt32(reader, "group_id"),
            X = reader.GetDouble(reader.GetOrdinal("x")),
            Y = reader.GetFloat(reader.GetOrdinal("y")),
            Z = reader.GetDouble(reader.GetOrdinal("z")),
            Rotation = reader.GetFloat(reader.GetOrdinal("rotation")),
            SetId = GetUInt32(reader, "set_id"),
            Created = GetDateTime(reader, "created"),
            Updated = GetDateTime(reader, "updated"),
        };
    }
}

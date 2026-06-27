CREATE TABLE IF NOT EXISTS "ddon_supply_cache"
(
    "id"         INTEGER PRIMARY KEY AUTOINCREMENT,
    "map_id"     INTEGER NOT NULL,
    "layer_no"   SMALLINT NOT NULL DEFAULT 0,
    "group_id"   INTEGER NOT NULL DEFAULT 0,
    "x"          REAL NOT NULL,
    "y"          REAL NOT NULL,
    "z"          REAL NOT NULL,
    "rotation"   REAL NOT NULL DEFAULT 0,
    "set_id"     INTEGER NOT NULL UNIQUE,
    "created"    DATETIME NOT NULL,
    "updated"    DATETIME NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_ddon_supply_cache_map_id" ON "ddon_supply_cache" ("map_id");

CREATE TABLE IF NOT EXISTS "ddon_supply_cache_item"
(
    "cache_id"         INTEGER NOT NULL,
    "slot"             SMALLINT NOT NULL,
    "serialized_item"  TEXT NOT NULL,
    CONSTRAINT "pk_ddon_supply_cache_item" PRIMARY KEY ("cache_id", "slot"),
    CONSTRAINT "fk_ddon_supply_cache_item_cache_id" FOREIGN KEY ("cache_id") REFERENCES "ddon_supply_cache" ("id") ON DELETE CASCADE
);

INSERT INTO "ddon_schedule_next" ("type", "timestamp") VALUES (27, strftime('%s', 'now', 'weekday 1', '-6 days', 'start of day', '+5 hours'));

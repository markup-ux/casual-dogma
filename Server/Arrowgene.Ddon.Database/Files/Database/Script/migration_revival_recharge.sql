CREATE TABLE IF NOT EXISTS "ddon_revival_recharge_pending"
(
    "id"            INTEGER PRIMARY KEY AUTOINCREMENT,
    "character_id"  INTEGER NOT NULL,
    "recharge_type" SMALLINT NOT NULL,
    "expires_at"    BIGINT NOT NULL,
    CONSTRAINT "fk_ddon_revival_recharge_pending_character_id" FOREIGN KEY ("character_id") REFERENCES "ddon_character" ("character_id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "idx_ddon_revival_recharge_pending_character_id"
    ON "ddon_revival_recharge_pending" ("character_id");

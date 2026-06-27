CREATE TABLE "temp_limit_break"
(
    "character_id"     INTEGER    NOT NULL,
    "item_uid"         TEXT       NOT NULL,
    "effect_id"        INTEGER    NOT NULL,
    "unk1"             INTEGER    NOT NULL,
    "effect_type"      SMALLINT    NOT NULL,
    "unk0"             SMALLINT    NOT NULL
);

INSERT INTO "temp_limit_break" SELECT 
    "character_id",
    "item_uid",
    "effect_1",
    "effect_2",
    1,
    0
FROM "ddon_equipment_limit_break";

DROP TABLE "ddon_equipment_limit_break";
DROP INDEX IF EXISTS "idx_ddon_equipment_limit_break_item_uid";

CREATE TABLE "ddon_equipment_limit_break"
(
    "character_id"     INTEGER    NOT NULL,
    "item_uid"         TEXT       NOT NULL,
    "effect_id"        INTEGER    NOT NULL,
    "unk1"             INTEGER    NOT NULL,
    "effect_type"      SMALLINT    NOT NULL,
    "unk0"             SMALLINT    NOT NULL,
    CONSTRAINT "pk_ddon_equipment_limit_break" PRIMARY KEY ("character_id", "item_uid", "effect_type"),
    CONSTRAINT "fk_ddon_equipment_limit_break_item_uid" FOREIGN KEY ("item_uid") REFERENCES "ddon_storage_item" ("item_uid") ON DELETE CASCADE,
    CONSTRAINT "fk_ddon_equipment_limit_break_character_id" FOREIGN KEY ("character_id") REFERENCES "ddon_character" ("character_id") ON DELETE CASCADE
);
CREATE INDEX "idx_ddon_equipment_limit_break_item_uid" ON "ddon_equipment_limit_break" ("item_uid");

INSERT INTO "ddon_equipment_limit_break" SELECT * FROM "temp_limit_break";

ALTER TABLE "ddon_reward_box" ADD COLUMN "reward_flags" INTEGER NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS "ddon_reward_box_item"
(
    "reward_box_item_id" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "uniq_reward_id"     INTEGER                           NOT NULL,
    "item_id"            INTEGER                           NOT NULL,
    "num"                INTEGER                           NOT NULL,
    "uid"                TEXT                              NOT NULL,
    "type"               INTEGER                           NOT NULL,
    "is_charge"          INTEGER                           NOT NULL DEFAULT 0,
    "is_help"            INTEGER                           NOT NULL DEFAULT 0,
    "select_group_id"    INTEGER                           NOT NULL DEFAULT 0,
    CONSTRAINT "fk_ddon_reward_box_item_uniq_reward_id"
        FOREIGN KEY ("uniq_reward_id")
        REFERENCES "ddon_reward_box" ("uniq_reward_id")
        ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "idx_ddon_reward_box_item_uniq_reward_id" ON "ddon_reward_box_item" ("uniq_reward_id");

CREATE TABLE IF NOT EXISTS "ddon_quest_period_first_clear"
(
    "character_common_id" INTEGER NOT NULL,
    "quest_type"          INTEGER NOT NULL,
    "quest_schedule_id"   INTEGER NOT NULL,
    CONSTRAINT "pk_ddon_quest_period_first_clear" PRIMARY KEY ("character_common_id", "quest_type", "quest_schedule_id"),
    CONSTRAINT "fk_ddon_quest_period_first_clear_character_common_id"
        FOREIGN KEY ("character_common_id")
        REFERENCES "ddon_character_common" ("character_common_id")
        ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "idx_ddon_quest_period_first_clear_quest_type" ON "ddon_quest_period_first_clear" ("quest_type");

INSERT INTO "ddon_quest_period_first_clear" ("character_common_id", "quest_type", "quest_schedule_id")
SELECT "character_common_id", 2, "quest_schedule_id"
FROM "ddon_world_quest_period_first_clear"
WHERE TRUE
ON CONFLICT DO NOTHING;

DROP TABLE IF EXISTS "ddon_world_quest_period_first_clear";

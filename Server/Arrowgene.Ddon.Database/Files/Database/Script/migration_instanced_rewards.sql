ALTER TABLE "ddon_reward_box_item" ADD COLUMN "is_instance" INTEGER NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS "ddon_reward_staged_item"
(
    "uid"                TEXT    PRIMARY KEY NOT NULL,
    "reward_box_item_id" INTEGER NOT NULL,
    "item_id"            INTEGER NOT NULL,
    "num"                INTEGER NOT NULL DEFAULT 1,
    "color"              INTEGER NOT NULL DEFAULT 0,
    "plus_value"         INTEGER NOT NULL DEFAULT 0,
    "safety_setting"     INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "fk_reward_staged_item_reward_box_item_id"
        FOREIGN KEY ("reward_box_item_id")
        REFERENCES "ddon_reward_box_item" ("reward_box_item_id")
        ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "idx_reward_staged_item_reward_box_item_id" ON "ddon_reward_staged_item" ("reward_box_item_id");

CREATE TABLE IF NOT EXISTS "ddon_reward_staged_item_crest"
(
    "uid"      TEXT    NOT NULL,
    "slot"     INTEGER NOT NULL,
    "crest_id" INTEGER NOT NULL,
    "level"    INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "pk_reward_staged_item_crest" PRIMARY KEY ("uid", "slot"),
    CONSTRAINT "fk_reward_staged_item_crest_uid"
        FOREIGN KEY ("uid")
        REFERENCES "ddon_reward_staged_item" ("uid")
        ON DELETE CASCADE
);

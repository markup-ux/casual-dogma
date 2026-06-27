CREATE TABLE IF NOT EXISTS "ddon_quest_delivery_progress"
(
    "character_common_id" INTEGER NOT NULL,
    "quest_schedule_id"   INTEGER NOT NULL,
    "item_id"             INTEGER NOT NULL,
    "amount_delivered"    INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "pk_ddon_quest_delivery_progress" PRIMARY KEY ("character_common_id", "quest_schedule_id", "item_id"),
    CONSTRAINT "fk_ddon_quest_delivery_progress_quest_progress"
        FOREIGN KEY ("character_common_id", "quest_schedule_id")
        REFERENCES "ddon_quest_progress" ("character_common_id", "quest_schedule_id")
        ON DELETE CASCADE
);

ALTER TABLE "ddon_reward_box" ADD COLUMN "is_repeat_reward" INTEGER NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS "ddon_world_quest_period_first_clear"
(
    "character_common_id" INTEGER NOT NULL,
    "quest_schedule_id"   INTEGER NOT NULL,
    CONSTRAINT "pk_ddon_world_quest_period_first_clear" PRIMARY KEY ("character_common_id", "quest_schedule_id"),
    CONSTRAINT "fk_ddon_world_quest_period_first_clear_character_common_id"
        FOREIGN KEY ("character_common_id")
        REFERENCES "ddon_character_common" ("character_common_id")
        ON DELETE CASCADE
);

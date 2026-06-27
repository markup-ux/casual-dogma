INSERT INTO "ddon_schedule_next" ("type", "timestamp")
VALUES (8, 0);

CREATE UNIQUE INDEX idx_quest_progress_identity ON "ddon_quest_progress" ("character_common_id", "quest_schedule_id");

DROP TABLE IF EXISTS ddon_priority_quests;
CREATE TABLE IF NOT EXISTS "ddon_priority_quests"
(
    "character_common_id" INTEGER NOT NULL,
    "quest_schedule_id"   INTEGER NOT NULL,
    CONSTRAINT "fk_priority_to_progress"
        FOREIGN KEY ("character_common_id", "quest_schedule_id")
        REFERENCES "ddon_quest_progress" ("character_common_id", "quest_schedule_id")
        ON DELETE CASCADE,
    CONSTRAINT "fk_ddon_priority_quests_character_common_id"
        FOREIGN KEY ("character_common_id")
        REFERENCES "ddon_character_common" ("character_common_id")
        ON DELETE CASCADE,
    CONSTRAINT "uq_character_common_id_quest_schedule_id" UNIQUE ("character_common_id", "quest_schedule_id")
);

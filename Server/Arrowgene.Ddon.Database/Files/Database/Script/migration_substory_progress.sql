CREATE TABLE IF NOT EXISTS "ddon_substory_progress" (
    "character_id"       INTEGER NOT NULL,
    "substory_group_id"  INTEGER NOT NULL,
    "sequence_step"      INTEGER NOT NULL DEFAULT 0,
    "is_complete"        INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "pk_ddon_substory_progress" PRIMARY KEY ("character_id", "substory_group_id"),
    CONSTRAINT "fk_ddon_substory_progress_character_id" FOREIGN KEY ("character_id") REFERENCES "ddon_character"("character_id") ON DELETE CASCADE
);

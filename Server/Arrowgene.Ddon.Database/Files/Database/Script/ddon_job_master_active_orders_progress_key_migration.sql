CREATE TABLE IF NOT EXISTS "ddon_job_master_active_orders_progress_new"
(
    "character_id" INTEGER NOT NULL,
    "job_id"       INTEGER NOT NULL,
    "release_type" INTEGER NOT NULL,
    "release_id"   INTEGER NOT NULL,
    "target_id"    INTEGER NOT NULL,
    "condition"    INTEGER NOT NULL,
    "target_rank"  INTEGER NOT NULL,
    "target_num"   INTEGER NOT NULL,
    "current_num"  INTEGER NOT NULL,
    CONSTRAINT "pk_ddon_job_master_active_orders_progress" PRIMARY KEY ("character_id", "job_id", "release_type", "release_id", "condition", "target_id", "target_rank"),
    CONSTRAINT "fk_ddon_job_master_active_orders_progress" FOREIGN KEY ("character_id", "job_id", "release_type", "release_id") REFERENCES "ddon_job_master_active_orders" ("character_id", "job_id", "release_type", "release_id") ON DELETE CASCADE,
    CONSTRAINT "fk_ddon_job_master_active_orders_progress_character_id" FOREIGN KEY ("character_id") REFERENCES "ddon_character" ("character_id") ON DELETE CASCADE
);

INSERT INTO "ddon_job_master_active_orders_progress_new"
    ("character_id", "job_id", "release_type", "release_id", "target_id", "condition", "target_rank", "target_num", "current_num")
SELECT
    "character_id",
    "job_id",
    "release_type",
    "release_id",
    "target_id",
    "condition",
    "target_rank",
    MAX("target_num") AS "target_num",
    MAX("current_num") AS "current_num"
FROM "ddon_job_master_active_orders_progress"
GROUP BY "character_id", "job_id", "release_type", "release_id", "target_id", "condition", "target_rank";

DROP TABLE "ddon_job_master_active_orders_progress";
ALTER TABLE "ddon_job_master_active_orders_progress_new" RENAME TO "ddon_job_master_active_orders_progress";

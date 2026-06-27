CREATE TABLE "ddon_character_profile"
(
    "character_common_id"    INTEGER PRIMARY KEY NOT NULL,
    "background_id"          SMALLINT            NOT NULL,
    "title_uid"              INTEGER             NOT NULL,
    "title_index"            INTEGER             NOT NULL,
    "motion_id"              SMALLINT            NOT NULL,
    "motion_frame_no"        INTEGER             NOT NULL,
    "comment"                TEXT,                
    CONSTRAINT "fk_ddon_character_profile_character_id" FOREIGN KEY ("character_common_id") REFERENCES "ddon_character_common" ("character_common_id") ON DELETE CASCADE
);

INSERT INTO "ddon_character_profile" (character_common_id, background_id, title_uid, title_index, motion_id, motion_frame_no, comment)
SELECT 
	character_common_id,
	background_id, 
	title_uid,
	title_index,
	motion_id,
	motion_frame_no,
	'' as comment
FROM "ddon_character_arisen_profile"
NATURAL JOIN "ddon_character";

INSERT INTO "ddon_character_profile" (character_common_id, background_id, title_uid, title_index, motion_id, motion_frame_no, comment)
SELECT 
	character_common_id,
	0 as background_id, 
	0 as title_uid,
	0 as title_index,
	0 as motion_id,
	0 as motion_frame_no,
	'' as comment
FROM "ddon_pawn";

CREATE TABLE IF NOT EXISTS "ddon_rental_pawn"
(
    "hiring_character_id"         INTEGER       NOT NULL,
    "pawn_id"                     INTEGER       NOT NULL,
    "data"                        BLOB          NOT NULL,
    "data_size"                   INTEGER       NOT NULL,
    "adventure_count"             TINYINT       NOT NULL,
    "craft_count"                 TINYINT       NOT NULL,
    "kill_count"                  INTEGER       NOT NULL,
    CONSTRAINT "pk_ddon_rental_pawn" PRIMARY KEY ("hiring_character_id", "pawn_id"),
    CONSTRAINT "fk_ddon_rental_pawn_hiring_character_id" FOREIGN KEY ("hiring_character_id") REFERENCES "ddon_character" ("character_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "ddon_rental_pawn_feedback"
(
    "hiring_character_id"         INTEGER       NOT NULL,
    "pawn_id"                     INTEGER       NOT NULL,
    "hire_date"                   DATETIME      NOT NULL,
    "return_date"                 DATETIME      NOT NULL,
    "adventure_count"             TINYINT       NOT NULL,
    "craft_count"                 TINYINT       NOT NULL,
    "kill_count"                  INTEGER       NOT NULL,
    "appearance_score"            TINYINT,
    "appearance_comment"          TINYINT,
    "combat_score"                TINYINT,
    "combat_comment"              TINYINT,
    "craft_score"                 TINYINT,
    "craft_comment"               TINYINT,
    CONSTRAINT "fk_ddon_rental_pawn_feedback_pawn_id" FOREIGN KEY ("pawn_id") REFERENCES "ddon_pawn" ("pawn_id") ON DELETE CASCADE
);

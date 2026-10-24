-- Apply once to the legacy osu_lazer_api schema before running stable score submission.
ALTER TABLE scores
    ADD COLUMN classic_total_score_without_mods BIGINT NOT NULL DEFAULT 0,
    ADD INDEX ix_scores_type_processed_id (type, processed, id);

-- Cover art is published on the beatmap set only; the fetcher copies it onto each difficulty.
ALTER TABLE beatmaps
    ADD COLUMN covers JSON NULL AFTER beatmap_status;
ALTER TABLE beatmapsets
    ADD COLUMN covers JSON NULL AFTER beatmap_status;
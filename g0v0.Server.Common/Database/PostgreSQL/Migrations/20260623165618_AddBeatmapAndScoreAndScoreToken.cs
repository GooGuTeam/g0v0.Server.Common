using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1861

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
    /// <inheritdoc />
#pragma warning disable MA0048
    public partial class AddBeatmapAndScoreAndScoreToken : Migration
#pragma warning restore MA0048
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beatmaps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    beatmap_set_id = table.Column<long>(type: "bigint", nullable: false),
                    url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    checksum = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    max_combo = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    total_length = table.Column<int>(type: "integer", nullable: false),
                    difficulty_rating = table.Column<double>(type: "double precision", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approach_rate = table.Column<float>(type: "real", nullable: false),
                    circle_size = table.Column<float>(type: "real", nullable: false),
                    hp_drain_rate = table.Column<float>(type: "real", nullable: false),
                    overall_difficulty = table.Column<float>(type: "real", nullable: false),
                    bpm = table.Column<float>(type: "real", nullable: false),
                    circles_count = table.Column<int>(type: "integer", nullable: false),
                    sliders_count = table.Column<int>(type: "integer", nullable: false),
                    spinners_count = table.Column<int>(type: "integer", nullable: false),
                    hit_length = table.Column<int>(type: "integer", nullable: false),
                    mapper_id = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_update = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    beatmap_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beatmaps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scores",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    beatmap_id = table.Column<int>(type: "integer", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    accuracy = table.Column<double>(type: "double precision", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    max_combo = table.Column<int>(type: "integer", nullable: false),
                    has_replay = table.Column<bool>(type: "boolean", nullable: false),
                    total_score = table.Column<int>(type: "integer", nullable: false),
                    total_score_without_mods = table.Column<int>(type: "integer", nullable: false),
                    classic_total_score = table.Column<long>(type: "bigint", nullable: false),
                    classic_total_score_without_mods = table.Column<long>(type: "bigint", nullable: false),
                    statistics = table.Column<string>(type: "jsonb", nullable: false),
                    maximum_statistics = table.Column<string>(type: "jsonb", nullable: false),
                    mods = table.Column<string>(type: "jsonb", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    beatmap_checksum = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false),
                    ranked = table.Column<bool>(type: "boolean", nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    pp = table.Column<double>(type: "double precision", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: true),
                    playlist_item_id = table.Column<long>(type: "bigint", nullable: true),
                    preserve = table.Column<bool>(type: "boolean", nullable: false),
                    client_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: string.Empty)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "score_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    score_id = table.Column<long>(type: "bigint", nullable: true),
                    ruleset_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    beatmap_id = table.Column<int>(type: "integer", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: true),
                    playlist_item_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    client_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_score_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_score_tokens_beatmaps_beatmap_id",
                        column: x => x.beatmap_id,
                        principalTable: "beatmaps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_score_tokens_scores_score_id",
                        column: x => x.score_id,
                        principalTable: "scores",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_score_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_score_tokens_beatmap_id",
                table: "score_tokens",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_tokens_playlist_item_id_room_id",
                table: "score_tokens",
                columns: new[] { "playlist_item_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "ix_score_tokens_score_id",
                table: "score_tokens",
                column: "score_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_tokens_user_id_playlist_item_id",
                table: "score_tokens",
                columns: new[] { "user_id", "playlist_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_scores_beatmap_checksum",
                table: "scores",
                column: "beatmap_checksum");

            migrationBuilder.CreateIndex(
                name: "ix_scores_beatmap_id",
                table: "scores",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "ix_scores_mode",
                table: "scores",
                column: "mode");

            migrationBuilder.CreateIndex(
                name: "ix_scores_user_id",
                table: "scores",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_scores_user_id_mode_ended_at_id",
                table: "scores",
                columns: new[] { "user_id", "mode", "ended_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_scores_user_id_mode_pp_id",
                table: "scores",
                columns: new[] { "user_id", "mode", "pp", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "score_tokens");

            migrationBuilder.DropTable(
                name: "beatmaps");

            migrationBuilder.DropTable(
                name: "scores");
        }
    }
#pragma warning restore CA1861

}
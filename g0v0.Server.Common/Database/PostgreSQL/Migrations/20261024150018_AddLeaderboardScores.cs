// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboardScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beatmap_playcounts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    beatmap_id = table.Column<int>(type: "integer", nullable: false),
                    playcount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beatmap_playcounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "best_scores",
                columns: table => new
                {
                    score_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    beatmap_id = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    pp = table.Column<float>(type: "real", nullable: false),
                    accuracy = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_best_scores", x => x.score_id);
                    table.ForeignKey(
                        name: "fk_best_scores_scores_score_id",
                        column: x => x.score_id,
                        principalTable: "scores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leaderboard_scores",
                columns: table => new
                {
                    score_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    beatmap_id = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    total_score = table.Column<long>(type: "bigint", nullable: false),
                    mods = table.Column<string>(type: "jsonb", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leaderboard_scores", x => x.score_id);
                    table.ForeignKey(
                        name: "fk_leaderboard_scores_scores_score_id",
                        column: x => x.score_id,
                        principalTable: "scores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "monthly_playcounts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monthly_playcounts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_beatmap_playcounts_beatmap_id",
                table: "beatmap_playcounts",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "ix_beatmap_playcounts_playcount_id",
                table: "beatmap_playcounts",
                columns: ["playcount", "id"]);

            migrationBuilder.CreateIndex(
                name: "ix_beatmap_playcounts_user_id",
                table: "beatmap_playcounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_best_scores_beatmap_id",
                table: "best_scores",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "ix_best_scores_gamemode",
                table: "best_scores",
                column: "mode");

            migrationBuilder.CreateIndex(
                name: "ix_best_scores_mode_pp_score",
                table: "best_scores",
                columns: ["mode", "pp", "score_id"]);

            migrationBuilder.CreateIndex(
                name: "ix_best_scores_user_id",
                table: "best_scores",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_best_scores_user_mode_pp_score",
                table: "best_scores",
                columns: ["user_id", "mode", "pp", "score_id"]);

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_scores_beatmap_id",
                table: "leaderboard_scores",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_scores_beatmap_mode_score",
                table: "leaderboard_scores",
                columns: ["beatmap_id", "mode", "total_score"]);

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_scores_gamemode",
                table: "leaderboard_scores",
                column: "mode");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_scores_user_id",
                table: "leaderboard_scores",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_scores_user_mode_score",
                table: "leaderboard_scores",
                columns: ["user_id", "mode", "score_id"]);

            migrationBuilder.CreateIndex(
                name: "ix_monthly_playcounts_month",
                table: "monthly_playcounts",
                column: "month");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_playcounts_user_id",
                table: "monthly_playcounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_playcounts_year",
                table: "monthly_playcounts",
                column: "year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beatmap_playcounts");

            migrationBuilder.DropTable(
                name: "best_scores");

            migrationBuilder.DropTable(
                name: "leaderboard_scores");

            migrationBuilder.DropTable(
                name: "monthly_playcounts");
        }
    }
}
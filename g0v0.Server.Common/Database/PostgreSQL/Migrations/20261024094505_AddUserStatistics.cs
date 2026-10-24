// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_statistics",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    count_100 = table.Column<long>(type: "bigint", nullable: false),
                    count_300 = table.Column<long>(type: "bigint", nullable: false),
                    count_50 = table.Column<long>(type: "bigint", nullable: false),
                    count_miss = table.Column<long>(type: "bigint", nullable: false),
                    pp = table.Column<double>(type: "double precision", nullable: false),
                    ranked_score = table.Column<long>(type: "bigint", nullable: false),
                    hit_accuracy = table.Column<double>(type: "double precision", nullable: false),
                    total_score = table.Column<long>(type: "bigint", nullable: false),
                    total_hits = table.Column<long>(type: "bigint", nullable: false),
                    maximum_combo = table.Column<int>(type: "integer", nullable: false),
                    play_count = table.Column<int>(type: "integer", nullable: false),
                    play_time = table.Column<long>(type: "bigint", nullable: false),
                    replays_watched_by_others = table.Column<int>(type: "integer", nullable: false),
                    is_ranked = table.Column<bool>(type: "boolean", nullable: false),
                    grade_ss = table.Column<int>(type: "integer", nullable: false),
                    grade_ssh = table.Column<int>(type: "integer", nullable: false),
                    grade_s = table.Column<int>(type: "integer", nullable: false),
                    grade_sh = table.Column<int>(type: "integer", nullable: false),
                    grade_a = table.Column<int>(type: "integer", nullable: false),
                    level_current = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_statistics", x => new { x.user_id, x.mode });
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_statistics_mode_pp",
                table: "user_statistics",
                columns: ["mode", "pp"]);

            migrationBuilder.CreateIndex(
                name: "ix_user_statistics_user_id",
                table: "user_statistics",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_statistics");
        }
    }
}
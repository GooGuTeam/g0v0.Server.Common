// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatmapFeedbackAndScreenshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beatmap_ratings",
                columns: table => new
                {
                    beatmap_set_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beatmap_ratings", x => new { x.beatmap_set_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_beatmap_ratings_beatmapsets_beatmap_set_id",
                        column: x => x.beatmap_set_id,
                        principalTable: "beatmapsets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_beatmap_ratings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favourite_beatmapset",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    beatmap_set_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favourite_beatmapset", x => new { x.user_id, x.beatmap_set_id });
                    table.ForeignKey(
                        name: "fk_favourite_beatmapset_beatmapsets_beatmap_set_id",
                        column: x => x.beatmap_set_id,
                        principalTable: "beatmapsets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favourite_beatmapset_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "screenshots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sha256_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hits = table.Column<int>(type: "integer", nullable: false),
                    last_access = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screenshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_beatmap_ratings_user_id",
                table: "beatmap_ratings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_favourite_beatmapset_beatmap_set_id",
                table: "favourite_beatmapset",
                column: "beatmap_set_id");

            migrationBuilder.CreateIndex(
                name: "ix_screenshots_hits_timestamp",
                table: "screenshots",
                columns: ["hits", "timestamp"]);

            migrationBuilder.CreateIndex(
                name: "ix_screenshots_sha256_hash",
                table: "screenshots",
                column: "sha256_hash");

            migrationBuilder.CreateIndex(
                name: "ix_screenshots_user_id",
                table: "screenshots",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_screenshots_user_id_sha256_hash",
                table: "screenshots",
                columns: ["user_id", "sha256_hash"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beatmap_ratings");

            migrationBuilder.DropTable(
                name: "favourite_beatmapset");

            migrationBuilder.DropTable(
                name: "screenshots");
        }
    }
}
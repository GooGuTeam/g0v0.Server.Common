using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
#pragma warning disable MA0048

    /// <inheritdoc />
    public partial class AddBeatmapSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "required_mods",
                table: "playlists",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "allowed_mods",
                table: "playlists",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<int>(
                name: "beatmap_set_id",
                table: "beatmaps",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "beatmapsets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<int>(type: "integer", nullable: false),
                    covers = table.Column<string>(type: "jsonb", nullable: false),
                    artist = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    artist_unicode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title_unicode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nsfw = table.Column<bool>(type: "boolean", nullable: false),
                    preview_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    spotlight = table.Column<bool>(type: "boolean", nullable: false),
                    track_id = table.Column<int>(type: "integer", nullable: true),
                    creator = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    video = table.Column<bool>(type: "boolean", nullable: false),
                    genre = table.Column<string>(type: "jsonb", nullable: false),
                    language = table.Column<string>(type: "jsonb", nullable: false),
                    current_nominations = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    bpm = table.Column<double>(type: "double precision", nullable: false),
                    storyboard = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ranked_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tags = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beatmapsets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_beatmaps_beatmap_set_id",
                table: "beatmaps",
                column: "beatmap_set_id");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_artist",
                table: "beatmapsets",
                column: "artist");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_artist_unicode",
                table: "beatmapsets",
                column: "artist_unicode");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_beatmap_genre",
                table: "beatmapsets",
                column: "genre");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_beatmap_language",
                table: "beatmapsets",
                column: "language");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_beatmap_status",
                table: "beatmapsets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_creator",
                table: "beatmapsets",
                column: "creator");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_id",
                table: "beatmapsets",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_last_updated",
                table: "beatmapsets",
                column: "last_updated");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_ranked_date",
                table: "beatmapsets",
                column: "ranked_date");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_storyboard",
                table: "beatmapsets",
                column: "storyboard");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_submitted_date",
                table: "beatmapsets",
                column: "submitted_date");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_title",
                table: "beatmapsets",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_title_unicode",
                table: "beatmapsets",
                column: "title_unicode");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_track_id",
                table: "beatmapsets",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_user_id",
                table: "beatmapsets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_beatmapsets_video",
                table: "beatmapsets",
                column: "video");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beatmapsets");

            migrationBuilder.DropIndex(
                name: "ix_beatmaps_beatmap_set_id",
                table: "beatmaps");

            migrationBuilder.AlterColumn<string>(
                name: "required_mods",
                table: "playlists",
                type: "jsonb",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "allowed_mods",
                table: "playlists",
                type: "jsonb",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "beatmap_set_id",
                table: "beatmaps",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
#pragma warning disable MA0048

    /// <inheritdoc />
    public partial class AddMultiplayerRoomEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_attempts = table.Column<int>(type: "integer", nullable: true),
                    participant_count = table.Column<int>(type: "integer", nullable: false),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    queue_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    auto_skip = table.Column<bool>(type: "boolean", nullable: false),
                    auto_start_duration = table.Column<int>(type: "integer", nullable: false),
                    host_id = table.Column<int>(type: "integer", nullable: true),
                    password = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    max_participants = table.Column<byte>(type: "smallint", nullable: true),
                    tournament_mode = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rooms", x => x.id);
                    table.ForeignKey(
                        name: "fk_rooms_users_host_id",
                        column: x => x.host_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "playlists",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<long>(type: "bigint", nullable: false),
                    beatmap_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    ruleset_id = table.Column<int>(type: "integer", nullable: false),
                    allowed_mods = table.Column<string>(type: "jsonb", nullable: false),
                    required_mods = table.Column<string>(type: "jsonb", nullable: false),
                    freestyle = table.Column<bool>(type: "boolean", nullable: false),
                    expired = table.Column<bool>(type: "boolean", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    playlist_order = table.Column<int>(type: "integer", nullable: false),
                    played_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    win_condition = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playlists", x => x.id);
                    table.ForeignKey(
                        name: "fk_playlists_beatmaps_beatmap_id",
                        column: x => x.beatmap_id,
                        principalTable: "beatmaps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_playlists_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_playlists_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_participated_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_participated_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_room_participated_users_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_room_participated_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_playlists_beatmap_id",
                table: "playlists",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlists_owner_id",
                table: "playlists",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlists_room_id",
                table: "playlists",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_room_participated_users_room_id",
                table: "room_participated_users",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_room_participated_users_user_id",
                table: "room_participated_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_category",
                table: "rooms",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_host_id",
                table: "rooms",
                column: "host_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_id",
                table: "rooms",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_name",
                table: "rooms",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "playlists");

            migrationBuilder.DropTable(
                name: "room_participated_users");

            migrationBuilder.DropTable(
                name: "rooms");
        }
    }

#pragma warning restore MA0048
}
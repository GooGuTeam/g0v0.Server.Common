using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
#pragma warning disable MA0048

    /// <inheritdoc />
    public partial class AddChatEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_channels",
                columns: table => new
                {
                    channel_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_channels", x => x.channel_id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    message_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    sender_id = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_messages", x => x.message_id);
                    table.ForeignKey(
                        name: "fk_chat_messages_chat_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "chat_channels",
                        principalColumn: "channel_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_messages_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_silence_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    banned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_silence_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_silence_users_chat_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "chat_channels",
                        principalColumn: "channel_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_silence_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_channels_description",
                table: "chat_channels",
                column: "description");

            migrationBuilder.CreateIndex(
                name: "ix_chat_channels_name",
                table: "chat_channels",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_chat_channels_type",
                table: "chat_channels",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_channel_id",
                table: "chat_messages",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_sender_id",
                table: "chat_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_timestamp",
                table: "chat_messages",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_chat_silence_users_banned_at",
                table: "chat_silence_users",
                column: "banned_at");

            migrationBuilder.CreateIndex(
                name: "ix_chat_silence_users_channel_id",
                table: "chat_silence_users",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_silence_users_until",
                table: "chat_silence_users",
                column: "until");

            migrationBuilder.CreateIndex(
                name: "ix_chat_silence_users_user_id",
                table: "chat_silence_users",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_silence_users");

            migrationBuilder.DropTable(
                name: "chat_channels");
        }
    }
}
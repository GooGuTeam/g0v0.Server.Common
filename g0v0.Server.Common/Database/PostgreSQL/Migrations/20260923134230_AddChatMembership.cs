using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
#pragma warning disable MA0048

    /// <inheritdoc />
    public partial class AddChatMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_message_id",
                table: "chat_channels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "moderated",
                table: "chat_channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "chat_user_channels",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    hidden = table.Column<bool>(type: "boolean", nullable: false),
                    last_read_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_user_channels", x => new { x.user_id, x.channel_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_user_channels_channel_id",
                table: "chat_user_channels",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_user_channels_hidden",
                table: "chat_user_channels",
                column: "hidden");

            // Existing channels predate the last_message_id column; seed it from
            // the messages that are already stored.
            migrationBuilder.Sql(@"
                UPDATE chat_channels AS channel
                SET last_message_id = latest.message_id
                FROM (
                    SELECT channel_id, MAX(message_id) AS message_id
                    FROM chat_messages
                    GROUP BY channel_id
                ) AS latest
                WHERE latest.channel_id = channel.channel_id;");

            // PM conversations created before this migration have no membership
            // rows yet; recreate them from the pm_{user1}_{user2} channel names so
            // that the conversations stay visible in the channel list.
            migrationBuilder.Sql(@"
                INSERT INTO chat_user_channels (user_id, channel_id, hidden, last_read_id)
                SELECT memberships.user_id, memberships.channel_id, FALSE, NULL
                FROM (
                    SELECT split_part(name, '_', 2)::integer AS user_id, channel_id
                    FROM chat_channels
                    WHERE type = 'PM' AND name ~ '^pm_[0-9]+_[0-9]+$'
                    UNION
                    SELECT split_part(name, '_', 3)::integer AS user_id, channel_id
                    FROM chat_channels
                    WHERE type = 'PM' AND name ~ '^pm_[0-9]+_[0-9]+$'
                ) AS memberships
                ON CONFLICT (user_id, channel_id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_user_channels");

            migrationBuilder.DropColumn(
                name: "last_message_id",
                table: "chat_channels");

            migrationBuilder.DropColumn(
                name: "moderated",
                table: "chat_channels");
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
#pragma warning disable MA0048

    /// <inheritdoc />
    public partial class AddChatMessageUuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "uuid",
                table: "chat_messages",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uuid",
                table: "chat_messages");
        }
    }
}
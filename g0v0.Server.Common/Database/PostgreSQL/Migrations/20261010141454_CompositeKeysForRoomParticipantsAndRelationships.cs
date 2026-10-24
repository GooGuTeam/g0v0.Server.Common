using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class CompositeKeysForRoomParticipantsAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_room_participated_users",
                table: "room_participated_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_relationship",
                table: "relationship");

            migrationBuilder.DropColumn(
                name: "id",
                table: "room_participated_users");

            migrationBuilder.DropColumn(
                name: "id",
                table: "relationship");

            migrationBuilder.AddPrimaryKey(
                name: "pk_room_participated_users",
                table: "room_participated_users",
                columns: ["room_id", "user_id"]);

            migrationBuilder.AddPrimaryKey(
                name: "pk_relationship",
                table: "relationship",
                columns: ["user_id", "target_id", "type"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_room_participated_users",
                table: "room_participated_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_relationship",
                table: "relationship");

            migrationBuilder.AddColumn<long>(
                name: "id",
                table: "room_participated_users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "relationship",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_room_participated_users",
                table: "room_participated_users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_relationship",
                table: "relationship",
                column: "id");
        }
    }
}
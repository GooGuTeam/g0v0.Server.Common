// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "g0v0_playmode",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "playmode",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "support_level",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing accounts get their creation timestamp set to the migration time instead of
            // the year 1, which the column default would otherwise leave behind.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "join_date",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE users SET join_date = now() WHERE join_date IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "join_date",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            // Several NULL addresses are allowed by the unique index: accounts created before
            // registration stored one keep theirs unset.
            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "g0v0_playmode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "join_date",
                table: "users");

            migrationBuilder.DropColumn(
                name: "playmode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "support_level",
                table: "users");
        }
    }
}
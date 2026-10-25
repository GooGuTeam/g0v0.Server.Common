// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations;

/// <inheritdoc />
public partial class AddRoomSource : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "source",
            table: "rooms",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Realtime");

        migrationBuilder.CreateIndex(
            name: "ix_rooms_source",
            table: "rooms",
            column: "source");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_rooms_source",
            table: "rooms");

        migrationBuilder.DropColumn(
            name: "source",
            table: "rooms");
    }
}
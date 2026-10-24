// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations;

/// <inheritdoc />
public partial class AddStableLoginSessions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "pw_bcrypt",
            table: "users",
            type: "character varying(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "trusted_devices",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<int>(type: "integer", nullable: false),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                user_agent = table.Column<string>(type: "text", nullable: false),
                client_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                device_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_trusted_devices", x => x.id);
                table.ForeignKey(
                    name: "fk_trusted_devices_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "user_login_log",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<int>(type: "integer", nullable: false),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                login_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                country_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                city_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                latitude = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                longitude = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                asn = table.Column<int>(type: "integer", nullable: true),
                organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                login_success = table.Column<bool>(type: "boolean", nullable: false),
                login_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_login_log", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "login_sessions",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<int>(type: "integer", nullable: false),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                user_agent = table.Column<string>(type: "text", nullable: true),
                is_verified = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                device_id = table.Column<int>(type: "integer", nullable: true),
                token_id = table.Column<int>(type: "integer", nullable: true),
                is_new_device = table.Column<bool>(type: "boolean", nullable: false),
                device_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                verification_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_login_sessions", x => x.id);
                table.ForeignKey(
                    name: "fk_login_sessions_oauth_tokens_token_id",
                    column: x => x.token_id,
                    principalTable: "oauth_tokens",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_login_sessions_trusted_devices_device_id",
                    column: x => x.device_id,
                    principalTable: "trusted_devices",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_login_sessions_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ix_login_sessions_device_id",
            table: "login_sessions",
            column: "device_id");

        migrationBuilder.CreateIndex(
            name: "ix_login_sessions_token_id",
            table: "login_sessions",
            column: "token_id");

        migrationBuilder.CreateIndex(
            name: "ix_login_sessions_user_id",
            table: "login_sessions",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_trusted_devices_user_id",
            table: "trusted_devices",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_login_log_ip_address",
            table: "user_login_log",
            column: "ip_address");

        migrationBuilder.CreateIndex(
            name: "ix_user_login_log_user_id",
            table: "user_login_log",
            column: "user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "login_sessions");

        migrationBuilder.DropTable(
            name: "user_login_log");

        migrationBuilder.DropTable(
            name: "trusted_devices");

        migrationBuilder.DropColumn(
            name: "pw_bcrypt",
            table: "users");
    }
}
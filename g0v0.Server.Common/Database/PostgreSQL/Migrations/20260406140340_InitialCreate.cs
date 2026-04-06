using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace g0v0.Server.Common.Database.PostgreSQL.Migrations
{
    /// <inheritdoc />
#pragma warning disable MA0048
    public partial class InitialCreate : Migration
#pragma warning restore MA0048
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatar_url = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_bot = table.Column<bool>(type: "boolean", nullable: false),
                    is_supporter = table.Column<bool>(type: "boolean", nullable: false),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    last_visit = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pm_friends_only = table.Column<bool>(type: "boolean", nullable: false),
                    profile_colour = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oauth_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    access_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    token_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    refresh_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oauth_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_oauth_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_oauth_tokens_access_token",
                table: "oauth_tokens",
                column: "access_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oauth_tokens_client_id",
                table: "oauth_tokens",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_oauth_tokens_expires_at",
                table: "oauth_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_oauth_tokens_refresh_token",
                table: "oauth_tokens",
                column: "refresh_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oauth_tokens_refresh_token_expires_at",
                table: "oauth_tokens",
                column: "refresh_token_expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_oauth_tokens_user_id",
                table: "oauth_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_country_code",
                table: "users",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oauth_tokens");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
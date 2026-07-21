using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class HardenClientPortalIdentitySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                schema: "cloud",
                table: "client_portal_users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_recovery_code_used_at_utc",
                schema: "cloud",
                table: "client_portal_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "last_totp_step",
                schema: "cloud",
                table: "client_portal_users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_protected_totp_secret",
                schema: "cloud",
                table: "client_portal_users",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_recovery_code_hashes_json",
                schema: "cloud",
                table: "client_portal_users",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "protected_totp_secret",
                schema: "cloud",
                table: "client_portal_users",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recovery_code_hashes_json",
                schema: "cloud",
                table: "client_portal_users",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "recovery_codes_generated_at_utc",
                schema: "cloud",
                table: "client_portal_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "security_version",
                schema: "cloud",
                table: "client_portal_users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "totp_enabled_at_utc",
                schema: "cloud",
                table: "client_portal_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "totp_enrollment_started_at_utc",
                schema: "cloud",
                table: "client_portal_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "client_portal_mail_deliveries",
                schema: "cloud",
                columns: table => new
                {
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    text_body = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_attempted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_portal_mail_deliveries", x => x.delivery_id);
                });

            migrationBuilder.CreateTable(
                name: "client_portal_password_resets",
                schema: "cloud",
                columns: table => new
                {
                    password_reset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_portal_password_resets", x => x.password_reset_id);
                });

            migrationBuilder.CreateTable(
                name: "client_portal_sessions",
                schema: "cloud",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    security_version = table.Column<int>(type: "integer", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    previous_refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_activity_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idle_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_portal_sessions", x => x.session_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_mail_deliveries_client_id",
                schema: "cloud",
                table: "client_portal_mail_deliveries",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_mail_deliveries_status_lease_expiry",
                schema: "cloud",
                table: "client_portal_mail_deliveries",
                columns: new[] { "status", "lease_expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_mail_deliveries_status_next_attempt",
                schema: "cloud",
                table: "client_portal_mail_deliveries",
                columns: new[] { "status", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_password_resets_user_expiry",
                schema: "cloud",
                table: "client_portal_password_resets",
                columns: new[] { "user_id", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_client_portal_password_resets_token_hash",
                schema: "cloud",
                table: "client_portal_password_resets",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_sessions_idle_expiry",
                schema: "cloud",
                table: "client_portal_sessions",
                column: "idle_expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_sessions_previous_refresh_hash",
                schema: "cloud",
                table: "client_portal_sessions",
                column: "previous_refresh_token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_sessions_user_revoked",
                schema: "cloud",
                table: "client_portal_sessions",
                columns: new[] { "user_id", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_client_portal_sessions_refresh_hash",
                schema: "cloud",
                table: "client_portal_sessions",
                column: "refresh_token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_portal_mail_deliveries",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "client_portal_password_resets",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "client_portal_sessions",
                schema: "cloud");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "last_recovery_code_used_at_utc",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "last_totp_step",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "pending_protected_totp_secret",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "pending_recovery_code_hashes_json",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "protected_totp_secret",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "recovery_code_hashes_json",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "recovery_codes_generated_at_utc",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "security_version",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "totp_enabled_at_utc",
                schema: "cloud",
                table: "client_portal_users");

            migrationBuilder.DropColumn(
                name: "totp_enrollment_started_at_utc",
                schema: "cloud",
                table: "client_portal_users");
        }
    }
}

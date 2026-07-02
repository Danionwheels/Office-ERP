using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    public partial class AddClientPortalIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_portal_invitations",
                schema: "cloud",
                columns: table => new
                {
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    full_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    invited_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_portal_invitations", x => x.invitation_id);
                });

            migrationBuilder.CreateTable(
                name: "client_portal_users",
                schema: "cloud",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    full_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_portal_users", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_invitations_client_email_status",
                schema: "cloud",
                table: "client_portal_invitations",
                columns: new[] { "client_id", "normalized_email", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_client_portal_invitations_token_hash",
                schema: "cloud",
                table: "client_portal_invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_users_client_status",
                schema: "cloud",
                table: "client_portal_users",
                columns: new[] { "client_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_client_portal_users_client_email",
                schema: "cloud",
                table: "client_portal_users",
                columns: new[] { "client_id", "normalized_email" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_portal_invitations",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "client_portal_users",
                schema: "cloud");
        }
    }
}

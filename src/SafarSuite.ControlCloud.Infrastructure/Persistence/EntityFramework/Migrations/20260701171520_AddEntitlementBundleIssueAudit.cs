using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitlementBundleIssueAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_installations",
                schema: "cloud",
                columns: table => new
                {
                    installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    registered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_bundle_issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    latest_entitlement_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_installations", x => x.installation_id);
                });

            migrationBuilder.CreateTable(
                name: "entitlement_bundle_issues",
                schema: "cloud",
                columns: table => new
                {
                    bundle_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    entitlement_version = table.Column<long>(type: "bigint", nullable: false),
                    entitlement_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    algorithm = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    key_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    signature_value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    paid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    warning_starts_at = table.Column<DateOnly>(type: "date", nullable: false),
                    grace_until = table.Column<DateOnly>(type: "date", nullable: false),
                    offline_valid_until = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement_bundle_issues", x => x.bundle_issue_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_installations_client_id",
                schema: "cloud",
                table: "client_installations",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_bundle_issues_client_id",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_bundle_issues_installation_id",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                column: "installation_id");

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_bundle_issues_installation_version",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                columns: new[] { "installation_id", "entitlement_version" });

            migrationBuilder.AddForeignKey(
                name: "FK_entitlement_bundle_issues_client_installations_installation~",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                column: "installation_id",
                principalSchema: "cloud",
                principalTable: "client_installations",
                principalColumn: "installation_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_entitlement_bundle_issues_client_installations_installation~",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropTable(
                name: "entitlement_bundle_issues",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "client_installations",
                schema: "cloud");
        }
    }
}

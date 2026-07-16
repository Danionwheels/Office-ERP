using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAccessRevisionToBundleIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_access_revision_id",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE cloud.entitlement_bundle_issues
                SET client_access_revision_id = COALESCE(
                    NULLIF(payload_json ->> 'clientAccessRevisionId', '')::uuid,
                    entitlement_snapshot_id);
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "client_access_revision_id",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_bundle_issues_access_revision",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                column: "client_access_revision_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_entitlement_bundle_issues_access_revision",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropColumn(
                name: "client_access_revision_id",
                schema: "cloud",
                table: "entitlement_bundle_issues");
        }
    }
}

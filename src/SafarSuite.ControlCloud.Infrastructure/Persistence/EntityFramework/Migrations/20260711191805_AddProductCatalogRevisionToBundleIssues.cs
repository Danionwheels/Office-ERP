using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCatalogRevisionToBundleIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "product_catalog_revision_id",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "product_catalog_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE cloud.entitlement_bundle_issues
                SET product_catalog_revision_id = CASE
                        WHEN payload_json ->> 'productCatalogRevisionId'
                            ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
                            THEN (payload_json ->> 'productCatalogRevisionId')::uuid
                        ELSE '00000000-0000-0000-0000-000000000000'::uuid
                    END,
                    product_catalog_revision_number = CASE
                        WHEN payload_json ->> 'productCatalogRevisionNumber' ~ '^[0-9]+$'
                            THEN (payload_json ->> 'productCatalogRevisionNumber')::bigint
                        ELSE 0
                    END;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_catalog_revision_id",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "product_catalog_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_bundle_issues_product_catalog_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                sql: "product_catalog_revision_number >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_bundle_issues_product_catalog_revision",
                schema: "cloud",
                table: "entitlement_bundle_issues",
                column: "product_catalog_revision_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_bundle_issues_product_catalog_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropIndex(
                name: "ix_entitlement_bundle_issues_product_catalog_revision",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropColumn(
                name: "product_catalog_revision_id",
                schema: "cloud",
                table: "entitlement_bundle_issues");

            migrationBuilder.DropColumn(
                name: "product_catalog_revision_number",
                schema: "cloud",
                table: "entitlement_bundle_issues");
        }
    }
}

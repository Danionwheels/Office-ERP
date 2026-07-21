using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "base_catalog_revision_id",
                schema: "control",
                table: "product_access_catalogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "base_catalog_revision_number",
                schema: "control",
                table: "product_access_catalogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_reason",
                schema: "control",
                table: "product_access_catalogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "draft_id",
                schema: "control",
                table: "product_access_catalogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modules_json",
                schema: "control",
                table: "product_access_catalogs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_catalog_revision_id",
                schema: "control",
                table: "client_contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "product_catalog_revision_number",
                schema: "control",
                table: "client_contracts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_catalog_revision_id",
                schema: "control",
                table: "client_access_revisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "product_catalog_revision_number",
                schema: "control",
                table: "client_access_revisions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_catalog_revisions",
                schema: "control",
                columns: table => new
                {
                    catalog_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<long>(type: "bigint", nullable: false),
                    supersedes_catalog_revision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modules_json = table.Column<string>(type: "jsonb", nullable: false),
                    module_groups_json = table.Column<string>(type: "jsonb", nullable: false),
                    resources_json = table.Column<string>(type: "jsonb", nullable: false),
                    change_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    published_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_catalog_revisions", x => x.catalog_revision_id);
                    table.UniqueConstraint("ak_product_catalog_revisions_id_number", x => new { x.catalog_revision_id, x.revision_number });
                    table.CheckConstraint("ck_product_catalog_revisions_lineage", "(revision_number = 1 AND supersedes_catalog_revision_id IS NULL) OR (revision_number > 1 AND supersedes_catalog_revision_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_product_catalog_revisions_product_catalog_revisions_superse~",
                        column: x => x.supersedes_catalog_revision_id,
                        principalSchema: "control",
                        principalTable: "product_catalog_revisions",
                        principalColumn: "catalog_revision_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                WITH legacy_modules AS (
                    SELECT COALESCE(
                        jsonb_agg(
                            jsonb_build_object(
                                'moduleCode', module_code,
                                'displayName', module_code,
                                'commercialMode', 'PaidAddOn',
                                'isActive', true,
                                'billingDefaults', NULL,
                                'compatibility', jsonb_build_object(
                                    'minimumSafarSuiteVersion', NULL,
                                    'minimumLocalServerVersion', NULL,
                                    'supportedDeploymentModes', '[]'::jsonb))
                            ORDER BY module_code),
                        '[]'::jsonb) AS modules
                    FROM (
                        SELECT DISTINCT module_code
                        FROM control.client_contract_module_allowances
                        WHERE upper(module_code) NOT IN ('CONTROL_DESK', 'PAYROLL', 'TOUR')
                    ) legacy
                )
                INSERT INTO control.product_catalog_revisions (
                    catalog_revision_id,
                    revision_number,
                    supersedes_catalog_revision_id,
                    modules_json,
                    module_groups_json,
                    resources_json,
                    change_reason,
                    published_by,
                    published_at_utc)
                SELECT
                    '9c1da88b-c763-4bb0-8dda-2d95fe63ec8f'::uuid,
                    1,
                    NULL,
                    '[
                      {
                        "moduleCode": "CONTROL_DESK",
                        "displayName": "Control Desk (Dummy)",
                        "commercialMode": "IncludedForAll",
                        "isActive": true,
                        "billingDefaults": null,
                        "compatibility": {
                          "minimumSafarSuiteVersion": null,
                          "minimumLocalServerVersion": null,
                          "supportedDeploymentModes": []
                        }
                      },
                      {
                        "moduleCode": "PAYROLL",
                        "displayName": "Payroll (Dummy)",
                        "commercialMode": "PaidAddOn",
                        "isActive": true,
                        "billingDefaults": {
                          "chargeCode": "PAYROLL",
                          "chargeName": "Payroll module (Dummy)",
                          "description": "Payroll module access (Dummy)",
                          "defaultUnitPriceAmount": 5000,
                          "currencyCode": "PKR",
                          "billingCycle": "Monthly"
                        },
                        "compatibility": {
                          "minimumSafarSuiteVersion": null,
                          "minimumLocalServerVersion": null,
                          "supportedDeploymentModes": []
                        }
                      },
                      {
                        "moduleCode": "TOUR",
                        "displayName": "Tour (Dummy)",
                        "commercialMode": "PaidAddOn",
                        "isActive": true,
                        "billingDefaults": {
                          "chargeCode": "TOUR",
                          "chargeName": "Tour module (Dummy)",
                          "description": "Tour module access (Dummy)",
                          "defaultUnitPriceAmount": 8000,
                          "currencyCode": "PKR",
                          "billingCycle": "Monthly"
                        },
                        "compatibility": {
                          "minimumSafarSuiteVersion": null,
                          "minimumLocalServerVersion": null,
                          "supportedDeploymentModes": []
                        }
                      }
                    ]'::jsonb || legacy_modules.modules,
                    COALESCE(
                        (SELECT module_groups_json
                         FROM control.product_access_catalogs
                         WHERE catalog_id = 'default'),
                        '[
                          {"groupId":"foundation-core","displayName":"Foundation Core","accessKind":"CoreIncluded","moduleCodes":["platform","identity-access","tenant-branch","module-registry","entitlements","notifications","audit"]},
                          {"groupId":"accounting-ledger","displayName":"Accounting Ledger","accessKind":"PaidModule","moduleCodes":["accounting"]},
                          {"groupId":"reporting","displayName":"Reporting","accessKind":"PaidModule","moduleCodes":["reporting-core"]},
                          {"groupId":"clients-parties","displayName":"Clients & Parties","accessKind":"PaidModule","moduleCodes":["clients-parties"]},
                          {"groupId":"travel","displayName":"Travel","accessKind":"PaidModule","moduleCodes":["travel","ticket-stock"]},
                          {"groupId":"tour","displayName":"Tour","accessKind":"PaidModule","moduleCodes":["tour","visa","hotels","transport"]},
                          {"groupId":"connectivity","displayName":"Connectivity","accessKind":"PaidModule","moduleCodes":["cloud-sync","owner-cloud-dashboard","cloud-backup","cloud-consolidated-reports","remote-monitoring"]}
                        ]'::jsonb),
                    COALESCE(
                        (SELECT resources_json
                         FROM control.product_access_catalogs
                         WHERE catalog_id = 'default'),
                        '[
                          {"resourceId":"product-kernel.state","displayName":"Product Kernel State","accessKind":"Public","requiredGroupIds":[],"requiredModuleCodes":[],"resolvedModuleCodes":[]},
                          {"resourceId":"product-kernel.modules","displayName":"Module Administration","accessKind":"CoreIncluded","requiredGroupIds":["foundation-core"],"requiredModuleCodes":[],"resolvedModuleCodes":["platform","identity-access","tenant-branch","module-registry","entitlements","notifications","audit"]},
                          {"resourceId":"reports.catalog","displayName":"Report Catalog","accessKind":"PaidModule","requiredGroupIds":["reporting"],"requiredModuleCodes":[],"resolvedModuleCodes":["reporting-core"]},
                          {"resourceId":"reports.execute","displayName":"Report Execution","accessKind":"PaidModule","requiredGroupIds":["reporting"],"requiredModuleCodes":[],"resolvedModuleCodes":["reporting-core"]},
                          {"resourceId":"reports.audit","displayName":"Report Audit","accessKind":"PaidModule","requiredGroupIds":["reporting"],"requiredModuleCodes":[],"resolvedModuleCodes":["reporting-core"]},
                          {"resourceId":"accounting.write","displayName":"Accounting Writes","accessKind":"PaidModule","requiredGroupIds":["accounting-ledger"],"requiredModuleCodes":[],"resolvedModuleCodes":["accounting"]}
                        ]'::jsonb),
                    'Imported the configured product definition and persisted access catalog as revision 1.',
                    'Control Desk migration',
                    '2026-07-11T00:00:00+00'::timestamptz
                FROM legacy_modules;

                UPDATE control.client_contracts
                SET product_catalog_revision_id = '9c1da88b-c763-4bb0-8dda-2d95fe63ec8f'::uuid,
                    product_catalog_revision_number = 1;

                UPDATE control.client_access_revisions
                SET product_catalog_revision_id = '9c1da88b-c763-4bb0-8dda-2d95fe63ec8f'::uuid,
                    product_catalog_revision_number = 1;

                DELETE FROM control.product_access_catalogs;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "base_catalog_revision_id",
                schema: "control",
                table: "product_access_catalogs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "base_catalog_revision_number",
                schema: "control",
                table: "product_access_catalogs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "change_reason",
                schema: "control",
                table: "product_access_catalogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "draft_id",
                schema: "control",
                table: "product_access_catalogs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "modules_json",
                schema: "control",
                table: "product_access_catalogs",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_catalog_revision_id",
                schema: "control",
                table: "client_contracts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "product_catalog_revision_number",
                schema: "control",
                table: "client_contracts",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_catalog_revision_id",
                schema: "control",
                table: "client_access_revisions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "product_catalog_revision_number",
                schema: "control",
                table: "client_access_revisions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE control.product_access_catalogs
                    ADD CONSTRAINT ck_product_access_catalogs_base_revision_number
                    CHECK (base_catalog_revision_number > 0);

                ALTER TABLE control.client_contracts
                    ADD CONSTRAINT ck_client_contracts_product_catalog_revision_number
                    CHECK (product_catalog_revision_number > 0);

                ALTER TABLE control.client_access_revisions
                    ADD CONSTRAINT ck_client_access_revisions_product_catalog_revision_number
                    CHECK (product_catalog_revision_number > 0);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_product_access_catalogs_base_catalog_revision_id_base_catal~",
                schema: "control",
                table: "product_access_catalogs",
                columns: new[] { "base_catalog_revision_id", "base_catalog_revision_number" });

            migrationBuilder.CreateIndex(
                name: "ix_client_contracts_product_catalog_revision",
                schema: "control",
                table: "client_contracts",
                column: "product_catalog_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_contracts_product_catalog_revision_id_product_catalo~",
                schema: "control",
                table: "client_contracts",
                columns: new[] { "product_catalog_revision_id", "product_catalog_revision_number" });

            migrationBuilder.CreateIndex(
                name: "ix_client_access_revisions_product_catalog_revision",
                schema: "control",
                table: "client_access_revisions",
                column: "product_catalog_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_access_revisions_product_catalog_revision_id_product~",
                schema: "control",
                table: "client_access_revisions",
                columns: new[] { "product_catalog_revision_id", "product_catalog_revision_number" });

            migrationBuilder.CreateIndex(
                name: "ux_product_catalog_revisions_number",
                schema: "control",
                table: "product_catalog_revisions",
                column: "revision_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_catalog_revisions_supersedes",
                schema: "control",
                table: "product_catalog_revisions",
                column: "supersedes_catalog_revision_id",
                unique: true,
                filter: "supersedes_catalog_revision_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_client_access_revisions_product_catalog_revisions_product_c~",
                schema: "control",
                table: "client_access_revisions",
                columns: new[] { "product_catalog_revision_id", "product_catalog_revision_number" },
                principalSchema: "control",
                principalTable: "product_catalog_revisions",
                principalColumns: new[] { "catalog_revision_id", "revision_number" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_client_contracts_product_catalog_revisions_product_catalog_~",
                schema: "control",
                table: "client_contracts",
                columns: new[] { "product_catalog_revision_id", "product_catalog_revision_number" },
                principalSchema: "control",
                principalTable: "product_catalog_revisions",
                principalColumns: new[] { "catalog_revision_id", "revision_number" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_product_access_catalogs_product_catalog_revisions_base_cata~",
                schema: "control",
                table: "product_access_catalogs",
                columns: new[] { "base_catalog_revision_id", "base_catalog_revision_number" },
                principalSchema: "control",
                principalTable: "product_catalog_revisions",
                principalColumns: new[] { "catalog_revision_id", "revision_number" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION control.reject_product_catalog_revision_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'Published product catalog revisions are append-only.';
                END;
                $function$;

                CREATE TRIGGER trg_product_catalog_revisions_append_only
                BEFORE UPDATE OR DELETE ON control.product_catalog_revisions
                FOR EACH ROW
                EXECUTE FUNCTION control.reject_product_catalog_revision_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_product_catalog_revisions_append_only
                    ON control.product_catalog_revisions;
                DROP FUNCTION IF EXISTS control.reject_product_catalog_revision_mutation();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_client_access_revisions_product_catalog_revisions_product_c~",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropForeignKey(
                name: "FK_client_contracts_product_catalog_revisions_product_catalog_~",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_product_access_catalogs_product_catalog_revisions_base_cata~",
                schema: "control",
                table: "product_access_catalogs");

            migrationBuilder.DropTable(
                name: "product_catalog_revisions",
                schema: "control");

            migrationBuilder.DropIndex(
                name: "IX_product_access_catalogs_base_catalog_revision_id_base_catal~",
                schema: "control",
                table: "product_access_catalogs");

            migrationBuilder.DropIndex(
                name: "ix_client_contracts_product_catalog_revision",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropIndex(
                name: "IX_client_contracts_product_catalog_revision_id_product_catalo~",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropIndex(
                name: "ix_client_access_revisions_product_catalog_revision",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropIndex(
                name: "IX_client_access_revisions_product_catalog_revision_id_product~",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropColumn(
                name: "base_catalog_revision_id",
                schema: "control",
                table: "product_access_catalogs");

            migrationBuilder.DropColumn(
                name: "base_catalog_revision_number",
                schema: "control",
                table: "product_access_catalogs");

            migrationBuilder.DropColumn(
                name: "change_reason",
                schema: "control",
                table: "product_access_catalogs");

            migrationBuilder.DropColumn(
                name: "draft_id",
                schema: "control",
                table: "product_access_catalogs");

            migrationBuilder.DropColumn(
                name: "modules_json",
                schema: "control",
                table: "product_access_catalogs");

            migrationBuilder.DropColumn(
                name: "product_catalog_revision_id",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "product_catalog_revision_number",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "product_catalog_revision_id",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropColumn(
                name: "product_catalog_revision_number",
                schema: "control",
                table: "client_access_revisions");
        }
    }
}

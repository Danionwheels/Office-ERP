using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAccessRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_access_revision_id",
                schema: "control",
                table: "entitlement_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "client_access_revisions",
                schema: "control",
                columns: table => new
                {
                    client_access_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_invoice_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    evidence_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    revision_number = table.Column<long>(type: "bigint", nullable: false),
                    supersedes_revision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    grace_until = table.Column<DateOnly>(type: "date", nullable: false),
                    offline_valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    allowed_devices = table.Column<int>(type: "integer", nullable: false),
                    allowed_branches = table.Column<int>(type: "integer", nullable: false),
                    approved_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    approval_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_access_revisions", x => x.client_access_revision_id);
                    table.ForeignKey(
                        name: "FK_client_access_revisions_client_access_revisions_supersedes_~",
                        column: x => x.supersedes_revision_id,
                        principalSchema: "control",
                        principalTable: "client_access_revisions",
                        principalColumn: "client_access_revision_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_client_access_revisions_client_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "control",
                        principalTable: "client_contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_client_access_revisions_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_client_access_revisions_invoices_source_invoice_id",
                        column: x => x.source_invoice_id,
                        principalSchema: "control",
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_access_revision_modules",
                schema: "control",
                columns: table => new
                {
                    client_access_revision_module_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    client_access_revision_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_access_revision_modules", x => x.client_access_revision_module_row_id);
                    table.ForeignKey(
                        name: "FK_client_access_revision_modules_client_access_revisions_clie~",
                        column: x => x.client_access_revision_id,
                        principalSchema: "control",
                        principalTable: "client_access_revisions",
                        principalColumn: "client_access_revision_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO control.client_access_revisions
                (
                    client_access_revision_id,
                    client_id,
                    contract_id,
                    source_invoice_id,
                    source_invoice_number,
                    evidence_type,
                    revision_number,
                    supersedes_revision_id,
                    paid_until,
                    grace_until,
                    offline_valid_until,
                    allowed_devices,
                    allowed_branches,
                    approved_by,
                    approval_reason,
                    approved_at_utc
                )
                SELECT
                    entitlement_snapshot_id,
                    client_id,
                    contract_id,
                    NULL,
                    NULL,
                    'LegacyEntitlementImport',
                    entitlement_version,
                    LAG(entitlement_snapshot_id) OVER
                    (
                        PARTITION BY client_id
                        ORDER BY entitlement_version, issued_at_utc, entitlement_snapshot_id
                    ),
                    paid_until,
                    grace_until,
                    offline_valid_until,
                    allowed_devices,
                    allowed_branches,
                    'migration:legacy-entitlement',
                    'Imported from an entitlement snapshot created before approved access revisions were introduced.',
                    issued_at_utc
                FROM control.entitlement_snapshots;

                INSERT INTO control.client_access_revision_modules
                (
                    module_code,
                    is_enabled,
                    client_access_revision_id
                )
                SELECT
                    module_code,
                    is_enabled,
                    entitlement_snapshot_id
                FROM control.entitlement_snapshot_modules;

                UPDATE control.entitlement_snapshots
                SET client_access_revision_id = entitlement_snapshot_id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "client_access_revision_id",
                schema: "control",
                table: "entitlement_snapshots",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_snapshots_client_access_revision_id",
                schema: "control",
                table: "entitlement_snapshots",
                column: "client_access_revision_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_client_access_revision_modules_revision_code",
                schema: "control",
                table: "client_access_revision_modules",
                columns: new[] { "client_access_revision_id", "module_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_access_revisions_contract",
                schema: "control",
                table: "client_access_revisions",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_access_revisions_source_invoice",
                schema: "control",
                table: "client_access_revisions",
                column: "source_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ux_client_access_revisions_client_number",
                schema: "control",
                table: "client_access_revisions",
                columns: new[] { "client_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_client_access_revisions_client_root",
                schema: "control",
                table: "client_access_revisions",
                column: "client_id",
                unique: true,
                filter: "supersedes_revision_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_client_access_revisions_supersedes",
                schema: "control",
                table: "client_access_revisions",
                column: "supersedes_revision_id",
                unique: true,
                filter: "supersedes_revision_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_entitlement_snapshots_client_access_revisions_client_access~",
                schema: "control",
                table: "entitlement_snapshots",
                column: "client_access_revision_id",
                principalSchema: "control",
                principalTable: "client_access_revisions",
                principalColumn: "client_access_revision_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_entitlement_snapshots_client_access_revisions_client_access~",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropTable(
                name: "client_access_revision_modules",
                schema: "control");

            migrationBuilder.DropTable(
                name: "client_access_revisions",
                schema: "control");

            migrationBuilder.DropIndex(
                name: "IX_entitlement_snapshots_client_access_revision_id",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropColumn(
                name: "client_access_revision_id",
                schema: "control",
                table: "entitlement_snapshots");
        }
    }
}

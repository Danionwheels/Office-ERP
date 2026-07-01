using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitlementSnapshotPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entitlement_snapshots",
                schema: "control",
                columns: table => new
                {
                    entitlement_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    paid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    grace_until = table.Column<DateOnly>(type: "date", nullable: false),
                    offline_valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    allowed_devices = table.Column<int>(type: "integer", nullable: false),
                    allowed_branches = table.Column<int>(type: "integer", nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement_snapshots", x => x.entitlement_snapshot_id);
                    table.ForeignKey(
                        name: "FK_entitlement_snapshots_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entitlement_snapshot_modules",
                schema: "control",
                columns: table => new
                {
                    entitlement_snapshot_module_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    entitlement_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement_snapshot_modules", x => x.entitlement_snapshot_module_row_id);
                    table.ForeignKey(
                        name: "FK_entitlement_snapshot_modules_entitlement_snapshots_entitlem~",
                        column: x => x.entitlement_snapshot_id,
                        principalSchema: "control",
                        principalTable: "entitlement_snapshots",
                        principalColumn: "entitlement_snapshot_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_entitlement_snapshot_modules_snapshot_code",
                schema: "control",
                table: "entitlement_snapshot_modules",
                columns: new[] { "entitlement_snapshot_id", "module_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_snapshots_client_issued",
                schema: "control",
                table: "entitlement_snapshots",
                columns: new[] { "client_id", "issued_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entitlement_snapshot_modules",
                schema: "control");

            migrationBuilder.DropTable(
                name: "entitlement_snapshots",
                schema: "control");
        }
    }
}

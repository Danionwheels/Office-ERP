using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddDesiredAccessLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "allowed_concurrent_users",
                schema: "control",
                table: "entitlement_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "allowed_named_users",
                schema: "control",
                table: "entitlement_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "allowed_concurrent_users",
                schema: "control",
                table: "client_contracts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "allowed_named_users",
                schema: "control",
                table: "client_contracts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "allowed_concurrent_users",
                schema: "control",
                table: "client_access_revisions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "allowed_named_users",
                schema: "control",
                table: "client_access_revisions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "client_access_revision_feature_limits",
                schema: "control",
                columns: table => new
                {
                    client_access_revision_feature_limit_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    limit_value = table.Column<long>(type: "bigint", nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    client_access_revision_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_access_revision_feature_limits", x => x.client_access_revision_feature_limit_row_id);
                    table.CheckConstraint("ck_client_access_revision_feature_limits_value", "limit_value >= 0");
                    table.ForeignKey(
                        name: "FK_client_access_revision_feature_limits_client_access_revisio~",
                        column: x => x.client_access_revision_id,
                        principalSchema: "control",
                        principalTable: "client_access_revisions",
                        principalColumn: "client_access_revision_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_contract_feature_limits",
                schema: "control",
                columns: table => new
                {
                    client_contract_feature_limit_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    limit_value = table.Column<long>(type: "bigint", nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_contract_feature_limits", x => x.client_contract_feature_limit_row_id);
                    table.CheckConstraint("ck_client_contract_feature_limits_value", "limit_value >= 0");
                    table.ForeignKey(
                        name: "FK_client_contract_feature_limits_client_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "control",
                        principalTable: "client_contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entitlement_snapshot_feature_limits",
                schema: "control",
                columns: table => new
                {
                    entitlement_snapshot_feature_limit_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    limit_value = table.Column<long>(type: "bigint", nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entitlement_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement_snapshot_feature_limits", x => x.entitlement_snapshot_feature_limit_row_id);
                    table.CheckConstraint("ck_entitlement_snapshot_feature_limits_value", "limit_value >= 0");
                    table.ForeignKey(
                        name: "FK_entitlement_snapshot_feature_limits_entitlement_snapshots_e~",
                        column: x => x.entitlement_snapshot_id,
                        principalSchema: "control",
                        principalTable: "entitlement_snapshots",
                        principalColumn: "entitlement_snapshot_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_snapshots_concurrent_users",
                schema: "control",
                table: "entitlement_snapshots",
                sql: "allowed_concurrent_users IS NULL OR allowed_concurrent_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_snapshots_named_users",
                schema: "control",
                table: "entitlement_snapshots",
                sql: "allowed_named_users IS NULL OR allowed_named_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_entitlement_snapshots_user_limit_order",
                schema: "control",
                table: "entitlement_snapshots",
                sql: "allowed_named_users IS NULL OR allowed_concurrent_users IS NULL OR allowed_concurrent_users <= allowed_named_users");

            migrationBuilder.AddCheckConstraint(
                name: "ck_client_contracts_concurrent_users",
                schema: "control",
                table: "client_contracts",
                sql: "allowed_concurrent_users IS NULL OR allowed_concurrent_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_client_contracts_named_users",
                schema: "control",
                table: "client_contracts",
                sql: "allowed_named_users IS NULL OR allowed_named_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_client_contracts_user_limit_order",
                schema: "control",
                table: "client_contracts",
                sql: "allowed_named_users IS NULL OR allowed_concurrent_users IS NULL OR allowed_concurrent_users <= allowed_named_users");

            migrationBuilder.AddCheckConstraint(
                name: "ck_client_access_revisions_concurrent_users",
                schema: "control",
                table: "client_access_revisions",
                sql: "allowed_concurrent_users IS NULL OR allowed_concurrent_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_client_access_revisions_named_users",
                schema: "control",
                table: "client_access_revisions",
                sql: "allowed_named_users IS NULL OR allowed_named_users >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_client_access_revisions_user_limit_order",
                schema: "control",
                table: "client_access_revisions",
                sql: "allowed_named_users IS NULL OR allowed_concurrent_users IS NULL OR allowed_concurrent_users <= allowed_named_users");

            migrationBuilder.CreateIndex(
                name: "ux_client_access_revision_feature_limits_revision_key",
                schema: "control",
                table: "client_access_revision_feature_limits",
                columns: new[] { "client_access_revision_id", "module_code", "feature_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_client_contract_feature_limits_contract_key",
                schema: "control",
                table: "client_contract_feature_limits",
                columns: new[] { "contract_id", "module_code", "feature_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_entitlement_snapshot_feature_limits_snapshot_key",
                schema: "control",
                table: "entitlement_snapshot_feature_limits",
                columns: new[] { "entitlement_snapshot_id", "module_code", "feature_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_access_revision_feature_limits",
                schema: "control");

            migrationBuilder.DropTable(
                name: "client_contract_feature_limits",
                schema: "control");

            migrationBuilder.DropTable(
                name: "entitlement_snapshot_feature_limits",
                schema: "control");

            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_snapshots_concurrent_users",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_snapshots_named_users",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_entitlement_snapshots_user_limit_order",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_client_contracts_concurrent_users",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_client_contracts_named_users",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_client_contracts_user_limit_order",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_client_access_revisions_concurrent_users",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_client_access_revisions_named_users",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_client_access_revisions_user_limit_order",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropColumn(
                name: "allowed_concurrent_users",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropColumn(
                name: "allowed_named_users",
                schema: "control",
                table: "entitlement_snapshots");

            migrationBuilder.DropColumn(
                name: "allowed_concurrent_users",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "allowed_named_users",
                schema: "control",
                table: "client_contracts");

            migrationBuilder.DropColumn(
                name: "allowed_concurrent_users",
                schema: "control",
                table: "client_access_revisions");

            migrationBuilder.DropColumn(
                name: "allowed_named_users",
                schema: "control",
                table: "client_access_revisions");
        }
    }
}

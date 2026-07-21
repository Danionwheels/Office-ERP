using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalOperatorAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "local_operators",
                schema: "auth",
                columns: table => new
                {
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    security_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_operators", x => x.operator_id);
                    table.CheckConstraint("ck_local_operators_normalized_email", "normalized_email = upper(btrim(email))");
                    table.CheckConstraint("ck_local_operators_security_version", "security_version > 0");
                    table.CheckConstraint("ck_local_operators_status", "status IN ('Active', 'Disabled')");
                });

            migrationBuilder.CreateTable(
                name: "local_operator_roles",
                schema: "auth",
                columns: table => new
                {
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_operator_roles", x => new { x.operator_id, x.role });
                    table.CheckConstraint("ck_local_operator_roles_role", "role IN ('Administrator', 'CommercialOperator', 'FinanceOperator', 'SupportOperator', 'Auditor')");
                    table.ForeignKey(
                        name: "FK_local_operator_roles_local_operators_operator_id",
                        column: x => x.operator_id,
                        principalSchema: "auth",
                        principalTable: "local_operators",
                        principalColumn: "operator_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "local_operator_scopes",
                schema: "auth",
                columns: table => new
                {
                    scope = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_operator_scopes", x => new { x.operator_id, x.scope });
                    table.CheckConstraint("ck_local_operator_scopes_scope", "scope IN ('control-desk:admin', 'command-center:read', 'clients:manage', 'contracts:manage', 'accounting:manage', 'billing:manage', 'payments:manage', 'entitlements:manage', 'control-cloud:manage', 'diagnostics:read', 'reports:read')");
                    table.ForeignKey(
                        name: "FK_local_operator_scopes_local_operators_operator_id",
                        column: x => x.operator_id,
                        principalSchema: "auth",
                        principalTable: "local_operators",
                        principalColumn: "operator_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_local_operators_status",
                schema: "auth",
                table: "local_operators",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_local_operators_normalized_email",
                schema: "auth",
                table: "local_operators",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "local_operator_roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "local_operator_scopes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "local_operators",
                schema: "auth");
        }
    }
}

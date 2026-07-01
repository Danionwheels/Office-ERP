using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientContractPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_contracts",
                schema: "control",
                columns: table => new
                {
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    recurring_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    billing_cycle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    billing_day_of_month = table.Column<int>(type: "integer", nullable: false),
                    allowed_devices = table.Column<int>(type: "integer", nullable: false),
                    allowed_branches = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_contracts", x => x.contract_id);
                    table.ForeignKey(
                        name: "FK_client_contracts_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_contract_module_allowances",
                schema: "control",
                columns: table => new
                {
                    client_contract_module_allowance_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_contract_module_allowances", x => x.client_contract_module_allowance_row_id);
                    table.ForeignKey(
                        name: "FK_client_contract_module_allowances_client_contracts_contract~",
                        column: x => x.contract_id,
                        principalSchema: "control",
                        principalTable: "client_contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_client_contract_modules_contract_code",
                schema: "control",
                table: "client_contract_module_allowances",
                columns: new[] { "contract_id", "module_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_contracts_client_status",
                schema: "control",
                table: "client_contracts",
                columns: new[] { "client_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_client_contracts_number",
                schema: "control",
                table: "client_contracts",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_contract_module_allowances",
                schema: "control");

            migrationBuilder.DropTable(
                name: "client_contracts",
                schema: "control");
        }
    }
}

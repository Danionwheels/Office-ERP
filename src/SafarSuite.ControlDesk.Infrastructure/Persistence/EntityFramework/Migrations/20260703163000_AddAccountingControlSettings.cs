using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ControlDeskDbContext))]
    [Migration("20260703163000_AddAccountingControlSettings")]
    public partial class AddAccountingControlSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounting_control_settings",
                schema: "control",
                columns: table => new
                {
                    accounting_control_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    base_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    retained_earnings_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    income_summary_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rounding_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_control_settings", x => x.accounting_control_settings_id);
                    table.ForeignKey(
                        name: "FK_accounting_control_settings_ledger_accounts_income_summary_~",
                        column: x => x.income_summary_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accounting_control_settings_ledger_accounts_retained_earni~",
                        column: x => x.retained_earnings_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accounting_control_settings_ledger_accounts_rounding_accoun~",
                        column: x => x.rounding_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_control_settings_income_summary_account_id",
                schema: "control",
                table: "accounting_control_settings",
                column: "income_summary_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_control_settings_retained_earnings_account_id",
                schema: "control",
                table: "accounting_control_settings",
                column: "retained_earnings_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_control_settings_rounding_account_id",
                schema: "control",
                table: "accounting_control_settings",
                column: "rounding_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_accounting_control_settings_company",
                schema: "control",
                table: "accounting_control_settings",
                column: "company_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_control_settings",
                schema: "control");
        }
    }
}

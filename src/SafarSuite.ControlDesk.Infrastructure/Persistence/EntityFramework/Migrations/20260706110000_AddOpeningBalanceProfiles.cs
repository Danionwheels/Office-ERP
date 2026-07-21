using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ControlDeskDbContext))]
    [Migration("20260706110000_AddOpeningBalanceProfiles")]
    public partial class AddOpeningBalanceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opening_balance_profiles",
                schema: "control",
                columns: table => new
                {
                    opening_balance_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fiscal_year_from = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year_to = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    transactions_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    profit_and_loss_carry_forward_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opening_balance_profiles", x => x.opening_balance_profile_id);
                    table.ForeignKey(
                        name: "FK_opening_balance_profiles_ledger_accounts_profit_and_loss_ca~",
                        column: x => x.profit_and_loss_carry_forward_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_opening_balance_profiles_pl_carry_forward_account_id",
                schema: "control",
                table: "opening_balance_profiles",
                column: "profit_and_loss_carry_forward_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_opening_balance_profiles_company",
                schema: "control",
                table: "opening_balance_profiles",
                column: "company_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opening_balance_profiles",
                schema: "control");
        }
    }
}

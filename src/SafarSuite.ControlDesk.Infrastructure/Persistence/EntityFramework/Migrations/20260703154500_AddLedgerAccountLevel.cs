using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ControlDeskDbContext))]
    [Migration("20260703154500_AddLedgerAccountLevel")]
    public partial class AddLedgerAccountLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "level",
                schema: "control",
                table: "ledger_accounts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Detail");

            migrationBuilder.Sql(
                """
                UPDATE control.ledger_accounts
                SET level = CASE
                    WHEN parent_account_id IS NOT NULL THEN 'Subsidiary'
                    WHEN is_posting_account = false THEN 'Master'
                    ELSE 'Detail'
                END
                """);

            migrationBuilder.Sql(
                """
                UPDATE control.ledger_accounts AS ledger
                SET level = 'Control'
                FROM control.account_code_ranges AS range
                WHERE range.is_active = true
                    AND (lower(range.role) LIKE '%control%' OR lower(range.display_name) LIKE '%control%')
                    AND length(ledger.code) = range.code_length
                    AND ledger.code LIKE range.search_prefix || '%'
                    AND ledger.code >= range.range_start
                    AND ledger.code <= range.range_end
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "level",
                schema: "control",
                table: "ledger_accounts");
        }
    }
}

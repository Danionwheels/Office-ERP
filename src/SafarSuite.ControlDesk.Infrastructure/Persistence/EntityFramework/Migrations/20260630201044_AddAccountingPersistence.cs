using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "journal_entries",
                schema: "control",
                columns: table => new
                {
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    memo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    posted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.journal_entry_id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_accounts",
                schema: "control",
                columns: table => new
                {
                    ledger_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normal_balance = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    parent_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_posting_account = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_accounts", x => x.ledger_account_id);
                    table.ForeignKey(
                        name: "FK_ledger_accounts_ledger_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                schema: "control",
                columns: table => new
                {
                    journal_line_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ledger_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    debit_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_lines", x => x.journal_line_row_id);
                    table.ForeignKey(
                        name: "FK_journal_lines_journal_entries_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalSchema: "control",
                        principalTable: "journal_entries",
                        principalColumn: "journal_entry_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_journal_lines_ledger_accounts_ledger_account_id",
                        column: x => x.ledger_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_entry_date_created_id",
                schema: "control",
                table: "journal_entries",
                columns: new[] { "entry_date", "created_at_utc", "journal_entry_id" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_source_type",
                schema: "control",
                table: "journal_entries",
                column: "source_type");

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_journal_entry_id",
                schema: "control",
                table: "journal_lines",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_ledger_account_id",
                schema: "control",
                table: "journal_lines",
                column: "ledger_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_accounts_parent_account_id",
                schema: "control",
                table: "ledger_accounts",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_ledger_accounts_code",
                schema: "control",
                table: "ledger_accounts",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journal_lines",
                schema: "control");

            migrationBuilder.DropTable(
                name: "journal_entries",
                schema: "control");

            migrationBuilder.DropTable(
                name: "ledger_accounts",
                schema: "control");
        }
    }
}

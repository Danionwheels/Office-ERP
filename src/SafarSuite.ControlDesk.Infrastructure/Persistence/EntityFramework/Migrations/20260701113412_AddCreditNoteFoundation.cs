using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditNoteFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_notes",
                schema: "control",
                columns: table => new
                {
                    credit_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    credit_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_notes", x => x.credit_note_id);
                    table.ForeignKey(
                        name: "FK_credit_notes_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_notes_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "control",
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credit_notes_client_id",
                schema: "control",
                table: "credit_notes",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ux_credit_notes_invoice_id",
                schema: "control",
                table: "credit_notes",
                column: "invoice_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_credit_notes_number",
                schema: "control",
                table: "credit_notes",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_notes",
                schema: "control");
        }
    }
}

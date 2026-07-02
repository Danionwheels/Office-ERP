using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCreditApplicationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_credit_applications",
                schema: "control",
                columns: table => new
                {
                    client_credit_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    applied_on = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_credit_applications", x => x.client_credit_application_id);
                    table.ForeignKey(
                        name: "FK_client_credit_applications_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_client_credit_applications_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "control",
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_credit_applications_client_id",
                schema: "control",
                table: "client_credit_applications",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_credit_applications_invoice_id",
                schema: "control",
                table: "client_credit_applications",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ux_client_credit_applications_reference",
                schema: "control",
                table: "client_credit_applications",
                column: "reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_credit_applications",
                schema: "control");
        }
    }
}

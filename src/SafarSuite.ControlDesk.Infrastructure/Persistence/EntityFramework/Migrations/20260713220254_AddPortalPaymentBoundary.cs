using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalPaymentBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "portal_claim_id",
                schema: "control",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "portal_payment_claims",
                schema: "control",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    transfer_reference_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    proof_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    proof_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    proof_content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    proof_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    proof_uploaded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    imported_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portal_payment_claims", x => x.claim_id);
                    table.ForeignKey(
                        name: "FK_portal_payment_claims_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_portal_payment_claims_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "control",
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provider_bank_details",
                schema: "control",
                columns: table => new
                {
                    provider_bank_details_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_configured = table.Column<bool>(type: "boolean", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    account_title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    iban = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    branch_or_routing_info = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_bank_details", x => x.provider_bank_details_id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_payments_portal_claim_id",
                schema: "control",
                table: "payments",
                column: "portal_claim_id",
                unique: true,
                filter: "\"portal_claim_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_portal_payment_claims_client_id",
                schema: "control",
                table: "portal_payment_claims",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_portal_payment_claims_client_status_submitted",
                schema: "control",
                table: "portal_payment_claims",
                columns: new[] { "client_id", "status", "submitted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_portal_payment_claims_invoice_id",
                schema: "control",
                table: "portal_payment_claims",
                column: "invoice_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portal_payment_claims",
                schema: "control");

            migrationBuilder.DropTable(
                name: "provider_bank_details",
                schema: "control");

            migrationBuilder.DropIndex(
                name: "ux_payments_portal_claim_id",
                schema: "control",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "portal_claim_id",
                schema: "control",
                table: "payments");
        }
    }
}

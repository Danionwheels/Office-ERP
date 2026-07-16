using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPortalPaymentBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_portal_attachments",
                schema: "cloud",
                columns: table => new
                {
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    uploaded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_portal_attachments", x => x.attachment_id);
                });

            migrationBuilder.CreateTable(
                name: "client_portal_payment_claims",
                schema: "cloud",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    transfer_reference_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_transfer_reference_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    proof_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_portal_payment_claims", x => x.claim_id);
                });

            migrationBuilder.CreateTable(
                name: "provider_bank_details",
                schema: "cloud",
                columns: table => new
                {
                    bank_details_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    account_title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    account_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    iban = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    branch_or_routing_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_bank_details", x => x.bank_details_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_attachments_client_uploaded",
                schema: "cloud",
                table: "client_portal_attachments",
                columns: new[] { "client_id", "uploaded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_payment_claims_client_status_submitted",
                schema: "cloud",
                table: "client_portal_payment_claims",
                columns: new[] { "client_id", "status", "submitted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_client_portal_payment_claims_invoice_id",
                schema: "cloud",
                table: "client_portal_payment_claims",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ux_client_portal_payment_claims_client_reference",
                schema: "cloud",
                table: "client_portal_payment_claims",
                columns: new[] { "client_id", "normalized_transfer_reference_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_portal_attachments",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "client_portal_payment_claims",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "provider_bank_details",
                schema: "cloud");
        }
    }
}

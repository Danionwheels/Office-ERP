using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientOwnedFinancialReadSpine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_invoice_lines_invoice_id",
                schema: "control",
                table: "invoice_lines",
                newName: "ix_invoice_lines_invoice_id");

            migrationBuilder.AddColumn<Guid>(
                name: "client_id",
                schema: "control",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_document_id",
                schema: "control",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE control.journal_entries AS j
                SET
                    client_id = i.client_id,
                    source_document_id = i.invoice_id
                FROM control.invoices AS i
                WHERE j.source_type IN ('BillingInvoice', 'BillingInvoiceVoid')
                  AND j.source_reference = i.number;

                UPDATE control.journal_entries AS j
                SET
                    client_id = n.client_id,
                    source_document_id = n.credit_note_id
                FROM control.credit_notes AS n
                WHERE j.source_type = 'BillingCreditNote'
                  AND j.source_reference = n.number;

                UPDATE control.journal_entries AS j
                SET
                    client_id = r.client_id,
                    source_document_id = r.client_refund_id
                FROM control.client_refunds AS r
                WHERE j.source_type = 'ClientRefund'
                  AND j.source_reference = r.reference;

                UPDATE control.journal_entries AS j
                SET (client_id, source_document_id) =
                (
                    SELECT p.client_id, p.payment_id
                    FROM control.payments AS p
                    WHERE p.reference = j.source_reference
                    ORDER BY
                        ABS(EXTRACT(EPOCH FROM (p.recorded_at_utc - j.created_at_utc))),
                        p.payment_id
                    LIMIT 1
                )
                WHERE j.source_type IN ('PaymentReceipt', 'PaymentReversal')
                  AND EXISTS
                  (
                      SELECT 1
                      FROM control.payments AS p
                      WHERE p.reference = j.source_reference
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "ix_payments_client_received_recorded_id",
                schema: "control",
                table: "payments",
                columns: new[] { "client_id", "received_on", "recorded_at_utc", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_client_status_received_recorded_id",
                schema: "control",
                table: "payments",
                columns: new[] { "client_id", "status", "received_on", "recorded_at_utc", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_client_date_created_id",
                schema: "control",
                table: "journal_entries",
                columns: new[] { "client_id", "entry_date", "created_at_utc", "journal_entry_id" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_source_document",
                schema: "control",
                table: "journal_entries",
                columns: new[] { "source_type", "source_document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_client_issue_created_id",
                schema: "control",
                table: "invoices",
                columns: new[] { "client_id", "issue_date", "created_at_utc", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_client_status_issue_created_id",
                schema: "control",
                table: "invoices",
                columns: new[] { "client_id", "status", "issue_date", "created_at_utc", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credit_notes_client_credit_created_id",
                schema: "control",
                table: "credit_notes",
                columns: new[] { "client_id", "credit_date", "created_at_utc", "credit_note_id" });

            migrationBuilder.CreateIndex(
                name: "ix_client_refunds_client_refunded_created_id",
                schema: "control",
                table: "client_refunds",
                columns: new[] { "client_id", "refunded_on", "created_at_utc", "client_refund_id" });

            migrationBuilder.CreateIndex(
                name: "ix_client_credit_applications_client_applied_created_id",
                schema: "control",
                table: "client_credit_applications",
                columns: new[] { "client_id", "applied_on", "created_at_utc", "client_credit_application_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_journal_entries_clients_client_id",
                schema: "control",
                table: "journal_entries",
                column: "client_id",
                principalSchema: "control",
                principalTable: "clients",
                principalColumn: "client_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journal_entries_clients_client_id",
                schema: "control",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "ix_payments_client_received_recorded_id",
                schema: "control",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_payments_client_status_received_recorded_id",
                schema: "control",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_journal_entries_client_date_created_id",
                schema: "control",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "ix_journal_entries_source_document",
                schema: "control",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "ix_invoices_client_issue_created_id",
                schema: "control",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoices_client_status_issue_created_id",
                schema: "control",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_credit_notes_client_credit_created_id",
                schema: "control",
                table: "credit_notes");

            migrationBuilder.DropIndex(
                name: "ix_client_refunds_client_refunded_created_id",
                schema: "control",
                table: "client_refunds");

            migrationBuilder.DropIndex(
                name: "ix_client_credit_applications_client_applied_created_id",
                schema: "control",
                table: "client_credit_applications");

            migrationBuilder.DropColumn(
                name: "client_id",
                schema: "control",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "source_document_id",
                schema: "control",
                table: "journal_entries");

            migrationBuilder.RenameIndex(
                name: "ix_invoice_lines_invoice_id",
                schema: "control",
                table: "invoice_lines",
                newName: "IX_invoice_lines_invoice_id");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCommercialProjectionDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "available_credit",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "balance_due",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "PKR");

            migrationBuilder.AddColumn<bool>(
                name: "is_paid",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "latest_entitlement_json",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_credit_applied",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_credited",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_invoiced",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_paid",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_refunded",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "commercial_documents",
                schema: "cloud",
                columns: table => new
                {
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    last_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    detail_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_documents", x => new { x.client_id, x.document_type, x.document_id });
                    table.ForeignKey(
                        name: "FK_commercial_documents_client_commercial_projections_client_id",
                        column: x => x.client_id,
                        principalSchema: "cloud",
                        principalTable: "client_commercial_projections",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE cloud.client_commercial_projections
                SET currency_code = LEFT(COALESCE(NULLIF(projection_json ->> 'currencyCode', ''), 'PKR'), 3),
                    total_invoiced = COALESCE(NULLIF(projection_json ->> 'totalInvoiced', '')::numeric, 0),
                    total_paid = COALESCE(NULLIF(projection_json ->> 'totalPaid', '')::numeric, 0),
                    total_credited = COALESCE(NULLIF(projection_json ->> 'totalCredited', '')::numeric, 0),
                    total_refunded = COALESCE(NULLIF(projection_json ->> 'totalRefunded', '')::numeric, 0),
                    total_credit_applied = COALESCE(NULLIF(projection_json ->> 'totalCreditApplied', '')::numeric, 0),
                    balance_due = COALESCE(NULLIF(projection_json ->> 'balanceDue', '')::numeric, 0),
                    available_credit = COALESCE(NULLIF(projection_json ->> 'availableCredit', '')::numeric, 0),
                    is_paid = COALESCE(NULLIF(projection_json ->> 'isPaid', '')::boolean, TRUE),
                    latest_entitlement_json = CASE
                        WHEN projection_json ? 'latestEntitlement'
                             AND projection_json -> 'latestEntitlement' <> 'null'::jsonb
                        THEN projection_json -> 'latestEntitlement'
                        ELSE NULL
                    END;

                INSERT INTO cloud.commercial_documents
                    (client_id, document_type, document_id, related_document_id, reference, status,
                     document_date, amount, balance_amount, currency_code, last_message_id,
                     occurred_at_utc, last_updated_at_utc, detail_json)
                SELECT p.client_id,
                       'Invoice',
                       COALESCE(NULLIF(item.value ->> 'invoiceId', ''), item.key)::uuid,
                       NULL,
                       LEFT(COALESCE(item.value ->> 'invoiceNumber', ''), 160),
                       LEFT(COALESCE(item.value ->> 'invoiceStatus', ''), 64),
                       COALESCE(NULLIF(item.value ->> 'issueDate', '')::date, DATE '1970-01-01'),
                       COALESCE(NULLIF(item.value ->> 'totalAmount', '')::numeric, 0),
                       COALESCE(NULLIF(item.value ->> 'balanceDue', '')::numeric, 0),
                       LEFT(COALESCE(NULLIF(item.value ->> 'currencyCode', ''), p.currency_code), 3),
                       '00000000-0000-0000-0000-000000000000'::uuid,
                       p.last_updated_at_utc,
                       p.last_updated_at_utc,
                       item.value
                FROM cloud.client_commercial_projections p
                CROSS JOIN LATERAL jsonb_each(COALESCE(p.projection_json -> 'invoices', '{}'::jsonb)) item;

                INSERT INTO cloud.commercial_documents
                    (client_id, document_type, document_id, related_document_id, reference, status,
                     document_date, amount, balance_amount, currency_code, last_message_id,
                     occurred_at_utc, last_updated_at_utc, detail_json)
                SELECT p.client_id,
                       'Payment',
                       COALESCE(NULLIF(item.value ->> 'paymentId', ''), item.key)::uuid,
                       NULLIF(item.value ->> 'invoiceId', '')::uuid,
                       LEFT(COALESCE(item.value ->> 'paymentReference', ''), 160),
                       LEFT(COALESCE(item.value ->> 'paymentStatus', ''), 64),
                       COALESCE(NULLIF(item.value ->> 'receivedOn', '')::date, DATE '1970-01-01'),
                       COALESCE(NULLIF(item.value ->> 'amount', '')::numeric, 0),
                       COALESCE(NULLIF(item.value ->> 'invoiceBalanceDue', '')::numeric, 0),
                       LEFT(COALESCE(NULLIF(item.value ->> 'currencyCode', ''), p.currency_code), 3),
                       '00000000-0000-0000-0000-000000000000'::uuid,
                       p.last_updated_at_utc,
                       p.last_updated_at_utc,
                       item.value
                FROM cloud.client_commercial_projections p
                CROSS JOIN LATERAL jsonb_each(COALESCE(p.projection_json -> 'payments', '{}'::jsonb)) item;

                INSERT INTO cloud.commercial_documents
                    (client_id, document_type, document_id, related_document_id, reference, status,
                     document_date, amount, balance_amount, currency_code, last_message_id,
                     occurred_at_utc, last_updated_at_utc, detail_json)
                SELECT p.client_id,
                       'CreditNote',
                       COALESCE(NULLIF(item.value ->> 'creditNoteId', ''), item.key)::uuid,
                       NULLIF(item.value ->> 'invoiceId', '')::uuid,
                       LEFT(COALESCE(item.value ->> 'creditNoteNumber', ''), 160),
                       LEFT(COALESCE(item.value ->> 'creditNoteStatus', ''), 64),
                       COALESCE(NULLIF(item.value ->> 'creditDate', '')::date, DATE '1970-01-01'),
                       COALESCE(NULLIF(item.value ->> 'amount', '')::numeric, 0),
                       0,
                       LEFT(COALESCE(NULLIF(item.value ->> 'currencyCode', ''), p.currency_code), 3),
                       '00000000-0000-0000-0000-000000000000'::uuid,
                       p.last_updated_at_utc,
                       p.last_updated_at_utc,
                       item.value
                FROM cloud.client_commercial_projections p
                CROSS JOIN LATERAL jsonb_each(COALESCE(p.projection_json -> 'creditNotes', '{}'::jsonb)) item;

                INSERT INTO cloud.commercial_documents
                    (client_id, document_type, document_id, related_document_id, reference, status,
                     document_date, amount, balance_amount, currency_code, last_message_id,
                     occurred_at_utc, last_updated_at_utc, detail_json)
                SELECT p.client_id,
                       'Refund',
                       COALESCE(NULLIF(item.value ->> 'refundId', ''), item.key)::uuid,
                       NULL,
                       LEFT(COALESCE(item.value ->> 'refundReference', ''), 160),
                       LEFT(COALESCE(item.value ->> 'refundStatus', ''), 64),
                       COALESCE(NULLIF(item.value ->> 'refundedOn', '')::date, DATE '1970-01-01'),
                       COALESCE(NULLIF(item.value ->> 'amount', '')::numeric, 0),
                       COALESCE(NULLIF(item.value ->> 'clientBalanceAfter', '')::numeric, 0),
                       LEFT(COALESCE(NULLIF(item.value ->> 'currencyCode', ''), p.currency_code), 3),
                       '00000000-0000-0000-0000-000000000000'::uuid,
                       p.last_updated_at_utc,
                       p.last_updated_at_utc,
                       item.value
                FROM cloud.client_commercial_projections p
                CROSS JOIN LATERAL jsonb_each(COALESCE(p.projection_json -> 'refunds', '{}'::jsonb)) item;

                INSERT INTO cloud.commercial_documents
                    (client_id, document_type, document_id, related_document_id, reference, status,
                     document_date, amount, balance_amount, currency_code, last_message_id,
                     occurred_at_utc, last_updated_at_utc, detail_json)
                SELECT p.client_id,
                       'CreditApplication',
                       COALESCE(NULLIF(item.value ->> 'creditApplicationId', ''), item.key)::uuid,
                       NULLIF(item.value ->> 'invoiceId', '')::uuid,
                       LEFT(COALESCE(item.value ->> 'reference', ''), 160),
                       LEFT(COALESCE(item.value ->> 'creditApplicationStatus', ''), 64),
                       COALESCE(NULLIF(item.value ->> 'appliedOn', '')::date, DATE '1970-01-01'),
                       COALESCE(NULLIF(item.value ->> 'amount', '')::numeric, 0),
                       COALESCE(NULLIF(item.value ->> 'invoiceBalanceAfter', '')::numeric, 0),
                       LEFT(COALESCE(NULLIF(item.value ->> 'currencyCode', ''), p.currency_code), 3),
                       '00000000-0000-0000-0000-000000000000'::uuid,
                       p.last_updated_at_utc,
                       p.last_updated_at_utc,
                       item.value
                FROM cloud.client_commercial_projections p
                CROSS JOIN LATERAL jsonb_each(COALESCE(p.projection_json -> 'creditApplications', '{}'::jsonb)) item;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_commercial_documents_client_type_date_id",
                schema: "cloud",
                table: "commercial_documents",
                columns: new[] { "client_id", "document_type", "document_date", "document_id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_commercial_documents_related_date_id",
                schema: "cloud",
                table: "commercial_documents",
                columns: new[] { "client_id", "document_type", "related_document_id", "document_date", "document_id" },
                descending: new[] { false, false, false, true, true });

            migrationBuilder.DropColumn(
                name: "projection_json",
                schema: "cloud",
                table: "client_commercial_projections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "projection_json",
                schema: "cloud",
                table: "client_commercial_projections",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.Sql(
                """
                UPDATE cloud.client_commercial_projections p
                SET projection_json = jsonb_build_object(
                    'clientId', p.client_id,
                    'currencyCode', p.currency_code,
                    'totalInvoiced', p.total_invoiced,
                    'totalPaid', p.total_paid,
                    'totalCredited', p.total_credited,
                    'totalRefunded', p.total_refunded,
                    'totalCreditApplied', p.total_credit_applied,
                    'balanceDue', p.balance_due,
                    'availableCredit', p.available_credit,
                    'isPaid', p.is_paid,
                    'lastUpdatedAtUtc', p.last_updated_at_utc,
                    'invoices', COALESCE((
                        SELECT jsonb_object_agg(d.document_id::text, d.detail_json)
                        FROM cloud.commercial_documents d
                        WHERE d.client_id = p.client_id AND d.document_type = 'Invoice'
                    ), '{}'::jsonb),
                    'payments', COALESCE((
                        SELECT jsonb_object_agg(d.document_id::text, d.detail_json)
                        FROM cloud.commercial_documents d
                        WHERE d.client_id = p.client_id AND d.document_type = 'Payment'
                    ), '{}'::jsonb),
                    'creditNotes', COALESCE((
                        SELECT jsonb_object_agg(d.document_id::text, d.detail_json)
                        FROM cloud.commercial_documents d
                        WHERE d.client_id = p.client_id AND d.document_type = 'CreditNote'
                    ), '{}'::jsonb),
                    'refunds', COALESCE((
                        SELECT jsonb_object_agg(d.document_id::text, d.detail_json)
                        FROM cloud.commercial_documents d
                        WHERE d.client_id = p.client_id AND d.document_type = 'Refund'
                    ), '{}'::jsonb),
                    'creditApplications', COALESCE((
                        SELECT jsonb_object_agg(d.document_id::text, d.detail_json)
                        FROM cloud.commercial_documents d
                        WHERE d.client_id = p.client_id AND d.document_type = 'CreditApplication'
                    ), '{}'::jsonb),
                    'latestEntitlement', p.latest_entitlement_json
                );
                """);

            migrationBuilder.DropTable(
                name: "commercial_documents",
                schema: "cloud");

            migrationBuilder.DropColumn(
                name: "available_credit",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "balance_due",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "currency_code",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "is_paid",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "latest_entitlement_json",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "total_credit_applied",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "total_credited",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "total_invoiced",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "total_paid",
                schema: "cloud",
                table: "client_commercial_projections");

            migrationBuilder.DropColumn(
                name: "total_refunded",
                schema: "cloud",
                table: "client_commercial_projections");

        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingOutboxPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "charge_codes",
                schema: "control",
                columns: table => new
                {
                    charge_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    default_unit_price_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    revenue_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_charge_codes", x => x.charge_code_id);
                    table.ForeignKey(
                        name: "FK_charge_codes_ledger_accounts_revenue_account_id",
                        column: x => x.revenue_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_charge_codes_ledger_accounts_tax_account_id",
                        column: x => x.tax_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_accounting_profiles",
                schema: "control",
                columns: table => new
                {
                    client_accounting_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounts_receivable_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    cloud_customer_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_accounting_profiles", x => x.client_accounting_profile_id);
                    table.ForeignKey(
                        name: "FK_client_accounting_profiles_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_accounting_profiles_ledger_accounts_accounts_receiva~",
                        column: x => x.accounts_receivable_account_id,
                        principalSchema: "control",
                        principalTable: "ledger_accounts",
                        principalColumn: "ledger_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cloud_outbox_messages",
                schema: "control",
                columns: table => new
                {
                    cloud_outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    subject_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cloud_outbox_messages", x => x.cloud_outbox_message_id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "control",
                columns: table => new
                {
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amount_paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_paid_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.invoice_id);
                    table.ForeignKey(
                        name: "FK_invoices_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_charge_rules",
                schema: "control",
                columns: table => new
                {
                    client_charge_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    charge_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description_override = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_price_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    billing_cycle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    billing_day_of_month = table.Column<int>(type: "integer", nullable: false),
                    effective_starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_charge_rules", x => x.client_charge_rule_id);
                    table.ForeignKey(
                        name: "FK_client_charge_rules_charge_codes_charge_code_id",
                        column: x => x.charge_code_id,
                        principalSchema: "control",
                        principalTable: "charge_codes",
                        principalColumn: "charge_code_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_client_charge_rules_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                schema: "control",
                columns: table => new
                {
                    invoice_line_row_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    charge_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_lines", x => x.invoice_line_row_id);
                    table.ForeignKey(
                        name: "FK_invoice_lines_charge_codes_charge_code_id",
                        column: x => x.charge_code_id,
                        principalSchema: "control",
                        principalTable: "charge_codes",
                        principalColumn: "charge_code_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_lines_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "control",
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_charge_codes_revenue_account_id",
                schema: "control",
                table: "charge_codes",
                column: "revenue_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_charge_codes_tax_account_id",
                schema: "control",
                table: "charge_codes",
                column: "tax_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_charge_codes_code",
                schema: "control",
                table: "charge_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_accounting_profiles_accounts_receivable_account_id",
                schema: "control",
                table: "client_accounting_profiles",
                column: "accounts_receivable_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_client_accounting_profiles_client_id",
                schema: "control",
                table: "client_accounting_profiles",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_charge_rules_charge_code_id",
                schema: "control",
                table: "client_charge_rules",
                column: "charge_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_charge_rules_client_id",
                schema: "control",
                table: "client_charge_rules",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_cloud_outbox_messages_status_type_occurred",
                schema: "control",
                table: "cloud_outbox_messages",
                columns: new[] { "status", "message_type", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_charge_code_id",
                schema: "control",
                table: "invoice_lines",
                column: "charge_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_invoice_id",
                schema: "control",
                table: "invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_client_id",
                schema: "control",
                table: "invoices",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_number",
                schema: "control",
                table: "invoices",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_accounting_profiles",
                schema: "control");

            migrationBuilder.DropTable(
                name: "client_charge_rules",
                schema: "control");

            migrationBuilder.DropTable(
                name: "cloud_outbox_messages",
                schema: "control");

            migrationBuilder.DropTable(
                name: "invoice_lines",
                schema: "control");

            migrationBuilder.DropTable(
                name: "charge_codes",
                schema: "control");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "control");
        }
    }
}

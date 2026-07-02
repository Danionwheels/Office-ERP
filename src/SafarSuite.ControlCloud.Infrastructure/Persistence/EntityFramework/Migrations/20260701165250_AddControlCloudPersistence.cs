using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddControlCloudPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cloud");

            migrationBuilder.CreateTable(
                name: "client_commercial_projections",
                schema: "cloud",
                columns: table => new
                {
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    projection_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_commercial_projections", x => x.client_id);
                });

            migrationBuilder.CreateTable(
                name: "control_desk_envelope_receipts",
                schema: "cloud",
                columns: table => new
                {
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    subject_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    source_system = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_environment = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    signature_key_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    signature_value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cloud_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    prepared_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_desk_envelope_receipts", x => x.receipt_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_commercial_projections_last_updated_at_utc",
                schema: "cloud",
                table: "client_commercial_projections",
                column: "last_updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_control_desk_envelope_receipts_message_id",
                schema: "cloud",
                table: "control_desk_envelope_receipts",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_control_desk_envelope_receipts_status_idempotency_key",
                schema: "cloud",
                table: "control_desk_envelope_receipts",
                columns: new[] { "status", "idempotency_key" });

            migrationBuilder.CreateIndex(
                name: "ux_control_desk_envelope_receipts_accepted_idempotency_key",
                schema: "cloud",
                table: "control_desk_envelope_receipts",
                column: "idempotency_key",
                unique: true,
                filter: "status = 'Accepted'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_commercial_projections",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "control_desk_envelope_receipts",
                schema: "cloud");
        }
    }
}

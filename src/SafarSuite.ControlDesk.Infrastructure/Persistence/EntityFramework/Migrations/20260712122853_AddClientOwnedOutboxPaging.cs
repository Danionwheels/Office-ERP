using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientOwnedOutboxPaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cloud_outbox_messages_status_type_occurred",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.AddColumn<Guid>(
                name: "client_id",
                schema: "control",
                table: "cloud_outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE control.cloud_outbox_messages
                SET client_id = CASE
                    WHEN jsonb_typeof(payload_json -> 'clientId') = 'string'
                         AND payload_json ->> 'clientId' ~* '^([0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$'
                        THEN (payload_json ->> 'clientId')::uuid
                    ELSE (payload_json ->> 'ClientId')::uuid
                END
                WHERE client_id IS NULL
                  AND (
                      (jsonb_typeof(payload_json -> 'clientId') = 'string'
                       AND payload_json ->> 'clientId' ~* '^([0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$')
                      OR
                      (jsonb_typeof(payload_json -> 'ClientId') = 'string'
                       AND payload_json ->> 'ClientId' ~* '^([0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$')
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "ix_cloud_outbox_messages_client_occurred",
                schema: "control",
                table: "cloud_outbox_messages",
                columns: new[] { "client_id", "occurred_at_utc", "cloud_outbox_message_id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_cloud_outbox_messages_occurred",
                schema: "control",
                table: "cloud_outbox_messages",
                columns: new[] { "occurred_at_utc", "cloud_outbox_message_id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_cloud_outbox_messages_status_type_occurred",
                schema: "control",
                table: "cloud_outbox_messages",
                columns: new[] { "status", "message_type", "occurred_at_utc", "cloud_outbox_message_id" },
                descending: new[] { false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cloud_outbox_messages_client_occurred",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_cloud_outbox_messages_occurred",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_cloud_outbox_messages_status_type_occurred",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.DropColumn(
                name: "client_id",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.CreateIndex(
                name: "ix_cloud_outbox_messages_status_type_occurred",
                schema: "control",
                table: "cloud_outbox_messages",
                columns: new[] { "status", "message_type", "occurred_at_utc" });
        }
    }
}

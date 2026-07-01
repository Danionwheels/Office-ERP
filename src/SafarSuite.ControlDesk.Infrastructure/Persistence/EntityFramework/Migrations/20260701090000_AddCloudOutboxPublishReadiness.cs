using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ControlDeskDbContext))]
    [Migration("20260701090000_AddCloudOutboxPublishReadiness")]
    public partial class AddCloudOutboxPublishReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_attempted_at_utc",
                schema: "control",
                table: "cloud_outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at_utc",
                schema: "control",
                table: "cloud_outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_cloud_outbox_messages_publish_ready",
                schema: "control",
                table: "cloud_outbox_messages",
                columns: new[] { "status", "next_attempt_at_utc", "attempt_count" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cloud_outbox_messages_publish_ready",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.DropColumn(
                name: "last_attempted_at_utc",
                schema: "control",
                table: "cloud_outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at_utc",
                schema: "control",
                table: "cloud_outbox_messages");
        }
    }
}

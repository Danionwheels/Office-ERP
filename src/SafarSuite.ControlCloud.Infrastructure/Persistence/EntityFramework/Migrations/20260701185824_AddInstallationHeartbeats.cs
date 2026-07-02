using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationHeartbeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installation_heartbeats",
                schema: "cloud",
                columns: table => new
                {
                    heartbeat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    heartbeat_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reported_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    license_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entitlement_version = table.Column<long>(type: "bigint", nullable: true),
                    paid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    warning_starts_at = table.Column<DateOnly>(type: "date", nullable: true),
                    grace_until = table.Column<DateOnly>(type: "date", nullable: true),
                    offline_valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    local_server_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation_heartbeats", x => x.heartbeat_id);
                    table.ForeignKey(
                        name: "FK_installation_heartbeats_client_installations_installation_id",
                        column: x => x.installation_id,
                        principalSchema: "cloud",
                        principalTable: "client_installations",
                        principalColumn: "installation_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_installation_heartbeats_client_id",
                schema: "cloud",
                table: "installation_heartbeats",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_heartbeats_installation_license_status",
                schema: "cloud",
                table: "installation_heartbeats",
                columns: new[] { "installation_id", "license_status" });

            migrationBuilder.CreateIndex(
                name: "ix_installation_heartbeats_installation_received_at",
                schema: "cloud",
                table: "installation_heartbeats",
                columns: new[] { "installation_id", "received_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installation_heartbeats",
                schema: "cloud");
        }
    }
}

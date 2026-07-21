using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationHeartbeatPairingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pairing_approved_device_count",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pairing_first_manager_device_approved",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pairing_first_manager_device_approved_at_utc",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pairing_last_device_updated_at_utc",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pairing_mode",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pairing_pending_device_count",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pairing_revoked_device_count",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pairing_suspended_device_count",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pairing_total_device_count",
                schema: "cloud",
                table: "installation_heartbeats",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pairing_approved_device_count",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_first_manager_device_approved",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_first_manager_device_approved_at_utc",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_last_device_updated_at_utc",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_mode",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_pending_device_count",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_revoked_device_count",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_suspended_device_count",
                schema: "cloud",
                table: "installation_heartbeats");

            migrationBuilder.DropColumn(
                name: "pairing_total_device_count",
                schema: "cloud",
                table: "installation_heartbeats");
        }
    }
}

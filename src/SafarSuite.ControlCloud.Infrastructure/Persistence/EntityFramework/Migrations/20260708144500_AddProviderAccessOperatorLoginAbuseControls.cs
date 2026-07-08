using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderAccessOperatorLoginAbuseControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_login_attempt_count",
                schema: "cloud",
                table: "provider_access_operators",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_failed_login_at_utc",
                schema: "cloud",
                table: "provider_access_operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_ends_at_utc",
                schema: "cloud",
                table: "provider_access_operators",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failed_login_attempt_count",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "last_failed_login_at_utc",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "lockout_ends_at_utc",
                schema: "cloud",
                table: "provider_access_operators");
        }
    }
}

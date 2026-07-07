using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderAccessOperatorRecoveryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "recovery_code_hashes_json",
                schema: "cloud",
                table: "provider_access_operators",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "recovery_codes_updated_at_utc",
                schema: "cloud",
                table: "provider_access_operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recovery_codes_updated_by",
                schema: "cloud",
                table: "provider_access_operators",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_recovery_code_used_at_utc",
                schema: "cloud",
                table: "provider_access_operators",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "recovery_code_hashes_json",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "recovery_codes_updated_at_utc",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "recovery_codes_updated_by",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "last_recovery_code_used_at_utc",
                schema: "cloud",
                table: "provider_access_operators");
        }
    }
}

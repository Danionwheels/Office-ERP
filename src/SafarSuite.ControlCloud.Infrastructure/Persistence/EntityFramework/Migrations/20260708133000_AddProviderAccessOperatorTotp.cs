using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderAccessOperatorTotp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "totp_secret",
                schema: "cloud",
                table: "provider_access_operators",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "totp_enabled_at_utc",
                schema: "cloud",
                table: "provider_access_operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "totp_updated_at_utc",
                schema: "cloud",
                table: "provider_access_operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "totp_updated_by",
                schema: "cloud",
                table: "provider_access_operators",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_totp_used_at_utc",
                schema: "cloud",
                table: "provider_access_operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "last_totp_step",
                schema: "cloud",
                table: "provider_access_operators",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "totp_secret",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "totp_enabled_at_utc",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "totp_updated_at_utc",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "totp_updated_by",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "last_totp_used_at_utc",
                schema: "cloud",
                table: "provider_access_operators");

            migrationBuilder.DropColumn(
                name: "last_totp_step",
                schema: "cloud",
                table: "provider_access_operators");
        }
    }
}

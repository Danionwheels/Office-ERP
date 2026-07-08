using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddBootstrapPackageRegisterMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "bootstrap_package_generated_at_utc",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "bootstrap_package_id",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "package_bundle_file_name",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "package_bundle_sha256",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "package_local_server_version",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "package_safar_suite_app_version",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_installation_setup_tokens_bootstrap_package_register",
                schema: "cloud",
                table: "installation_setup_tokens",
                columns: new[] { "client_id", "installation_id", "bootstrap_package_generated_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_installation_setup_tokens_bootstrap_package_register",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "bootstrap_package_generated_at_utc",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "bootstrap_package_id",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "package_bundle_file_name",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "package_bundle_sha256",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "package_local_server_version",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "package_safar_suite_app_version",
                schema: "cloud",
                table: "installation_setup_tokens");
        }
    }
}

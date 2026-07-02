using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationDeploymentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bootstrap_mode",
                schema: "cloud",
                table: "client_installations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_deployment_mode",
                schema: "cloud",
                table: "client_installations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "site_id",
                schema: "cloud",
                table: "client_installations",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "site_role",
                schema: "cloud",
                table: "client_installations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parent_site_id",
                schema: "cloud",
                table: "client_installations",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                schema: "cloud",
                table: "client_installations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sync_topology_id",
                schema: "cloud",
                table: "client_installations",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_deployment_mode",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "site_id",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "site_role",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parent_site_id",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sync_topology_id",
                schema: "cloud",
                table: "installation_setup_tokens",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bootstrap_mode",
                schema: "cloud",
                table: "client_installations");

            migrationBuilder.DropColumn(
                name: "client_deployment_mode",
                schema: "cloud",
                table: "client_installations");

            migrationBuilder.DropColumn(
                name: "site_id",
                schema: "cloud",
                table: "client_installations");

            migrationBuilder.DropColumn(
                name: "site_role",
                schema: "cloud",
                table: "client_installations");

            migrationBuilder.DropColumn(
                name: "parent_site_id",
                schema: "cloud",
                table: "client_installations");

            migrationBuilder.DropColumn(
                name: "branch_code",
                schema: "cloud",
                table: "client_installations");

            migrationBuilder.DropColumn(
                name: "sync_topology_id",
                schema: "cloud",
                table: "client_installations");

            migrationBuilder.DropColumn(
                name: "client_deployment_mode",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "site_id",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "site_role",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "parent_site_id",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "branch_code",
                schema: "cloud",
                table: "installation_setup_tokens");

            migrationBuilder.DropColumn(
                name: "sync_topology_id",
                schema: "cloud",
                table: "installation_setup_tokens");
        }
    }
}

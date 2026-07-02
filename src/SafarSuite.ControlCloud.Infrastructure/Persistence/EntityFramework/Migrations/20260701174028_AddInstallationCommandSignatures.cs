using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationCommandSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payload_sha256",
                schema: "cloud",
                table: "installation_commands",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "signature_algorithm",
                schema: "cloud",
                table: "installation_commands",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "signature_key_id",
                schema: "cloud",
                table: "installation_commands",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "signature_value",
                schema: "cloud",
                table: "installation_commands",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payload_sha256",
                schema: "cloud",
                table: "installation_commands");

            migrationBuilder.DropColumn(
                name: "signature_algorithm",
                schema: "cloud",
                table: "installation_commands");

            migrationBuilder.DropColumn(
                name: "signature_key_id",
                schema: "cloud",
                table: "installation_commands");

            migrationBuilder.DropColumn(
                name: "signature_value",
                schema: "cloud",
                table: "installation_commands");
        }
    }
}

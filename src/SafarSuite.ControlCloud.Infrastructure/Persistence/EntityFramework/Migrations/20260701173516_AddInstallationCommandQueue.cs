using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationCommandQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installation_commands",
                schema: "cloud",
                columns: table => new
                {
                    command_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    command_version = table.Column<long>(type: "bigint", nullable: false),
                    command_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    queued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    not_before_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledgement_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    acknowledgement_detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation_commands", x => x.command_id);
                    table.ForeignKey(
                        name: "FK_installation_commands_client_installations_installation_id",
                        column: x => x.installation_id,
                        principalSchema: "cloud",
                        principalTable: "client_installations",
                        principalColumn: "installation_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installation_command_acknowledgements",
                schema: "cloud",
                columns: table => new
                {
                    acknowledgement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    command_version = table.Column<long>(type: "bigint", nullable: false),
                    result_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation_command_acknowledgements", x => x.acknowledgement_id);
                    table.ForeignKey(
                        name: "FK_installation_command_acknowledgements_installation_commands~",
                        column: x => x.command_id,
                        principalSchema: "cloud",
                        principalTable: "installation_commands",
                        principalColumn: "command_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_installation_command_acknowledgements_command_id",
                schema: "cloud",
                table: "installation_command_acknowledgements",
                column: "command_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_command_acknowledgements_installation_id",
                schema: "cloud",
                table: "installation_command_acknowledgements",
                column: "installation_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_commands_client_id",
                schema: "cloud",
                table: "installation_commands",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_commands_installation_status",
                schema: "cloud",
                table: "installation_commands",
                columns: new[] { "installation_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_installation_commands_idempotency_key",
                schema: "cloud",
                table: "installation_commands",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_installation_commands_installation_version",
                schema: "cloud",
                table: "installation_commands",
                columns: new[] { "installation_id", "command_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installation_command_acknowledgements",
                schema: "cloud");

            migrationBuilder.DropTable(
                name: "installation_commands",
                schema: "cloud");
        }
    }
}

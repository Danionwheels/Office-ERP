using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationDiagnosticReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installation_diagnostic_reports",
                schema: "cloud",
                columns: table => new
                {
                    diagnostic_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    local_server_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    license_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    bundle_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation_diagnostic_reports", x => x.diagnostic_report_id);
                    table.ForeignKey(
                        name: "FK_installation_diagnostic_reports_client_installations_instal~",
                        column: x => x.installation_id,
                        principalSchema: "cloud",
                        principalTable: "client_installations",
                        principalColumn: "installation_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_installation_diagnostic_reports_client_id",
                schema: "cloud",
                table: "installation_diagnostic_reports",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_diagnostic_reports_installation_received",
                schema: "cloud",
                table: "installation_diagnostic_reports",
                columns: new[] { "installation_id", "received_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installation_diagnostic_reports",
                schema: "cloud");
        }
    }
}

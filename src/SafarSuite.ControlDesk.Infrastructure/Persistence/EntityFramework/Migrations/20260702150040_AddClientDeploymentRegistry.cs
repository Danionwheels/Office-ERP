using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddClientDeploymentRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_deployments",
                schema: "control",
                columns: table => new
                {
                    client_deployment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    bootstrap_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    client_deployment_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    site_id = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    site_role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    parent_site_id = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    branch_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sync_topology_id = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    local_server_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    safarsuite_app_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_deployments", x => x.client_deployment_id);
                    table.ForeignKey(
                        name: "FK_client_deployments_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "control",
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_deployments_client_primary",
                schema: "control",
                table: "client_deployments",
                columns: new[] { "client_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "ux_client_deployments_client_installation",
                schema: "control",
                table: "client_deployments",
                columns: new[] { "client_id", "installation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_deployments",
                schema: "control");
        }
    }
}

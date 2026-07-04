using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ControlDeskDbContext))]
    [Migration("20260704190000_AddProductAccessCatalogPersistence")]
    public partial class AddProductAccessCatalogPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_access_catalogs",
                schema: "control",
                columns: table => new
                {
                    catalog_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    module_groups_json = table.Column<string>(type: "jsonb", nullable: false),
                    resources_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_access_catalogs", x => x.catalog_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_access_catalogs",
                schema: "control");
        }
    }
}

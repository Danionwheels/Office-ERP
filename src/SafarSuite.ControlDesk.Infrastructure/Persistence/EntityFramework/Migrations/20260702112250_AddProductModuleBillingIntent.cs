using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddProductModuleBillingIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "product_module_code",
                schema: "control",
                table: "invoice_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_module_code",
                schema: "control",
                table: "client_charge_rules",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_product_module_code",
                schema: "control",
                table: "invoice_lines",
                column: "product_module_code");

            migrationBuilder.CreateIndex(
                name: "ix_client_charge_rules_product_module_code",
                schema: "control",
                table: "client_charge_rules",
                column: "product_module_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoice_lines_product_module_code",
                schema: "control",
                table: "invoice_lines");

            migrationBuilder.DropIndex(
                name: "ix_client_charge_rules_product_module_code",
                schema: "control",
                table: "client_charge_rules");

            migrationBuilder.DropColumn(
                name: "product_module_code",
                schema: "control",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "product_module_code",
                schema: "control",
                table: "client_charge_rules");
        }
    }
}

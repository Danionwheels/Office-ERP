using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingTaxFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "line_type",
                schema: "control",
                table: "invoice_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Charge");

            migrationBuilder.AddColumn<decimal>(
                name: "tax_percent",
                schema: "control",
                table: "client_charge_rules",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "line_type",
                schema: "control",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "tax_percent",
                schema: "control",
                table: "client_charge_rules");
        }
    }
}
